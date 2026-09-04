using AutoMapper;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Mnemo.Contracts.Entry;
using Mnemo.Contracts.Entry.Requests;
using Mnemo.Data;
using Mnemo.Data.Entities;
using Mnemo.Data.Queries;
using Mnemo.Services.RepetitionService;
using Mnemo.Shared;
using Mnemo.Shared.Enums;
using Mnemo.Shared.Extensions;

namespace Mnemo.Services.VocabularyService
{
    public class EntryManagementService
    {
        private readonly ILogger<EntryManagementService> _logger;
        private readonly IValidator<CreateEntryRequest> _createValidator;
        private readonly IValidator<PatchEntryRequest> _patchValidator;
        private readonly IMapper _mapper;
        private readonly IOptions<SM2Options> _sm2;
        private readonly AppDbContext _context;
        private readonly VocabularyQueries _vocabularyQueries;
        private readonly VocabularyEntryQueries _entryQueries;


        public EntryManagementService(
            ILogger<EntryManagementService> logger,
            IValidator<CreateEntryRequest> createValidator,
            IValidator<PatchEntryRequest> patchValidator,
            IMapper mapper,
            IOptions<SM2Options> sm2,
            AppDbContext context,
            VocabularyQueries vocabularyQueries,
            VocabularyEntryQueries entryQueries)
        {
            _logger = logger;
            _createValidator = createValidator;
            _patchValidator = patchValidator;
            _mapper = mapper;
            _sm2 = sm2;
            _context = context;
            _vocabularyQueries = vocabularyQueries;
            _entryQueries = entryQueries;
        }



        public async Task<VocabularyPageResponse> GetVocabularyPageAsync(int userId, Guid guid, string startWord, string endWord, int page, int pageSize)
        {
            bool isDescending = string.Compare(endWord, startWord) < 0;

            string minWord, maxWord;
            if (isDescending)
            {
                minWord = endWord;
                maxWord = startWord;
            }
            else
            {
                minWord = startWord;
                maxWord = endWord;
            }


            var filteredQuery = _entryQueries
                .GetEntriesByVocabularyGuidQuery(userId, guid)
                .Where(e => string.Compare(e.Foreign, minWord) >= 0 &&
                            string.Compare(e.Foreign, maxWord) <= 0);

            IOrderedQueryable<VocabularyEntry> orderedQuery;
            if (isDescending)
            {
                orderedQuery = filteredQuery
                    .OrderByDescending(e => e.Foreign)
                    .ThenByDescending(e => e.PartOfSpeech);
            }
            else
            {
                orderedQuery = filteredQuery
                    .OrderBy(e => e.Foreign)
                    .ThenBy(e => e.PartOfSpeech);
            }

            var totalSectorEntries = await orderedQuery.CountAsync();
            int totalPages = (int)Math.Ceiling(totalSectorEntries / (decimal)pageSize);

            var entries = await orderedQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var entriesResponse = _mapper.Map<EntryResponse[]>(entries);

            return new VocabularyPageResponse
            {
                Entries = entriesResponse,
                hasMore = page < totalPages,
                SectorEntries = totalSectorEntries,
            };
        }


        public async Task<RequestResult<VocabularyEntry>> CreateEntryAsync(int userId, Guid guid, CreateEntryRequest request)
        {
            var result = await CreateEntriesAsync(userId, guid, new List<CreateEntryRequest>() { request });
            return result.SucceededResults.FirstOrDefault() ?? result.FailedResults.First();
        }

