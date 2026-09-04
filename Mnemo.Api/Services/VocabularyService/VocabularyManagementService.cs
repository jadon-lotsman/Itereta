using AutoMapper;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Mnemo.Contracts.Entry;
using Mnemo.Contracts.Entry.Requests;
using Mnemo.Contracts.Vocabulary.Requests;
using Mnemo.Data;
using Mnemo.Data.Entities;
using Mnemo.Data.Queries;
using Mnemo.Shared;
using Mnemo.Shared.Enums;
using Mnemo.Shared.Extensions;

namespace Mnemo.Services.VocabularyService
{
    public class VocabularyManagementService
    {
        private readonly ILogger<VocabularyManagementService> _logger;
        private readonly IValidator<CreateVocabularyRequest> _createVocabularyValidator;
        private readonly IValidator<CreateEntryRequest> _createEntryValidator;
        private readonly IMapper _mapper;
        private readonly AppDbContext _context;
        private readonly AccountQueries _accountQueries;
        private readonly VocabularyEntryQueries _entryQueries;
        private readonly VocabularyQueries _vocabularyQueries;



        public VocabularyManagementService(
            ILogger<VocabularyManagementService> logger,
            IValidator<CreateVocabularyRequest> createVocabularyValidator,
            IValidator<CreateEntryRequest> createEntryValidator,
            IMapper mapper,
            AppDbContext context,
            AccountQueries accountQueries,
            VocabularyEntryQueries entryQueries,
            VocabularyQueries vocabularyQueries)
        {
            _logger = logger;
            _createVocabularyValidator = createVocabularyValidator;
            _createEntryValidator = createEntryValidator;
            _mapper = mapper;
            _context = context;
            _accountQueries = accountQueries;
            _entryQueries = entryQueries;
            _vocabularyQueries = vocabularyQueries;
        }


