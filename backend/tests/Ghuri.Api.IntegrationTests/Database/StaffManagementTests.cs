using System.Net;
using System.Net.Http.Headers;
using Ghuri.Application.Abstractions.Ports;
using Ghuri.Application.Features.Identity.Commands.ForgotPassword;
using Ghuri.Application.Features.Identity.Commands.Login;
using Ghuri.Application.Features.Identity.Commands.RefreshSession;
using Ghuri.Application.Features.Identity.Commands.ResetPassword;
using Ghuri.Application.Features.Staff.Commands.ChangeStaffRole;
using Ghuri.Application.Features.Staff.Commands.CreateStaffUser;
using Ghuri.Application.Features.Staff.Commands.DisableStaffUser;
using Ghuri.Application.Features.Staff.Commands.EnableStaffUser;
using Ghuri.Application.Features.Staff.Commands.SendStaffPasswordLink;
using Ghuri.Application.Features.Staff.Queries.GetStaffUsers;
using Ghuri.Domain.Entities.Iam;
using Ghuri.Domain.Enums;
using Ghuri.Domain.ValueObjects;
using Ghuri.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Ghuri.Api.IntegrationTests.Database;

/// <summary>
/// Admin → Staff: the Super Admin makes Manager / Sales / Accounts accounts.
/// Customers sign up themselves; staff are only made here, and set their
/// own password from an emailed link.
/// </summary>
public class StaffManagementTests(SqlServerFixture sql) : IClassFixture<SqlServerFixture>
{
    private const string Password = "staff-password-1";

    private static string NewPhone() => "016" + Random.Shared.Next(0, 100_000_000).ToString("D8");

    private static string NewEmail() => $"{Guid.NewGuid():N}@ghuri.local";

    private async Task<(Guid Id, string Phone, string Email)> CreateAsync(SystemRole role = SystemRole.Sales)
    {
        var (phone, email) = (NewPhone(), NewEmail());
        var result = await sql.SendCommandAsync(new CreateStaffUserCommand("Karim Sales", phone, email, role), asUser: null);
        Assert.True(result.IsSuccess, result.Error.Message);
        return (result.Value, phone, email);
    }

    private async Task<User> ReloadAsync(Guid id)
    {
        await using var scope = sql.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<AppDbContext>().Users
            .Include(u => u.Roles).AsNoTracking().SingleAsync(u => u.Id == id, TestContext.Current.CancellationToken);
    }

    /// <summary>The token inside the newest "set your password" link sent to this address.</summary>
    private string TokenEmailedTo(string email)
    {
        var message = sql.Emails.Sent.Last(m => m.To == email);
        var link = message.HtmlBody[(message.HtmlBody.IndexOf("token=", StringComparison.Ordinal) + "token=".Length)..];
        return Uri.UnescapeDataString(link[..link.IndexOf('"')]);
    }

    private async Task<User> SaveAsync(User user)
    {
        await sql.SaveAsync(user);
        return user;
    }

    [Fact]
    public async Task Create_MakesAStaffAccount_WithOnlyThatRole_AndNoPassword_AndEmailsALink()
    {
        var (id, _, email) = await CreateAsync(SystemRole.Accounts);

        var saved = await ReloadAsync(id);
        Assert.Equal([(byte)SystemRole.Accounts], saved.Roles.Select(r => r.RoleId)); // no Customer role
        Assert.Null(saved.PasswordHash); // the admin never sets or sees it
        Assert.Equal(UserStatus.Active, saved.Status);

        var welcome = sql.Emails.Sent.Last(m => m.To == email);
        Assert.Contains("/reset-password?email=", welcome.HtmlBody);
        Assert.Contains("Accounts", welcome.HtmlBody);
    }

    [Fact]
    public async Task TheWelcomeLink_SetsThePassword_ThenTheyCanLogIn()
    {
        var (_, phone, email) = await CreateAsync();

        var reset = await sql.SendCommandAsync(new ResetPasswordCommand(email, TokenEmailedTo(email), Password));
        var login = await sql.SendCommandAsync(new LoginCommand(phone, Password), asUser: null);

        Assert.True(reset.IsSuccess, reset.Error.Message);
        Assert.True(login.IsSuccess, login.Error.Message);
    }