        public async Task<BatchRequestResult<VocabularyEntry>> CreateEntriesAsync(int userId, Guid guid, List<CreateEntryRequest> requests)
        {
            _logger.LogInformation("Creating {Count} vocabulary entries for user (UserId:{UserId})...", requests.Count, userId);

            int? id = await _vocabularyQueries.GetIdByGuidAsync(userId, guid);
            if (!id.HasValue)
            {
                _logger.LogWarning("Vocabulary (Guid:{Guid}) not found or access denied for user (UserId:{UserId})!", guid, userId);
                return BatchRequestResult<VocabularyEntry>.CriticalFailure(ErrorCode.VocabularyNotFound);
            }


            var validationResults = await _createValidator.ValidateBatchAsync(requests, _logger);

            var messages = string.Join("; ", validationResults.FailedResults.Select(e => e.ErrorMessage));
            var validationErrors = BatchRequestResult<VocabularyEntry>.CriticalFailure(ErrorCode.InvalidData, messages);

            if (validationResults.IsCriticalFailure)
                return validationErrors;


            var succeedRequests = validationResults.SucceededResults.Select(r => r.Value!);
            var entries =
                _mapper.Map<List<VocabularyEntry>>(succeedRequests)
                .RemoveKeyDuplicates();

            var filterResults = await FilterVocabularyDuplicatesAsync(userId, id.Value, entries);

            if (filterResults.IsCriticalFailure)
                return filterResults;


            var entriesToAdd = filterResults.SucceededResults.Select(r => r.Value!).ToList();

            foreach (var entry in entriesToAdd)
            {
                entry.VocabularyId = id.Value;
                entry.RepetitionState = new RepetitionState()
                {
                    EasinessFactor = _sm2.Value.InitEF,
                    RepetitionInterval = _sm2.Value.MinInterval
                };
            }

            if (entriesToAdd.Any())
            {
                await _context.VocabularyEntries.AddRangeAsync(entriesToAdd);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Created {Count} vocabulary entry (UserId:{UserId}, VocabId:{VocabId})!", entriesToAdd.Count, userId, id.Value);
            }

            var results = validationErrors.Results.Concat(filterResults.Results).ToList();
            return BatchRequestResult<VocabularyEntry>.Return(results);
        }

        private async Task<BatchRequestResult<VocabularyEntry>> FilterVocabularyDuplicatesAsync(int userId, int vocabId, IEnumerable<VocabularyEntry> entries)
        {
            var entryList = entries.ToList();
            int total = entryList.Count;

            _logger.LogDebug("Starting duplicate filter for {Count} entries (UserId: {UserId}, VocabId: {VocabId})...", total, userId, vocabId);

            var foreigns = entryList
                .Select(e => e.Foreign)
                .Where(f => !string.IsNullOrWhiteSpace(f))
                .Distinct()
                .ToList();

            var existingKeys = await _entryQueries
                .GetExistingKeysAsync(userId, vocabId, foreigns);

            var results = new List<RequestResult<VocabularyEntry>>(total);

            foreach (var entry in entries)
            {
                if (existingKeys.Contains((entry.Foreign, entry.PartOfSpeech)))
                {
                    _logger.LogWarning("Duplicate entry detected (UserId: {UserId}, VocabId: {VocabId}): Foreign:{Foreign}, PartOfSpeech:{PartOfSpeech}", userId, vocabId, entry.Foreign, entry.PartOfSpeech);
                    results.Add(RequestResult<VocabularyEntry>.Failure(ErrorCode.DuplicateEntry, $"Entry '{entry.Foreign}' with part of speech '{entry.PartOfSpeech}' already exists"));
                }
                else
                {
                    results.Add(RequestResult<VocabularyEntry>.Success(entry));
                }
            }

            int succeeded = results.Count(r => r.IsSuccess);
            int failed = total - succeeded;

            if (failed == total)
                _logger.LogWarning("Duplicate filter completed (UserId:{UserId}, VocabId:{VocabId}): all {Total} entries are diplicates!", userId, vocabId, total);
            else if (failed > 0)
                _logger.LogInformation("Duplicate filter completed (UserId:{UserId}, VocabId:{VocabId}): {Succeeded} unique, {Failed} duplicates out of {Total}!", userId, vocabId, succeeded, failed, total);
            else
                _logger.LogDebug("Duplicate filter completed (UserId:{UserId}, VocabId:{VocabId}): all {Total} entries are unique!", userId, vocabId, total);

            return BatchRequestResult<VocabularyEntry>.Return(results);
        }

        public async Task<RequestResult<VocabularyEntry>> PatchEntryAsync(int userId, Guid guid, int entryId, PatchEntryRequest request)
        {
            _logger.LogInformation("Patching entry (EntryId:{EntryId}) for user (UserId:{UserId})", entryId, userId);

            int? id = await _vocabularyQueries.GetIdByGuidAsync(userId, guid);
            if (!id.HasValue)
            {
                _logger.LogWarning("Vocabulary (Guid:{Guid}) not found or access denied for user (UserId:{UserId})", guid, userId);
                return RequestResult<VocabularyEntry>.Failure(ErrorCode.VocabularyNotFound);
            }

            var validationResult = await _patchValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var messages = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                _logger.LogWarning("PatchEntryRequest (EntryId:{EntryId}) is not valid: {messages}", entryId, messages);
                return RequestResult<VocabularyEntry>.Failure(ErrorCode.InvalidData, string.Join("; ", messages));
            }


