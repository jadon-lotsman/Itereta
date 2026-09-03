using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mnemo.Contracts.Entry;
using Mnemo.Contracts.Vocabulary;
using Mnemo.Contracts.Vocabulary.Requests;
using Mnemo.Data.Queries;
using Mnemo.Services.VocabularyService;
using Mnemo.Shared.Enums;
using System.Security.Claims;

namespace Mnemo.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class VocabularyController : ControllerBase
    {
        private readonly IMapper _mapper;
        private readonly VocabularyQueries _vocabularyQueries;
        private readonly VocabularyEntryQueries _entryQueries;
        private readonly VocabularyManagementService _vocabularyService;


        public VocabularyController(IMapper mapper, VocabularyQueries vocabularyQueries, VocabularyEntryQueries entryQueries, VocabularyManagementService vocabularyService)
        {
            _mapper = mapper;
            _vocabularyQueries = vocabularyQueries;
            _entryQueries = entryQueries;
            _vocabularyService = vocabularyService;
        }

        private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));



        [HttpGet]
        public async Task<IActionResult> GetAllPublic()
        {
            var vocabs = await _vocabularyQueries.GetPublishedAsync();

            var vocabsResponse = _mapper.Map<List<VocabularyResponse>>(vocabs);
            return Ok(vocabsResponse);
        }

        [HttpGet("{guid}")]
        public async Task<IActionResult> GetVocabularyByGuid([FromRoute] Guid guid)
        {
            var vocab = await _vocabularyQueries.GetByGuidAsync(UserId, guid);

            if (vocab == null)
                return NotFound();

            var vocabResponse = _mapper.Map<VocabularyResponse>(vocab);
            return Ok(vocabResponse);
        }

        [HttpGet("{guid}/search")]
        public async Task<IActionResult> SearchInVocabulary(Guid guid, [FromQuery] string query)
        {
            var id = await _vocabularyQueries.GetIdByGuidAsync(UserId, guid);

            if (!id.HasValue)
                return Forbid();

            var entries = await _entryQueries.GetByQueryAsync(UserId, id.Value, query);

            if (entries == null)
                return NotFound();

            var entriesResponse = _mapper.Map<List<EntryResponse>>(entries);
            return Ok(entriesResponse);
        }


        [HttpPost]
        public async Task<IActionResult> CreateVocabulary([FromBody] CreateVocabularyRequest request)
        {
            var result = await _vocabularyService.CreateVocabularyAsync(UserId, request);

            if (!result.IsSuccess)
            {
                return result.ErrorCode switch
                {
                    ErrorCode.InvalidData => BadRequest(new { message = result.ErrorMessage }),
                    ErrorCode.UserNotFound => NotFound(new { message = result.ErrorMessage }),
                    _ => StatusCode(500, new { message = result.ErrorMessage })
                };
            }

            var vocabResponse = _mapper.Map<VocabularyResponse>(result.Value);
            return Ok(vocabResponse);
        }

        [HttpPost("{guid}")]
        public async Task<IActionResult> RevokeVocabularyGuid(Guid guid)
        {
            var result = await _vocabularyService.RevokeVocabularyGuidAsync(UserId, guid);

            if (!result.IsSuccess)
            {
                return result.ErrorCode switch
                {
                    ErrorCode.VocabularyNotFound => NotFound(new { message = result.ErrorMessage }),
                    _ => StatusCode(500, new { message = result.ErrorMessage })
                };
            }

            return Ok(new { newGuid = result.Value });
        }

        [HttpDelete("{guid}")]
        public async Task<IActionResult> DeleteVocabulary(Guid guid)
        {
            var result = await _vocabularyService.RemoveVocabularyByGuidAsync(UserId, guid);

            if (!result.IsSuccess)
            {
                return result.ErrorCode switch
                {
                    ErrorCode.VocabularyNotFound => NotFound(new { message = result.ErrorMessage }),
                    _ => StatusCode(500, new { message = result.ErrorMessage })
                };
            }

            return NoContent();
        }
    }
}
