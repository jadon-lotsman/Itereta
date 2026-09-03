using AutoMapper;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Mnemo.Contracts.Vocabulary;
using Mnemo.Contracts.Vocabulary.Requests;
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



        public async Task<VocabularyStatisticsResponse> GetVocabularyStatisticsAsync(int userId, Guid vocabGuid)
        {
            var query = _entryQueries
                .GetEntriesByVocabularyGuidQuery(userId, vocabGuid);

            var totalEntries = await query
                .CountAsync();
            var totalTranslations = await query
                .SumAsync(e => e.Translations.Count);


            return new VocabularyStatisticsResponse()
            {
                TotalEntries = totalEntries,
                TotalTranslations = totalTranslations
            };
        }

        public async Task<List<VocabularySectorResponse>> GetVocabularySectorsAsync(int userId, Guid vocabGuid, bool isDescending)
        {
            var query = _entryQueries.GetEntriesByVocabularyGuidQuery(userId, vocabGuid);
            int minSectorSize = Math.Max(10, query.Count() / 7);

            var groupQuery = query
                .GroupBy(e => e.Foreign.Substring(0, 1))
                .Select(g => new
                {
                    Letter = g.Key,
                    Count = g.Count(),
                    StartWord = g.Min(e => e.Foreign)!,
                    EndWord = g.Max(e => e.Foreign)!
                })
                .OrderBy(e => e.Letter);


            var groups = await groupQuery.ToListAsync();
            var sectors = new List<VocabularySectorResponse>();
            var index = 0;

            foreach (var group in groups)
            {
                string sectorStart = group.StartWord;
                string sectorEnd = group.EndWord;
                int count = group.Count;


                if (!sectors.Any())
                {
                    sectors.Add(new VocabularySectorResponse()
                    {
                        StartWord = sectorStart,
                        EndWord = sectorEnd,
                        Count = count
                    });
                }
                else
                {
                    var lastSection = sectors.Last();
                    var isLastGroup = index == groups.Count - 1;

                    if (lastSection.Count < minSectorSize || (isLastGroup && count < minSectorSize))
                    {
                        lastSection.EndWord = sectorEnd;
                        lastSection.Count += count;
                    }
                    else
                    {
                        sectors.Add(new VocabularySectorResponse()
                        {
                            StartWord = sectorStart,
                            EndWord = sectorEnd,
                            Count = count
                        });
                    }
                }

                index++;
            }

            if (sectors.Any())
            {
                sectors.First().StartWord = "a";
                sectors.Last().EndWord = "z" + char.MaxValue;

                if (isDescending)
                {
                    foreach (var sector in sectors)
                        (sector.EndWord, sector.StartWord) = (sector.StartWord, sector.EndWord);

                    sectors.Reverse();
                }
            }


            return sectors;
        }

        public async Task<VocabularyPageResponse> GetVocabularyPageAsync(int userId, Guid vocabGuid, string startWord, string endWord, int page, int pageSize)
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
                .GetEntriesByVocabularyGuidQuery(userId, vocabGuid)
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


        public async Task<RequestResult<VocabularyEntry>> CreateEntryAsync(int userId, Guid vocabGuid, CreateEntryRequest request)
        {
            var result = await CreateEntriesAsync(userId, vocabGuid, new List<CreateEntryRequest>() { request });
            return result.Results.First();
        }

        public async Task<MassRequestResult<VocabularyEntry>> CreateEntriesAsync(int userId, Guid vocabGuid, List<CreateEntryRequest> requests)
        {
            _logger.LogInformation("Attempting to create {Count} vocabulary entries for user (UserId:{UserId})", requests.Count, userId);

            int? id = await _vocabularyQueries.GetIdByGuidAsync(userId, vocabGuid);
            if (!id.HasValue)
            {
                _logger.LogWarning("Vocabulary (Guid:{Guid}) not found or access denied for user (UserId:{UserId})", vocabGuid, userId);
                return MassRequestResult<VocabularyEntry>.AbsolutelyFailure(requests.Count, ErrorCode.AccessDenied);
            }

            if (!await _vocabularyQueries.ExistsByIdAsync(userId, id.Value))
            {
                _logger.LogWarning("Vocabulary (Guid:{Guid}) not found", vocabGuid);
                return MassRequestResult<VocabularyEntry>.AbsolutelyFailure(requests.Count, ErrorCode.VocabularyNotFound);
            }


            var results = new List<RequestResult<VocabularyEntry>>();


            _logger.LogDebug("Requests validating from user (UserId:{UserId})...", userId);

            var validReq = new List<CreateEntryRequest>();
            foreach (var req in requests)
            {
                var validationResult = await _createValidator.ValidateAsync(req);
                if (!validationResult.IsValid)
                {
                    var messages = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));

                    _logger.LogWarning("CreateEntryRequest (UserId:{UserId}) is not valid: {messages}", userId, messages);
                    results.Add(RequestResult<VocabularyEntry>.Failure(ErrorCode.InvalidData, messages));
                    continue;
                }

                validReq.Add(req);
            }

            if (!validReq.Any())
            {
                _logger.LogInformation("All requests ({Count}) is not valid from user (UserId:{UserId})", requests.Count, userId);
                return MassRequestResult<VocabularyEntry>.PartialSuccess(results);
            }


            var validEntries = _mapper.Map<List<VocabularyEntry>>(validReq);

            _logger.LogDebug("Exclude entry duplicates from user (UserId:{UserId})...", userId);

            var foreigns = validReq
                .Select(r => r.Foreign)
                .Where(f => !string.IsNullOrWhiteSpace(f))
                .Distinct()
                .ToList();

            var existingKeys = await _entryQueries
                .GetExistingKeysAsync(userId, id.Value, foreigns!);


            var entriesToAdd = new List<VocabularyEntry>();
            foreach (var entry in validEntries)
            {
                if (existingKeys.Contains((entry.Foreign, entry.PartOfSpeech)))
                {
                    _logger.LogWarning("Duplicate entry for user (UserId:{UserId}): Foreign:{Foreign}, PartOfSpeech:{PartOfSpeech}", userId, entry.Foreign, entry.PartOfSpeech);
                    results.Add(RequestResult<VocabularyEntry>.Failure(ErrorCode.DuplicateEntry, $"Entry '{entry.Foreign}' ({entry.PartOfSpeech}) already exists"));
                    continue;
                }

                entry.VocabularyId = id.Value;
                entry.RepetitionState = new RepetitionState()
                {
                    EasinessFactor = _sm2.Value.InitEF,
                    RepetitionInterval = _sm2.Value.MinInterval
                };

                entriesToAdd.Add(entry);
                results.Add(RequestResult<VocabularyEntry>.Success(entry));
            }


            if (entriesToAdd.Any())
            {
                await _context.VocabularyEntries.AddRangeAsync(entriesToAdd);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Created {Count} vocabulary entry for user (UserId:{UserId})", entriesToAdd.Count, userId);
            }

            return MassRequestResult<VocabularyEntry>.PartialSuccess(results);
        }

        public async Task<RequestResult<VocabularyEntry>> PatchEntryAsync(int userId, Guid vocabGuid, int entryId, PatchEntryRequest request)
        {
            _logger.LogInformation("Patching entry (EntryId:{EntryId}) for user (UserId:{UserId})", entryId, userId);

            int? id = await _vocabularyQueries.GetIdByGuidAsync(userId, vocabGuid);
            if (!id.HasValue)
            {
                _logger.LogWarning("Vocabulary (Guid:{Guid}) not found or access denied for user (UserId:{UserId})", vocabGuid, userId);
                return RequestResult<VocabularyEntry>.Failure(ErrorCode.AccessDenied);
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

        public async Task<RequestResult<bool>> RemoveEntryByIdAsync(int userId, Guid vocabGuid, int entryId)
        {
            _logger.LogInformation("Attempting to delete entry (EntryId:{EntryId}) for user (UserId:{UserId})", entryId, userId);

            int? id = await _vocabularyQueries.GetIdByGuidAsync(userId, vocabGuid);
            if (!id.HasValue)
            {
                _logger.LogWarning("Vocabulary (Guid:{Guid}) not found or access denied for user (UserId:{UserId})", vocabGuid, userId);
                return RequestResult<bool>.Failure(ErrorCode.AccessDenied);
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
