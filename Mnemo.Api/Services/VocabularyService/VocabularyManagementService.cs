using AutoMapper;
using FluentValidation;
using Mnemo.Contracts.Pack.Requests;
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
        private readonly IValidator<CreateVocabularyRequest> _createPackValidator;
        private readonly IValidator<CreateEntryRequest> _createEntryValidator;
        private readonly IMapper _mapper;
        private readonly AppDbContext _context;
        private readonly AccountQueries _accountQueries;
        private readonly VocabularyQueries _packQueries;



        public VocabularyManagementService(ILogger<VocabularyManagementService> logger, IValidator<CreateVocabularyRequest> createPackValidator, IValidator<CreateEntryRequest> createEntryValidator, IMapper mapper, AppDbContext context, AccountQueries accountQueries, VocabularyQueries packQueries)
        {
            _logger = logger;
            _createPackValidator = createPackValidator;
            _createEntryValidator = createEntryValidator;
            _mapper = mapper;
            _context = context;
            _accountQueries = accountQueries;
            _packQueries = packQueries;
        }


        public async Task<RequestResult<Vocabulary>> CreateVocabularyAsync(int userId, CreateVocabularyRequest request)
        {
            _logger.LogInformation("Attempting to create a pack for user (UserId:{UserId})", userId);

            var validationPackResult = await _createPackValidator.ValidateAsync(request);
            if (!validationPackResult.IsValid)
            {
                var messages = string.Join("; ", validationPackResult.Errors.Select(e => e.ErrorMessage));
                _logger.LogWarning("CreatePackRequest (UserId:{UserId}) is not valid: {messages}", userId, messages);
                return RequestResult<Vocabulary>.Failure(ErrorCode.InvalidData, string.Join("; ", messages));
            }


            if (!await _accountQueries.ExistsByIdAsync(userId))
            {
                _logger.LogWarning("User (UserId:{UserId}) not found", userId);
                return RequestResult<Vocabulary>.Failure(ErrorCode.UserNotFound);
            }


            var results = new List<RequestResult<VocabularyEntry>>();


            _logger.LogDebug("Requests validating from user (UserId:{UserId})...", userId);

            var validReq = new List<CreateEntryRequest>();
            foreach (var req in request.PackEntries)
            {
                var validationEntryResult = await _createEntryValidator.ValidateAsync(req);
                if (!validationEntryResult.IsValid)
                {
                    var messages = string.Join("; ", validationEntryResult.Errors.Select(e => e.ErrorMessage));

                    _logger.LogWarning("CreateEntryRequest (UserId:{UserId}) is not valid: {messages}", userId, messages);
                    results.Add(RequestResult<VocabularyEntry>.Failure(ErrorCode.InvalidData, messages));
                    continue;
                }

                validReq.Add(req);
            }

            if (!validReq.Any())
            {
                var messages = string.Join("; ", validationPackResult.Errors.Select(e => e.ErrorMessage));
                _logger.LogInformation("All requests ({Count}) is not valid from user (UserId:{UserId}): {messages}", request.PackEntries.Length, userId, messages);
                return RequestResult<Vocabulary>.Failure(ErrorCode.InvalidData, string.Join("; ", messages));
            }


            var validPackEntries = _mapper.Map<List<VocabularyEntry>>(validReq);

            var pack = _mapper.Map<Vocabulary>(request);
            pack.OwnerId = userId;
            pack.Entries = validPackEntries;


            await _context.AddAsync(pack);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Successfully created pack (Guid:{Guid}) for user (UserId:{UserId})", pack.Guid, userId);

            return RequestResult<Vocabulary>.Success(pack);
        }

        public async Task<RequestResult<Vocabulary>> PatchVocabularyAsync(int userId, Guid vocabGuid, PatchVocabularyRequest request)
        {
            _logger.LogInformation("Attempting to patch a vocabulary pack for user (UserId:{UserId})", userId);

            var currentPack = await _packQueries.GetByGuidAsync(userId, vocabGuid);
            if (currentPack == null)
            {
                _logger.LogWarning("Pack (Guid:{Guid}) not found for user (UserId:{UserId})", vocabGuid, userId);
                return RequestResult<Vocabulary>.Failure(ErrorCode.VocabularyNotFound);
            }

            var isPatched = currentPack.TryPatch(request);

            if (!isPatched)
            {
                _logger.LogError("TryPatch failed for pack (Guid:{Guid}): Invalid Data", vocabGuid);
                return RequestResult<Vocabulary>.Failure(ErrorCode.InvalidData, "Failed to apply patch");
            }


            await _context.SaveChangesAsync();
            _logger.LogInformation("Successfully patched pack (Guid:{Guid}) for user (UserId:{UserId})", vocabGuid, userId);

            return RequestResult<Vocabulary>.Success(currentPack);
        }

        //public async Task<MassRequestResult<VocabularyEntry>> ImportFromPackAsync(int userId, Guid packGuid);

        public async Task<RequestResult<Guid>> RevokeVocabularyGuidAsync(int userId, Guid vocabGuid)
        {
            _logger.LogInformation("Attempting to revoke guid for pack (Guid:{Guid}) for user (UserId:{UserId})", vocabGuid, userId);

            var currentPack = await _packQueries.GetByGuidAsync(userId, vocabGuid);
            if (currentPack == null)
            {
                _logger.LogWarning("Pack (Guid:{Guid}) not found for user (UserId:{UserId})", vocabGuid, userId);
                return RequestResult<Guid>.Failure(ErrorCode.VocabularyNotFound);
            }


            var newGuid = Guid.NewGuid();
            currentPack.Guid = newGuid;
            await _context.SaveChangesAsync();
            _logger.LogInformation("Successfully revoked guid for pack (Guid:{Guid}) for user (UserId:{UserId})", vocabGuid, userId);

            return RequestResult<Guid>.Success(newGuid);
        }

        public async Task<RequestResult<bool>> RemoveVocabularyByGuidAsync(int userId, Guid vocabGuid)
        {
            _logger.LogInformation("Attempting to delete pack (Guid:{Guid}) for user (UserId:{UserId})", vocabGuid, userId);

            var currentPack = await _packQueries.GetByGuidAsync(userId, vocabGuid);
            if (currentPack == null)
            {
                _logger.LogWarning("Pack (Guid:{Guid}) not found for user (UserId:{UserId})", vocabGuid, userId);
                return RequestResult<bool>.Failure(ErrorCode.VocabularyNotFound);
            }


            _context.Vocabularies.Remove(currentPack);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Successfully deleted pack (Guid:{Guid}) for user (UserId:{UserId})", vocabGuid, userId);

            return RequestResult<bool>.Success(true);
        }
    }
}
