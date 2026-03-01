using AIMS.ViewModels.Systems;
using FluentValidation;

namespace AIMS.BackendServer.Validators;

public class CreateRoleRequestValidator : AbstractValidator<CreateRoleRequest>
{
    public CreateRoleRequestValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id không được để trống")
            .MaximumLength(50).WithMessage("Id tối đa 50 ký tự")
            .Matches("^[A-Z_]+$").WithMessage("Id chỉ dùng chữ hoa và dấu gạch dưới. VD: ADMIN, HR_MANAGER");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên role không được để trống")
            .MaximumLength(100).WithMessage("Tên tối đa 100 ký tự");
    }
}