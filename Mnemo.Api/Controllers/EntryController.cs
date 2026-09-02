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
    [Route("api/[controller]")]
    public class EntryController : ControllerBase
    {
        private readonly IMapper _mapper;
        private readonly VocabularyEntryQueries _vocabularyQueries;
        private readonly EntryManagementService _vocabularyService;


        public EntryController(IMapper mapper, VocabularyEntryQueries vocabularyQueries, EntryManagementService vocabularyService)
        {
            _mapper = mapper;
            _vocabularyQueries = vocabularyQueries;
            _vocabularyService = vocabularyService;
        }

        private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));



        [HttpGet("{guid}")]
        public async Task<IActionResult> GetVocabularyPage(Guid guid, [FromQuery] string startWord, string endWord, int page, int pageSize)
        {
            var response = await _vocabularyService.GetVocabularyPageAsync(UserId, guid, startWord, endWord, page, pageSize);

            return Ok(response);
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

        [HttpGet("{guid}/{id:int}")]
        public async Task<IActionResult> GetEntryById(Guid guid, int id)
        {
            var entry = await _vocabularyQueries.GetByIdAsync(UserId,  guid, id);

            if (entry == null)
                return NotFound();

            var entryRespose = _mapper.Map<EntryResponse>(entry);
            return Ok(entryRespose);
        }

        [HttpGet("{guid}/search")]
        public async Task<IActionResult> SearchInVocabularyByQuery(Guid guid, [FromQuery] string query)
        {
            var entries = await _vocabularyQueries.GetByQueryAsync(UserId, guid, query);

            if (entries == null)
                return NotFound();

            var entriesResponse = _mapper.Map<List<EntryResponse>>(entries);
            return Ok(entriesResponse);
        }


        [HttpPost("{guid}")]
        public async Task<IActionResult> CreateEntry(Guid guid, [FromBody] CreateEntryRequest request)
        {
            var result = await _vocabularyService.CreateEntryAsync(UserId, guid, request);

            if (!result.IsSuccess)
            {
                return result.ErrorCode switch
                {
                    ErrorCode.InvalidData => BadRequest(new { message = result.ErrorMessage }),
                    ErrorCode.UserNotFound => NotFound(new { message = result.ErrorMessage }),
                    ErrorCode.DuplicateEntry => Conflict(new { message = result.ErrorMessage }),
                    _ => StatusCode(500, new { message = result.ErrorMessage })
                };
            }

            var entry = result.Value;
            var entryRespose = _mapper.Map<EntryResponse>(entry);
            return Ok(entryRespose);
        }

        [HttpPatch("{guid}/{id:int}")]
        public async Task<IActionResult> PatchEntry(Guid guid, int id, [FromBody] PatchEntryRequest request)
        {
            var result = await _vocabularyService.PatchEntryAsync(UserId, guid, id, request);

            if (!result.IsSuccess)
            {
                return result.ErrorCode switch
                {
                    ErrorCode.InvalidData => BadRequest(new { message = result.ErrorMessage }),
                    ErrorCode.EntryNotFound => NotFound(new { message = result.ErrorMessage }),
                    ErrorCode.DuplicateEntry => Conflict(new { message = result.ErrorMessage }),
                    _ => StatusCode(500, new { message = result.ErrorMessage })
                };
            }

            var entry = result.Value;
            var entryRespose = _mapper.Map<EntryResponse>(entry);
            return Ok(entryRespose);
        }

        [HttpDelete("{guid}/{id:int}")]
        public async Task<IActionResult> DeleteEntry(Guid guid, int id)
        {
            var result = await _vocabularyService.RemoveEntryByIdAsync(UserId, guid, id);

            if (!result.IsSuccess)
            {
                return result.ErrorCode switch
                {
                    ErrorCode.EntryNotFound => NotFound(new { message = result.ErrorMessage }),
                    _ => StatusCode(500, new { message = result.ErrorMessage })
                };
            }

            return NoContent();
        }
    }
}
