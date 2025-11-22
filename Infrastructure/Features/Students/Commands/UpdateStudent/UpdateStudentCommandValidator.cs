using FluentValidation;

namespace Infrastructure.Features.Students.Commands.UpdateStudent;

public class UpdateStudentCommandValidator : AbstractValidator<UpdateStudentCommand>
{
    public UpdateStudentCommandValidator()
    {
        RuleFor(x => x.Dto.FullName)
            .NotEmpty().WithMessage("ФИО обязательно для заполнения.")
            .MaximumLength(100).WithMessage("ФИО не должно превышать 100 символов.");

        RuleFor(x => x.Dto.Email)
            .NotEmpty().WithMessage("Email обязателен для заполнения.")
            .EmailAddress().WithMessage("Требуется действительный Email.");

        RuleFor(x => x.Dto.PhoneNumber)
            .NotEmpty().WithMessage("Номер телефона обязателен для заполнения.");

        RuleFor(x => x.Dto.Birthday)
            .LessThan(DateTime.Now).WithMessage("Дата рождения должна быть в прошлом.");

        RuleFor(x => x.Dto.Gender)
            .IsInEnum().WithMessage("Неверный пол.");
    }
}
