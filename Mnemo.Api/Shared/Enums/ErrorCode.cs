namespace Mnemo.Shared.Enums
{
    public enum ErrorCode
    {
        // 400 BadRequest
        InvalidData,
        InvalidPassword,

        // 403 Forbidden
        AccessDenied,

        // 404 NotFound
        UserNotFound,
        VocabularyNotFound,
        EntryNotFound,
        StateNotFound,
        TaskNotFound,
        RepetitionNotFound,

        // 409 Conflict/Dublicate
        UsernameTaken,
        DuplicateEntry,

        // 422 UnprocessableEntity
        TaskGenerationFailed,
        ExternalDictionaryError,
    }
}
