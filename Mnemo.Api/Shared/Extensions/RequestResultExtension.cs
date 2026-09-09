using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Mnemo.Controllers.ActionResult;

namespace Mnemo.Shared.Extensions
{
    public static class RequestResultExtension
    {
        public static IActionResult ToActionResult<T>(this RequestResult<T> requestResult, int? successCode = null)
            => new RequestResultActionResult<T, object>(requestResult, successCode);

        public static IActionResult ToActionResult<T, TDto>(this RequestResult<T> requestResult, IMapper mapper, int? successCode = null)
            => requestResult.ToActionResult(mapper.Map<T, TDto>, successCode);

        public static IActionResult ToActionResult<T, TDto>(this RequestResult<T> requestResult, Func<T, TDto> mapFunc, int? successCode = null)
            => new RequestResultActionResult<T, TDto>(requestResult, mapFunc, successCode);
    }
}
