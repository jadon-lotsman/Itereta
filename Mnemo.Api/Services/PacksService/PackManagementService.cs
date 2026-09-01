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

namespace Mnemo.Services.PackService
{
    public class PackManagementService
    {
        private readonly ILogger<PackManagementService> _logger;
        private readonly IValidator<CreatePackRequest> _createPackValidator;
        private readonly IValidator<CreateEntryRequest> _createEntryValidator;
        private readonly IMapper _mapper;
        private readonly AppDbContext _context;
        private readonly AccountQueries _accountQueries;
        private readonly PackQueries _packQueries;



        public PackManagementService(ILogger<PackManagementService> logger, IValidator<CreatePackRequest> createPackValidator, IValidator<CreateEntryRequest> createEntryValidator, IMapper mapper, AppDbContext context, AccountQueries accountQueries, PackQueries packQueries)
        {
            _logger = logger;
            _createPackValidator = createPackValidator;
            _createEntryValidator = createEntryValidator;
            _mapper = mapper;
            _context = context;
            _accountQueries = accountQueries;
            _packQueries = packQueries;
        }


        public async Task<RequestResult<VocabularyPack>> CreatePackAsync(int userId, CreatePackRequest request)
        {
            _logger.LogInformation("Attempting to create a pack for user (UserId:{UserId})", userId);

            var validationPackResult = await _createPackValidator.ValidateAsync(request);
            if (!validationPackResult.IsValid)
            {
                var messages = string.Join("; ", validationPackResult.Errors.Select(e => e.ErrorMessage));
                _logger.LogWarning("CreatePackRequest (UserId:{UserId}) is not valid: {messages}", userId, messages);
                return RequestResult<VocabularyPack>.Failure(ErrorCode.InvalidData, string.Join("; ", messages));
            }


            if (!await _accountQueries.ExistsByIdAsync(userId))
            {
                _logger.LogWarning("User (UserId:{UserId}) not found", userId);
                return RequestResult<VocabularyPack>.Failure(ErrorCode.UserNotFound);
            }


            var results = new List<RequestResult<VocabularyPackEntry>>();


            _logger.LogDebug("Requests validating from user (UserId:{UserId})...", userId);

            var validReq = new List<CreateEntryRequest>();
            foreach (var req in request.PackEntries)
            {
                var validationEntryResult = await _createEntryValidator.ValidateAsync(req);
                if (!validationEntryResult.IsValid)
                {
                    var messages = string.Join("; ", validationEntryResult.Errors.Select(e => e.ErrorMessage));

                    _logger.LogWarning("CreateEntryRequest (UserId:{UserId}) is not valid: {messages}", userId, messages);
                    results.Add(RequestResult<VocabularyPackEntry>.Failure(ErrorCode.InvalidData, messages));
                    continue;
                }

                validReq.Add(req);
            }

            if (!validReq.Any())
            {
                _logger.LogInformation("All requests ({Count}) is not valid from user (UserId:{UserId})", request.PackEntries.Length, userId);
                return RequestResult<VocabularyPack>.Failure(ErrorCode.InvalidData);
            }


            var validPackEntries = _mapper.Map<List<VocabularyPackEntry>>(validReq);

            var pack = _mapper.Map<VocabularyPack>(request);
            pack.AuthorId = userId;
            pack.PackEntries = validPackEntries;


            await _context.AddAsync(pack);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Successfully created pack (Guid:{Guid}) for user (UserId:{UserId})", pack.Guid, userId);

            return RequestResult<VocabularyPack>.Success(pack);
        }

        public async Task<RequestResult<VocabularyPack>> PatchPackAsync(int userId, Guid packGuid, PatchPackRequest request)
        {
            _logger.LogInformation("Attempting to patch a vocabulary pack for user (UserId:{UserId})", userId);

            var currentPack = await _packQueries.GetByGuidAsync(userId, packGuid);
            if (currentPack == null)
            {
                _logger.LogWarning("Pack (Guid:{Guid}) not found for user (UserId:{UserId})", packGuid, userId);
                return RequestResult<VocabularyPack>.Failure(ErrorCode.PackNotFound);
            }

            var isPatched = currentPack.TryPatch(request);

            if (!isPatched)
            {
                _logger.LogError("TryPatch failed for pack (Guid:{Guid}): Invalid Data", packGuid);
                return RequestResult<VocabularyPack>.Failure(ErrorCode.InvalidData, "Failed to apply patch");
            }


            await _context.SaveChangesAsync();
            _logger.LogInformation("Successfully patched pack (Guid:{Guid}) for user (UserId:{UserId})", packGuid, userId);

            return RequestResult<VocabularyPack>.Success(currentPack);
        }

        //public async Task<MassRequestResult<VocabularyEntry>> ImportFromPackAsync(int userId, Guid packGuid);

        //public async Task<RequestResult<bool>> RemovePackByGuidAsync(int userId, Guid packGuid);
    }
}
