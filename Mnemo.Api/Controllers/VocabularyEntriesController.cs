using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mnemo.Contracts.Entry;
using Mnemo.Contracts.Entry.Requests;
using Mnemo.Data.Queries;
using Mnemo.Services.VocabularyService;
using Mnemo.Shared.Enums;
using System.Security.Claims;

namespace Mnemo.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/vocabularies/{guid}/entries")]
    public class VocabularyEntriesController : ControllerBase
    {
        private readonly IMapper _mapper;
        private readonly VocabularyQueries _vocabularyQueries;
        private readonly VocabularyEntryQueries _entryQueries;
        private readonly EntryManagementService _entryService;


        public VocabularyEntriesController(IMapper mapper, VocabularyQueries vocabularyQueries, VocabularyEntryQueries entryQueries, EntryManagementService entryService)
        {
            _mapper = mapper;
            _vocabularyQueries = vocabularyQueries;
            _entryQueries = entryQueries;
            _entryService = entryService;
        }

        private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));


        [HttpGet]
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

        [HttpGet("{startWord:alpha}-{endWord:alpha}")]
        public async Task<IActionResult> GetVocabularyPage(Guid guid, string startWord, string endWord, [FromQuery] int page, int pageSize)
        {
            var response = await _entryService.GetVocabularyPageAsync(UserId, guid, startWord, endWord, page, pageSize);

            return Ok(response);
        }

        [HttpPost]
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

        [HttpPatch("{id:int}")]
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

        [HttpDelete("{id:int}")]
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
