using FluentValidation;
using Mnemo.Contracts.Vocabulary.Requests;

namespace Mnemo.Controllers.Validation
{
    public class CreateVocabularyRequestValidator : AbstractValidator<CreateVocabularyRequest>
    {
        public CreateVocabularyRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Vocabulary name is required")
                .Must(VocabularyRules.IsValidName)
                .WithMessage("Vocabulary name is required");

            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("Vocabulary description is required")
                .Must(VocabularyRules.IsValidDescription)
                .WithMessage("Vocabulary description is required");
        }
    }
}
