using FluentValidation;
using Ghuri.Domain.Entities.Iam;

namespace Ghuri.Application.Features.Staff.Commands.ChangeStaffRole;

internal sealed class ChangeStaffRoleValidator : AbstractValidator<ChangeStaffRoleCommand>
{
    public ChangeStaffRoleValidator()
    {
        RuleFor(x => x.Role).Must(User.AssignableStaffRoles.Contains).WithMessage("Choose Manager, Sales or Accounts.");
    }
}
