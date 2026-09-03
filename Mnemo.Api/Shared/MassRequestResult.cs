using Mnemo.Shared.Enums;

namespace Mnemo.Shared
{
    public class MassRequestResult<T>
    {
        public int Succeeded => Results.Count(r => r.IsSuccess);
        public int Total => Results.Count;
        public int Failed => Total - Succeeded;

        public List<RequestResult<T>> Results { get; }
        public List<T> SucceededValues => Results.Where(r => r.IsSuccess).Select(r => r.Value!).ToList();


        public MassRequestResult(IEnumerable<RequestResult<T>> results)
        {
            Results = results.ToList();
        }


        public static MassRequestResult<T> PartialSuccess(List<RequestResult<T>> results) => new MassRequestResult<T>(results);
        public static MassRequestResult<T> AbsolutelyFailure(int count, ErrorCode errorCode, string? errorMessage = null) => new MassRequestResult<T>(Enumerable.Repeat(RequestResult<T>.Failure(errorCode, errorMessage), count));
    }
}