    [Fact]
    public async Task ForgotPassword_RefusesTheCurrentPassword_ButTakesANewOne()
    {
        var (_, phone, email) = await CreateAsync();
        await sql.SendCommandAsync(new ResetPasswordCommand(email, TokenEmailedTo(email), Password));
        await sql.SendCommandAsync(new ForgotPasswordCommand(email), asUser: null);
        var token = TokenEmailedTo(email);

        var same = await sql.SendCommandAsync(new ResetPasswordCommand(email, token, Password), asUser: null);
        var fresh = await sql.SendCommandAsync(new ResetPasswordCommand(email, token, "another-password"), asUser: null); // same link: not used up
        var login = await sql.SendCommandAsync(new LoginCommand(phone, "another-password"), asUser: null);

        Assert.Equal("new_password_same_as_old", same.Error.Code);
        Assert.True(fresh.IsSuccess, fresh.Error.Message);
        Assert.True(login.IsSuccess, login.Error.Message);
    }

    [Fact]
    public async Task APhoneOrEmailThatAlreadyHasAnAccount_IsRefused()
    {
        var customer = await sql.NewCustomerUserAsync();
        var (_, _, takenEmail) = await CreateAsync();

        var samePhone = await sql.SendCommandAsync(
            new CreateStaffUserCommand("Someone", customer.PhoneNumber.Value, NewEmail(), SystemRole.Sales), asUser: null);
        var sameEmail = await sql.SendCommandAsync(
            new CreateStaffUserCommand("Someone", NewPhone(), takenEmail.ToUpperInvariant(), SystemRole.Sales), asUser: null);

        Assert.Equal(("phone_taken", "email_taken"), (samePhone.Error.Code, sameEmail.Error.Code));
    }

    [Theory]
    [InlineData(SystemRole.SuperAdmin)]
    [InlineData(SystemRole.Customer)]
    public async Task OnlyManagerSalesOrAccounts_CanBeGiven(SystemRole role)
    {
        var result = await sql.SendCommandAsync(new CreateStaffUserCommand("Someone", NewPhone(), NewEmail(), role), asUser: null);

        Assert.Equal("validation_failed", result.Error.Code);
    }