            var currentEntry = await _entryQueries.GetByIdAsync(userId, id.Value, entryId);
            if (currentEntry == null)
            {
                _logger.LogWarning("Entry (EntryId:{EntryId}) not found for user (UserId:{UserId})", entryId, userId);
                return RequestResult<VocabularyEntry>.Failure(ErrorCode.EntryNotFound);
            }


            PartOfSpeech? newPartOfSpeech = null;
            if (request.PartOfSpeech != null)
                newPartOfSpeech = Enum.Parse<PartOfSpeech>(request.PartOfSpeech, true);

            string? newForeign = null;
            if (request.Foreign != null)
                newForeign = TextNormalizer.NormalizeForeign(request.Foreign);

            string? newTranscription = null;
            if (request.Transcription != null)
                newTranscription = TextNormalizer.NormalizeTranscription(request.Transcription);

            bool foreignUpdated = (newForeign != null && newForeign != currentEntry.Foreign);
            bool partOfSpeechUpdated = (newPartOfSpeech != null && newPartOfSpeech.Value != currentEntry.PartOfSpeech);
            bool transcriptionUpdated = (newTranscription != null && newTranscription != currentEntry.Transcription);

            bool needDuplicateCheck = foreignUpdated || partOfSpeechUpdated;


            if (needDuplicateCheck)
            {
                var checkForeign = newForeign ?? currentEntry.Foreign;
                var checkPartOfSpeech = newPartOfSpeech ?? currentEntry.PartOfSpeech;

                if (await _entryQueries.ExistsByKeysAsync(userId, id.Value, checkForeign, checkPartOfSpeech))
                {
                    _logger.LogWarning("Duplicate check failed for entry (EntryId:{EntryId})", entryId);
                    return RequestResult<VocabularyEntry>.Failure(ErrorCode.DuplicateEntry, "Entry already exists");
                }
            }


            if (foreignUpdated || partOfSpeechUpdated)
            {
                currentEntry.ResetAllMeta();
                _logger.LogDebug("All metadata reset and set as {Status}: (EntryId:{EntryId}) for user (UserId:{UserId})", currentEntry.EnrichmentStatus, entryId, userId);
            }
            else if (transcriptionUpdated)
            {
                currentEntry.ResetAudio();
                _logger.LogDebug("Audio reset and set as {Status}: (EntryId:{EntryId}) for user (UserId:{UserId})", currentEntry.EnrichmentStatus, entryId, userId);
            }


            var isPatched = currentEntry.TryPatch(request);

            if (!isPatched)
            {
                _logger.LogError("TryPatch failed for entry (EntryId:{EntryId}): Invalid Data", entryId);
                return RequestResult<VocabularyEntry>.Failure(ErrorCode.InvalidData, "Failed to apply patch");
            }


            await _context.SaveChangesAsync();
            _logger.LogInformation("Successfully patched entry (EntryId:{EntryId}) for user (UserId:{UserId})", entryId, userId);

            return RequestResult<VocabularyEntry>.Success(currentEntry);
        }

        public async Task<RequestResult<bool>> RemoveEntryByIdAsync(int userId, Guid guid, int entryId)
        {
            _logger.LogInformation("Attempting to delete entry (EntryId:{EntryId}) for user (UserId:{UserId})", entryId, userId);

            int? id = await _vocabularyQueries.GetIdByGuidAsync(userId, guid);
            if (!id.HasValue)
            {
                _logger.LogWarning("Vocabulary (Guid:{Guid}) not found or access denied for user (UserId:{UserId})", guid, userId);
                return RequestResult<bool>.Failure(ErrorCode.VocabularyNotFound);
            }

            var currentEntry = await _entryQueries.GetByIdAsync(userId, id.Value, entryId);
            if (currentEntry == null)
            {
                _logger.LogWarning("Entry (EntryId:{EntryId}) not found for user (UserId:{UserId})", entryId, userId);
                return RequestResult<bool>.Failure(ErrorCode.EntryNotFound);
            }


            _context.VocabularyEntries.Remove(currentEntry);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Successfully deleted entry (EntryId:{EntryId}) for user (UserId:{UserId})", entryId, userId);

            return RequestResult<bool>.Success(true);
        }
    }
}
