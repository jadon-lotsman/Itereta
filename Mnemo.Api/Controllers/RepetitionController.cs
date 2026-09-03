using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mnemo.Contracts.Repetition;
using Mnemo.Contracts.Repetition.Requests;
using Mnemo.Data.Queries;
using Mnemo.Services.RepetitionService;
using Mnemo.Shared.Enums;
using System.Security.Claims;

namespace Mnemo.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class RepetitionController : ControllerBase
    {
        private readonly IMapper _mapper;
        private readonly TaskQueries _taskQueries;
        private readonly RepetitionTaskService _taskService;
        private readonly StateManagementService _stateService;


        public RepetitionController(IMapper mapper, TaskQueries sessionQueries, RepetitionTaskService taskService, StateManagementService stateService)
        {
            _mapper = mapper;
            _taskQueries = sessionQueries;
            _taskService = taskService;
            _stateService = stateService;
        }

        private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));



        [HttpGet]
        public async Task<IActionResult> ExistsRepetitionStatus()
        {
            var result = await _taskService.ExistsRepetitionAsync(UserId);

            if (!result.IsSuccess)
            {
                return result.ErrorCode switch
                {
                    ErrorCode.UserNotFound => NotFound(new { message = result.ErrorMessage }),
                    _ => StatusCode(500, new { message = result.ErrorMessage })
                };
            }

            return Ok(new { inProcess = result.Value });
        }

        [HttpPost]
        public async Task<IActionResult> StartRepetitionSession([FromQuery] string mode)
        {
            var result = await _taskService.StartRepetitionAsync(UserId, mode);

            if (!result.IsSuccess)
            {
                return result.ErrorCode switch
                {
                    ErrorCode.InvalidData => BadRequest(new { message = result.ErrorMessage }),
                    ErrorCode.UserNotFound => NotFound(new { message = result.ErrorMessage }),
                    ErrorCode.TaskGenerationFailed => UnprocessableEntity(new { message = result.ErrorMessage }),
                    _ => StatusCode(500, new { message = result.ErrorMessage })
                };
            }

            var tasks = result.Value;
            var tasksResponse = _mapper.Map<List<TaskResponse>>(tasks);
            return Ok(tasksResponse);
        }

        [HttpDelete]
        public async Task<IActionResult> FinishRepetitionSession()
        {
            var result = await _taskService.FinishRepetitionAsync(UserId);

            if (!result.IsSuccess)
            {
                return result.ErrorCode switch
                {
                    ErrorCode.InvalidData => BadRequest(new { message = result.ErrorMessage }),
                    ErrorCode.StateNotFound => NotFound(new { message = result.ErrorMessage }),
                    ErrorCode.RepetitionNotFound => NotFound(new { message = result.ErrorMessage }),
                    _ => StatusCode(500, new { message = result.ErrorMessage })
                };
            }

            return Ok(result.Value);
        }



        [HttpGet("states/")]
        public async Task<IActionResult> GetRepetitionSchedule()
        {
            var days = await _stateService.GetRepetitionScheduleAsync(UserId);

            return Ok(days);
        }



        [HttpGet("tasks")]
        public async Task<IActionResult> GetAllTasks()
        {
            var tasks = await _taskQueries.GetByUserIdQuery(UserId).ToListAsync();


            var tasksResponse = _mapper.Map<List<TaskResponse>>(tasks);
            return Ok(tasksResponse);
        }

        [HttpGet("tasks/{id:int}")]
        public async Task<IActionResult> GetTaskById(int id)
        {
            var task = await _taskQueries.GetTaskByIdAsync(UserId, id);

            if (task == null)
                return NotFound();

            var taskResponse = _mapper.Map<TaskResponse>(task);
            return Ok(taskResponse);
        }

        [HttpPost("tasks/{id:int}")]
        public async Task<IActionResult> SubmitTaskAnswer(int id, [FromBody] SubmitTaskRequest request)
        {
            var result = await _taskService.SubmitRepetitionTaskAnswerAsync(UserId, id, request.UserAnswer, TimeSpan.FromMilliseconds(request.ElapsedTimeMilliseconds));

            if (!result.IsSuccess)
            {
                return result.ErrorCode switch
                {
                    ErrorCode.TaskNotFound => NotFound(new { message = result.ErrorMessage }),
                    _ => StatusCode(500, new { message = result.ErrorMessage })
                };
            }

            var task = result.Value;
            var taskResponse = _mapper.Map<TaskResponse>(task);
            return Ok(taskResponse);
        }
    }
}
