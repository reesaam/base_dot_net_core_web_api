using BaseWebApi.Application.DTOs.Items;
using FluentValidation;

namespace BaseWebApi.Application.Validators.Items;

public sealed class CreateItemRequestValidator : AbstractValidator<CreateItemRequestDto>
{
    public CreateItemRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .MaximumLength(2000)
            .When(x => x.Description is not null);

        RuleFor(x => x.Priority)
            .Must(p => string.IsNullOrWhiteSpace(p)
                       || Enum.TryParse<Domain.Enums.Priority>(p, ignoreCase: true, out _))
            .WithMessage("Priority must be Low, Medium, High, or Critical.");
    }
}

public sealed class UpdateItemRequestValidator : AbstractValidator<UpdateItemRequestDto>
{
    public UpdateItemRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Status)
            .NotEmpty()
            .Must(s => Enum.TryParse<Domain.Enums.Status>(s, ignoreCase: true, out _))
            .WithMessage("Status is invalid.");

        RuleFor(x => x.Priority)
            .NotEmpty()
            .Must(p => Enum.TryParse<Domain.Enums.Priority>(p, ignoreCase: true, out _))
            .WithMessage("Priority is invalid.");
    }
}
