using FluentValidation;
using Mnemo.Contracts.Vocabulary.Requests;
using Mnemo.Shared.Enums;

namespace Mnemo.Controllers.Validation
{
    public class CreateEntryRequestValidator : AbstractValidator<CreateEntryRequest>
    {
        public CreateEntryRequestValidator()
        {
            RuleFor(x => x.PartOfSpeech)
                .Must(VocabularyValidatorRules.IsValidPartOfSpeech)
                .When(x => x.PartOfSpeech != null)
                .WithMessage($"Invalid Part of Speech. Allowed values: {string.Join(", ", Enum.GetNames<PartOfSpeech>())}");

            RuleFor(x => x.CERF)
                .Must(VocabularyValidatorRules.IsValidCEFRLevel)
                .When(x => x.CERF != null)
                .WithMessage($"Invalid CEFR level. Allowed values: {string.Join(", ", Enum.GetNames<CEFRLevel>())}");

            RuleFor(x => x.Foreign)
                .NotEmpty().WithMessage("Foreign word is required")
                .Must(VocabularyValidatorRules.IsValidForeign)
                .WithMessage("Foreign must contain only letters (no digits or special chars)");

            RuleFor(x => x.Transcription)
                .Must(VocabularyValidatorRules.IsValidTranscription)
                .When(x => x.Transcription != null)
                .WithMessage("Transcription format is invalid");

            RuleFor(x => x.Translations)
                .NotEmpty().WithMessage("At least one translation is required")
                .ForEach(t => t.Must(VocabularyValidatorRules.IsValidTranslation)
                    .WithMessage("Each translation must be valid (no digits, must contain letters)"));

            RuleForEach(x => x.Examples)
                .Must(VocabularyValidatorRules.IsValidExample)
                .When(x => x.Examples != null)
                .WithMessage("Example too short or invalid");
        }
    }
}
