using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Mnemo.Shared;
using Mnemo.Shared.Enums;
using Mnemo.Shared.Extensions;

namespace Mnemo.Controllers.ActionResult
{
    public class RequestResultActionResult<T, TDto> : IActionResult
    {
        private readonly RequestResult<T> _result;
        private readonly Func<T, TDto>? _mapFunc;
        private readonly int _successStatusCode = StatusCodes.Status200OK;


        public RequestResultActionResult(RequestResult<T> result, int? successCode = null)
        {
            _result = result;
            _successStatusCode = successCode ?? _successStatusCode;
        }

        public RequestResultActionResult(RequestResult<T> result, Func<T, TDto> mapFunc, int? successCode = null)
        {
            _result = result;
            _mapFunc = mapFunc;
            _successStatusCode = successCode ?? _successStatusCode;
        }


        public async Task ExecuteResultAsync(ActionContext context)
        {
            var response = context.HttpContext.Response;
            var jsonOptions = context.HttpContext.RequestServices.GetRequiredService<IOptions<JsonOptions>>().Value;

            if (_result.IsSuccess)
            {
                response.StatusCode = _successStatusCode;

                if (_mapFunc != null)
                {
                    var mappedValue = _mapFunc(_result.Value!);
                    await response.WriteAsJsonAsync(mappedValue, jsonOptions.JsonSerializerOptions);
                }
                else
                {
                    await response.WriteAsJsonAsync(_result.Value, jsonOptions.JsonSerializerOptions);
                }
            }
            else
            {
                response.StatusCode = _result.ErrorCode switch
                {
                    // BadRequest
                    ErrorCode.InvalidData       => StatusCodes.Status400BadRequest,
                    ErrorCode.InvalidPassword   => StatusCodes.Status400BadRequest,
                    // NotFound
                    ErrorCode.UserNotFound          => StatusCodes.Status404NotFound,
                    ErrorCode.VocabularyNotFound    => StatusCodes.Status404NotFound,
                    ErrorCode.EntryNotFound         => StatusCodes.Status404NotFound,
                    ErrorCode.StateNotFound         => StatusCodes.Status404NotFound,
                    ErrorCode.RepetitionNotFound    => StatusCodes.Status404NotFound,
                    ErrorCode.TaskNotFound          => StatusCodes.Status404NotFound,
                    // Conflict/Duplicate
                    ErrorCode.UsernameTaken     => StatusCodes.Status409Conflict,
                    ErrorCode.DuplicateEntry    => StatusCodes.Status409Conflict,
                    // UnprocessableEntity
                    ErrorCode.TaskGenerationFailed      => StatusCodes.Status422UnprocessableEntity,
                    ErrorCode.ExternalDictionaryError   => StatusCodes.Status422UnprocessableEntity,
                    _ => StatusCodes.Status418ImATeapot
                };

                var errorResponse = new { errorCode = _result.ErrorCode, message = _result.ErrorMessage };
                await response.WriteAsJsonAsync(errorResponse, jsonOptions.JsonSerializerOptions);
            }
        }
    }
}
