using FluentValidation;

namespace GuidanceDepartmentMain.Contracts;

public sealed class StudentRequestValidator : AbstractValidator<StudentRequestDto>
{
    public StudentRequestValidator()
    {
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Details).NotEmpty().MaximumLength(10_000);
        RuleFor(x => x.SafetyValveText).MaximumLength(10_000);
        RuleFor(x => x.Urgency).IsInEnum();
    }
}