        public async Task<VocabularyStatisticsResponse> GetVocabularyStatisticsAsync(int userId, Guid guid)
        {
            var query = _entryQueries
                .GetEntriesByVocabularyGuidQuery(userId, guid);

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

        public async Task<List<VocabularySectorResponse>> GetVocabularySectorsAsync(int userId, Guid guid, bool isDescending)
        {
            var query = _entryQueries.GetEntriesByVocabularyGuidQuery(userId, guid);
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

        public async Task<RequestResult<Vocabulary>> CreateVocabularyAsync(int userId, CreateVocabularyRequest request)
        {
            _logger.LogInformation("Creating a vocabulary for user (UserId:{UserId})...", userId);

            var validationResult = await _createVocabularyValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var messages = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                _logger.LogWarning("CreateVocabularyRequest (UserId:{UserId}) is not valid: {messages}", userId, messages);
                return RequestResult<Vocabulary>.Failure(ErrorCode.InvalidData, string.Join("; ", messages));
            }


            if (!await _accountQueries.ExistsByIdAsync(userId))
            {
                _logger.LogWarning("User (UserId:{UserId}) not found", userId);
                return RequestResult<Vocabulary>.Failure(ErrorCode.UserNotFound);
            }


            var results = new List<RequestResult<VocabularyEntry>>();

            var validationResults = await _createEntryValidator.ValidateBatchAsync(request.Entries, _logger);

            if (validationResults.IsCriticalFailure)
            {
                _logger.LogInformation("All requests ({Count}) is not valid from user (UserId:{UserId})!", request.Entries.Length, userId);
                var messages = string.Join("; ", validationResults.FailedResults.Select(e => e.ErrorMessage));
                return RequestResult<Vocabulary>.Failure(ErrorCode.InvalidData, string.Join("; ", messages));
            }


            var succeedRequests = validationResults.SucceededResults.Select(r => r.Value!);
            var validNewEntries = _mapper.Map<List<VocabularyEntry>>(succeedRequests);

            var vocab = _mapper.Map<Vocabulary>(request);
            vocab.OwnerId = userId;
            vocab.Entries = validNewEntries;


            await _context.AddAsync(vocab);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Successfully created vocabulary (Guid:{Guid}) for user (UserId:{UserId})!", vocab.Guid, userId);

            return RequestResult<Vocabulary>.Success(vocab);
        }

        public async Task<RequestResult<Vocabulary>> PatchVocabularyAsync(int userId, Guid guid, PatchVocabularyRequest request)
        {
            _logger.LogInformation("Attempting to patch a vocabulary for user (UserId:{UserId})", userId);

            var id = await _vocabularyQueries.GetIdByGuidAsync(userId, guid);
            if (!id.HasValue)
            {
                _logger.LogWarning("Vocabulary (Guid:{Guid}) not found or access denied for user (UserId:{UserId})", guid, userId);
                return RequestResult<Vocabulary>.Failure(ErrorCode.AccessDenied);
            }

            var currentVocab = await _vocabularyQueries.GetByIdAsync(userId, id.Value);
            if (currentVocab == null)
            {
                _logger.LogWarning("Vocabulary (Guid:{Guid}) not found for user (UserId:{UserId})", guid, userId);
                return RequestResult<Vocabulary>.Failure(ErrorCode.VocabularyNotFound);
            }

            var isPatched = currentVocab.TryPatch(request);
            if (!isPatched)
            {
                _logger.LogError("TryPatch failed for vocabulary (Guid:{Guid}): Invalid Data", guid);
                return RequestResult<Vocabulary>.Failure(ErrorCode.InvalidData, "Failed to apply patch");
            }


            await _context.SaveChangesAsync();
            _logger.LogInformation("Successfully patched vocabulary (Guid:{Guid}) for user (UserId:{UserId})", guid, userId);

            return RequestResult<Vocabulary>.Success(currentVocab);
        }

        public async Task<RequestResult<Guid>> RevokeVocabularyGuidAsync(int userId, Guid guid)
        {
            _logger.LogInformation("Attempting to revoke guid for vocabulary (Guid:{Guid}) for user (UserId:{UserId})", guid, userId);

            var id = await _vocabularyQueries.GetIdByGuidAsync(userId, guid);
            if (!id.HasValue)
            {
                _logger.LogWarning("Vocabulary (Guid:{Guid}) not found or access denied for user (UserId:{UserId})", guid, userId);
                return RequestResult<Guid>.Failure(ErrorCode.AccessDenied);
            }

            var currentVocab = await _vocabularyQueries.GetByIdAsync(userId, id.Value);
            if (currentVocab == null)
            {
                _logger.LogWarning("Vocabulary (Guid:{Guid}) not found for user (UserId:{UserId})", guid, userId);
                return RequestResult<Guid>.Failure(ErrorCode.VocabularyNotFound);
            }


            var newGuid = Guid.NewGuid();
            currentVocab.Guid = newGuid;
            await _context.SaveChangesAsync();
            _logger.LogInformation("Successfully revoked guid for vocabulary (Guid:{Guid}) for user (UserId:{UserId})", guid, userId);

            return RequestResult<Guid>.Success(newGuid);
        }

        public async Task<RequestResult<bool>> RemoveVocabularyByGuidAsync(int userId, Guid guid)
        {
            _logger.LogInformation("Attempting to delete vocabulary (Guid:{Guid}) for user (UserId:{UserId})", guid, userId);

            var id = await _vocabularyQueries.GetIdByGuidAsync(userId, guid);
            if (!id.HasValue)
            {
                _logger.LogWarning("Vocabulary (Guid:{Guid}) not found or access denied for user (UserId:{UserId})", guid, userId);
                return RequestResult<bool>.Failure(ErrorCode.AccessDenied);
            }

            var currentVocab = await _vocabularyQueries.GetByIdAsync(userId, id.Value);
            if (currentVocab == null)
            {
                _logger.LogWarning("Vocabulary (Guid:{Guid}) not found for user (UserId:{UserId})", guid, userId);
                return RequestResult<bool>.Failure(ErrorCode.VocabularyNotFound);
            }


            _context.Vocabularies.Remove(currentVocab);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Successfully deleted vocabulary (Guid:{Guid}) for user (UserId:{UserId})", guid, userId);

            return RequestResult<bool>.Success(true);
        }
    }
}
