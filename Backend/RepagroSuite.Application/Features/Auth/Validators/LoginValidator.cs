using FluentValidation;
using RepagroSuite.Application.Features.Auth.DTOs;

namespace RepagroSuite.Application.Features.Auth.Validators;

public class LoginValidator : AbstractValidator<LoginRequestDto>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El correo electrónico es obligatorio.")
            .Must(e => !e.Contains(' ')).WithMessage("El correo electrónico no puede contener espacios.")
            .EmailAddress().WithMessage("Ingrese un correo electrónico válido.")
            .MaximumLength(254).WithMessage("El correo electrónico no puede superar 254 caracteres.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("La contraseña es obligatoria.");
    }
}

public class ChangePasswordValidator : AbstractValidator<ChangePasswordDto>
{
    public ChangePasswordValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty().WithMessage("La contraseña actual es obligatoria.");
        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("La nueva contraseña es obligatoria.")
            .MinimumLength(8).WithMessage("La contraseña debe tener al menos 8 caracteres.")
            .Matches("[A-Z]").WithMessage("Debe contener al menos una letra mayúscula.")
            .Matches("[a-z]").WithMessage("Debe contener al menos una letra minúscula.")
            .Matches("[0-9]").WithMessage("Debe contener al menos un número.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Debe contener al menos un carácter especial.");
        RuleFor(x => x.ConfirmNewPassword)
            .Equal(x => x.NewPassword).WithMessage("Las contraseñas no coinciden.");
        RuleFor(x => x.NewPassword)
            .NotEqual(x => x.CurrentPassword).WithMessage("La nueva contraseña no puede ser igual a la actual.");
    }
}

public class ResetPasswordValidator : AbstractValidator<ResetPasswordDto>
{
    public ResetPasswordValidator()
    {
        RuleFor(x => x.Token).NotEmpty().WithMessage("El token es obligatorio.");
        RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("Correo inválido.");
        RuleFor(x => x.NewPassword)
            .NotEmpty().MinimumLength(8)
            .Matches("[A-Z]").Matches("[a-z]").Matches("[0-9]").Matches("[^a-zA-Z0-9]");
        RuleFor(x => x.ConfirmNewPassword).Equal(x => x.NewPassword).WithMessage("Las contraseñas no coinciden.");
    }
}

public class ForcedChangePasswordValidator : AbstractValidator<ForcedChangePasswordDto>
{
    public ForcedChangePasswordValidator()
    {
        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("La nueva contraseña es obligatoria.")
            .MinimumLength(8).WithMessage("La contraseña debe tener al menos 8 caracteres.")
            .Matches("[A-Z]").WithMessage("Debe contener al menos una letra mayúscula.")
            .Matches("[a-z]").WithMessage("Debe contener al menos una letra minúscula.")
            .Matches("[0-9]").WithMessage("Debe contener al menos un número.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Debe contener al menos un carácter especial.");
        RuleFor(x => x.ConfirmNewPassword).Equal(x => x.NewPassword).WithMessage("Las contraseñas no coinciden.");
    }
}
