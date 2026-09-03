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
        private readonly VocabularyEntryQueries _entryQueries;
        private readonly EntryManagementService _entryService;


        public EntryController(IMapper mapper, VocabularyEntryQueries entryQueries, EntryManagementService entryService)
        {
            _mapper = mapper;
            _entryQueries = entryQueries;
            _entryService = entryService;
        }

        private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));



        [HttpGet("{guid}")]
        public async Task<IActionResult> GetVocabularyPage(Guid guid, [FromQuery] string startWord, string endWord, int page, int pageSize)
        {
            var response = await _entryService.GetVocabularyPageAsync(UserId, guid, startWord, endWord, page, pageSize);

            return Ok(response);
        }

        [HttpGet("{guid}/sectors")]
        public async Task<IActionResult> GetVocabularySectors(Guid guid, [FromQuery] string isDescending)
        {
            var isDescendingBoolean = isDescending == "true" ? true : false;
            var response = await _entryService.GetVocabularySectorsAsync(UserId, guid, isDescendingBoolean);

            return Ok(response);
        }

        [HttpGet("{guid}/statistics")]
        public async Task<IActionResult> GetVocabularyStatistics(Guid guid)
        {
            var response = await _entryService.GetVocabularyStatisticsAsync(UserId, guid);

            return Ok(response);
        }


        [HttpPost("{guid}")]
        public async Task<IActionResult> CreateEntry(Guid guid, [FromBody] CreateEntryRequest request)
        {
            var result = await _entryService.CreateEntryAsync(UserId, guid, request);

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
            var result = await _entryService.PatchEntryAsync(UserId, guid, id, request);

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
            var result = await _entryService.RemoveEntryByIdAsync(UserId, guid, id);

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
