using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mnemo.Contracts.Pack;
using Mnemo.Contracts.Pack.Requests;
using Mnemo.Data.Queries;
using Mnemo.Services.PackService;
using Mnemo.Shared.Enums;
using System.Security.Claims;

namespace Mnemo.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]/packs")]
    public class PackController : ControllerBase
    {
        private readonly IMapper _mapper;
        private readonly PackQueries _packQueries;
        private readonly PackManagementService _packService;


        public PackController(IMapper mapper, PackQueries packQueries, PackManagementService packService)
        {
            _mapper = mapper;
            _packQueries = packQueries;
            _packService = packService;
        }

        private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));



        [HttpGet]
        public async Task<IActionResult> GetAllPublic()
        {
            var packs = await _packQueries.GetAllPublicAsync();

            var packsResponse = _mapper.Map<List<PackResponse>>(packs);
            return Ok(packsResponse);
        }

        [HttpGet("{guid}")]
        public async Task<IActionResult> GetPackByGuid([FromRoute] Guid guid)
        {
            var pack = await _packQueries.GetByGuidAsync(UserId, guid);

            if (pack == null)
                return NotFound();

            var packResponse = _mapper.Map<PackResponse>(pack);
            return Ok(packResponse);
        }


        [HttpPost]
        public async Task<IActionResult> CreatePack([FromBody] CreatePackRequest request)
        {
            var result = await _packService.CreatePackAsync(UserId, request);

            if( !result.IsSuccess)
            {
                return result.ErrorCode switch
                {
                    ErrorCode.InvalidData => BadRequest(new { message = result.ErrorMessage }),
                    ErrorCode.UserNotFound => NotFound(new { message = result.ErrorMessage }),
                    _ => StatusCode(500, new { message = result.ErrorMessage })
                };
            }

            var packResponse = _mapper.Map<PackResponse>(result.Value);
            return Ok(packResponse);
        }
    }
}
