using FluentValidation;
using Mnemo.Contracts.Pack.Requests;

namespace Mnemo.Controllers.Validation
{
    public class CreateVocabularyRequestValidator : AbstractValidator<CreateVocabularyRequest>
    {
        public CreateVocabularyRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Pack name is required")
                .Must(VocabularyRules.IsValidName)
                .WithMessage("Lorem Ipsum Name");

            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("Pack name is required")
                .Must(VocabularyRules.IsValidDescription)
                .WithMessage("Lorem Ipsum Description");
        }
    }
}
