using FluentValidation;
using Mnemo.Contracts.Pack.Requests;

namespace Mnemo.Controllers.Validation
{
    public class CreatePackRequestValidator : AbstractValidator<CreatePackRequest>
    {
        public CreatePackRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Pack name is required")
                .Must(VocabularyPackRules.IsValidName)
                .WithMessage("Lorem Ipsum Name");

            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("Pack name is required")
                .Must(VocabularyPackRules.IsValidDescription)
                .WithMessage("Lorem Ipsum Description");
        }
    }
}
