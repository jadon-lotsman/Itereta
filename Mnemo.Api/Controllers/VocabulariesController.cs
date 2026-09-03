using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    [Route("api/vocabularies")]
    public class VocabulariesController : ControllerBase
    {
        private readonly IMapper _mapper;
        private readonly VocabularyQueries _vocabularyQueries;
        private readonly VocabularyManagementService _vocabularyService;


        public VocabulariesController(
            IMapper mapper,
            VocabularyQueries vocabularyQueries,
            VocabularyManagementService vocabularyService)
        {
            _mapper = mapper;
            _vocabularyQueries = vocabularyQueries;
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

        [HttpGet("{guid}/sectors")]
        public async Task<IActionResult> GetVocabularySectors(Guid guid, [FromQuery] string isDescending)
        {
            var isDescendingBoolean = isDescending == "true" ? true : false;
            var response = await _vocabularyService.GetVocabularySectorsAsync(UserId, guid, isDescendingBoolean);

            return Ok(response);
        }

        [HttpGet("{guid}/statistics")]
        public async Task<IActionResult> GetVocabularyStatistics(Guid guid)
        {
            var response = await _vocabularyService.GetVocabularyStatisticsAsync(UserId, guid);

            return Ok(response);
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

        [HttpDelete("{guid}/guid")]
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