    [Fact]
    public async Task IfTheEmailCantBeSent_NoAccountIsMade()
    {
        var phone = NewPhone();
        sql.Emails.FailWith = new InvalidOperationException("Mail server down");
        try
        {
            await Assert.ThrowsAnyAsync<Exception>(() =>
                sql.SendCommandAsync(new CreateStaffUserCommand("Someone", phone, NewEmail(), SystemRole.Sales), asUser: null));
        }
        finally
        {
            sql.Emails.FailWith = null;
        }

        await using var scope = sql.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.False(await db.Users.AnyAsync(u => u.PhoneNumber == PhoneNumber.Create(phone), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ChangeRole_ReplacesTheOldRole()
    {
        var (id, _, _) = await CreateAsync(SystemRole.Sales);

        var result = await sql.SendCommandAsync(new ChangeStaffRoleCommand(id, SystemRole.Manager));

        Assert.True(result.IsSuccess, result.Error.Message);
        Assert.Equal([(byte)SystemRole.Manager], (await ReloadAsync(id)).Roles.Select(r => r.RoleId));
    }

    [Fact]
    public async Task Disable_EndsTheirSessions_AndRefusesLogin_UntilEnabledAgain()
    {
        var (id, phone, email) = await CreateAsync();
        await sql.SendCommandAsync(new ResetPasswordCommand(email, TokenEmailedTo(email), Password));
        var session = await sql.SendCommandAsync(new LoginCommand(phone, Password), asUser: null);

        var disabled = await sql.SendCommandAsync(new DisableStaffUserCommand(id));
        var loginWhileDisabled = await sql.SendCommandAsync(new LoginCommand(phone, Password), asUser: null);
        var refreshWhileDisabled = await sql.SendCommandAsync(new RefreshSessionCommand(session.Value.RefreshToken), asUser: null);
        var enabled = await sql.SendCommandAsync(new EnableStaffUserCommand(id));
        var loginAgain = await sql.SendCommandAsync(new LoginCommand(phone, Password), asUser: null);

        Assert.True(disabled.IsSuccess, disabled.Error.Message);
        Assert.Equal("account_disabled", loginWhileDisabled.Error.Code);
        Assert.Equal("session_expired", refreshWhileDisabled.Error.Code);
        Assert.True(enabled.IsSuccess, enabled.Error.Message);
        Assert.True(loginAgain.IsSuccess, loginAgain.Error.Message);
    }

    [Fact]
    public async Task TheSuperAdmin_AndCustomers_CantBeChangedHere()
    {
        var owner = User.Create("Owner", PhoneNumber.Create(NewPhone()), NewEmail(), passwordHash: null);
        owner.AssignRole(SystemRole.SuperAdmin, DateTime.UtcNow);
        await SaveAsync(owner);
        var customer = await sql.NewCustomerUserAsync();

        var disableOwner = await sql.SendCommandAsync(new DisableStaffUserCommand(owner.Id));
        var promoteCustomer = await sql.SendCommandAsync(new ChangeStaffRoleCommand(customer.Id, SystemRole.Manager));

        Assert.Equal(("super_admin_protected", "staff_not_found"), (disableOwner.Error.Code, promoteCustomer.Error.Code));
    }

    [Fact]
    public async Task SendPasswordLink_EmailsANewLink_OnlyWhileActive()
    {
        var (id, _, email) = await CreateAsync();
        var firstToken = TokenEmailedTo(email);

        var resent = await sql.SendCommandAsync(new SendStaffPasswordLinkCommand(id));
        await sql.SendCommandAsync(new DisableStaffUserCommand(id));
        var whileDisabled = await sql.SendCommandAsync(new SendStaffPasswordLinkCommand(id));

        Assert.True(resent.IsSuccess, resent.Error.Message);
        Assert.NotEqual(firstToken, TokenEmailedTo(email));
        Assert.Equal("staff_disabled", whileDisabled.Error.Code);
    }

    [Fact]
    public async Task TheList_ShowsStaff_NotCustomers()
    {
        var (staffId, _, _) = await CreateAsync(SystemRole.Manager);
        var customer = await sql.NewCustomerAsync();

        var list = await sql.SendAsync(new GetStaffUsersQuery());

        var row = Assert.Single(list.Value, s => s.Id == staffId);
        Assert.Equal((SystemRole.Manager, false, true), (row.Role, row.PasswordSet, row.Editable));
        Assert.DoesNotContain(list.Value, s => s.Id == customer);
    }

    // ---------- Who may manage staff (HTTP, real tokens) ----------

    [Theory]
    [InlineData(SystemRole.SuperAdmin, HttpStatusCode.OK)]
    [InlineData(SystemRole.Manager, HttpStatusCode.Forbidden)] // whoever hands out roles can give themselves any power
    [InlineData(SystemRole.Accounts, HttpStatusCode.Forbidden)]
    [InlineData(SystemRole.Customer, HttpStatusCode.Forbidden)]
    public async Task OnlyTheSuperAdmin_CanOpenTheStaffPage(SystemRole role, HttpStatusCode expected)
    {
        var user = User.Create($"{role} person", PhoneNumber.Create(NewPhone()), NewEmail(), passwordHash: null);
        user.AssignRole(role, DateTime.UtcNow);
        await SaveAsync(user);

        var client = sql.CreateClient();
        var token = sql.Services.GetRequiredService<ITokenService>().CreateAccessToken(user);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Value);

        var response = await client.GetAsync("/api/v1/admin/staff", TestContext.Current.CancellationToken);

        Assert.Equal(expected, response.StatusCode);
    }
}
