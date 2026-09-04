using FluentValidation;
using Mnemo.Data.Entities;
using Mnemo.Shared.Enums;

namespace Mnemo.Shared.Extensions
{
    public static class FluentValidationExtension
    {
        public static async Task<BatchRequestResult<T>> ValidateBatchAsync<T>(this IValidator<T> validator, IEnumerable<T> items, ILogger? logger = null)
        {
            var itemList = items.ToList();
            int total = itemList.Count;

            logger?.LogDebug("Starting batch validation for {Count} items (Type:{ItemType})...", total, typeof(T).Name);

            var results = new List<RequestResult<T>>(total);

            foreach (var item in items)
            {
                var validationResult = await validator.ValidateAsync(item);
                if (!validationResult.IsValid)
                {
                    var messages = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                    logger?.LogWarning("Validation failed for item (Type:'{ItemType}'): {Messages}", typeof(T).Name, messages);
                    results.Add(RequestResult<T>.Failure(ErrorCode.InvalidData, messages));
                }
                else
                {
                    results.Add(RequestResult<T>.Success(item));
                }
            }

            int succeeded = results.Count(r => r.IsSuccess);
            int failed = total - succeeded;

            if (failed > 0)
            {
                logger?.LogInformation("Batch validation completed for items (Type:{ItemType}): {Succeeded} succeeded, {Failed} failed out of {Total}!",
                    typeof(T).Name, succeeded, failed, total);
            }
            else
            {
                logger?.LogDebug("Batch validation completed for items (Type:{ItemType}): all {Total} items succeeded!", typeof(T).Name, total);
            }

            return BatchRequestResult<T>.Return(results);
        }
    }
}
