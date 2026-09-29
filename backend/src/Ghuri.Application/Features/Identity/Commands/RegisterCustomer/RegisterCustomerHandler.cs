using Ghuri.Application.Abstractions.Messaging;
using Ghuri.Application.Abstractions.Ports;
using Ghuri.Application.Common;
using Ghuri.Domain.Entities.Iam;
using Ghuri.Domain.Enums;
using Ghuri.Domain.Repositories;
using Ghuri.Domain.ValueObjects;

namespace Ghuri.Application.Features.Identity.Commands.RegisterCustomer;

internal sealed class RegisterCustomerHandler(
    IUserRepository users,
    IPasswordHasher passwords,
    SessionIssuer sessions,
    TimeProvider clock) : ICommandHandler<RegisterCustomerCommand, AuthTokens>
{
    public async ValueTask<Result<AuthTokens>> Handle(RegisterCustomerCommand command, CancellationToken cancellationToken)
    {
        var phone = PhoneNumber.Create(command.Phone); // safe: the validator already checked it
        if (await users.PhoneExistsAsync(phone, cancellationToken))
            return IdentityErrors.PhoneTaken;

        var email = string.IsNullOrWhiteSpace(command.Email) ? null : command.Email.Trim();
        if (email is not null && await users.EmailExistsAsync(email, cancellationToken))
            return IdentityErrors.EmailTaken;

        // (Two people registering the same phone in the same millisecond
        // would both pass the check above - the database's unique index on
        // PhoneNumber still stops the second one.)

        var nowUtc = clock.GetUtcNow().UtcDateTime;
        var user = User.Create(command.FullName.Trim(), phone, email, passwords.Hash(command.Password));
        user.AssignRole(SystemRole.Customer, nowUtc);
        user.RecordSuccessfulLogin(nowUtc); // they're logged in from the first moment
        users.Add(user);

        return sessions.Issue(user, familyId: Guid.CreateVersion7(), nowUtc);
    }
}
