using Mnemo.Shared.Enums;

namespace Mnemo.Shared
{
    public class BatchRequestResult<T>
    {
        public bool IsAllFailure { get; }

        public List<RequestResult<T>> Results { get; }
        public List<RequestResult<T>> SucceededResults => Results.Where(r => r.IsSuccess).ToList();
        public List<RequestResult<T>> FailedResults => Results.Where(r => !r.IsSuccess).ToList();


        public BatchRequestResult(List<RequestResult<T>> results)
        {
            IsAllFailure = !results.Any(r => r.IsSuccess);
            Results = results;
        }

        public BatchRequestResult(ErrorCode errorCode, string? errorMessage)
        {
            IsAllFailure = true;
            Results = [RequestResult<T>.Failure(errorCode, errorMessage)];
        }


        public static BatchRequestResult<T> Return(List<RequestResult<T>> results) => new BatchRequestResult<T>(results);
        public static BatchRequestResult<T> CriticalFailure(ErrorCode errorCode, string? errorMessage = null) => new BatchRequestResult<T>(errorCode, errorMessage);
    }
}
