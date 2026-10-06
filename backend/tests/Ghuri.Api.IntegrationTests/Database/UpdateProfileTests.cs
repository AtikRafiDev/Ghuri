using Ghuri.Application.Abstractions.Ports;
using Ghuri.Application.Features.Identity.Commands.UpdateProfile;
using Ghuri.Domain.Entities.Iam;
using Ghuri.Domain.ValueObjects;
using Ghuri.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Ghuri.Api.IntegrationTests.Database;

/// <summary>
/// A user editing their own profile (17-day plan, Day 11: UpdateProfile).
/// Security: the email - where password-reset links go - only changes
/// with the current password.
/// </summary>
public class UpdateProfileTests(SqlServerFixture sql) : IClassFixture<SqlServerFixture>
{
    private const string Password = "rahim-password";

    private async Task<User> CustomerWithPasswordAsync(string? email = null)
    {
        var hasher = sql.Services.GetRequiredService<IPasswordHasher>();
        var phone = PhoneNumber.Create("018" + Random.Shared.Next(0, 100_000_000).ToString("D8"));
        var user = User.Create("Rahim Uddin", phone, email, hasher.Hash(Password));
        await sql.SaveAsync(user);
        return user;
    }

    private async Task<User> ReloadAsync(Guid id)
    {
        await using var scope = sql.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<AppDbContext>().Users.AsNoTracking().SingleAsync(u => u.Id == id);
    }

    [Fact]
    public async Task ChangingOnlyTheName_NeedsNoPassword()
    {
        var user = await CustomerWithPasswordAsync("rahim1@example.com");

        var result = await sql.SendCommandAsync(new UpdateProfileCommand("  Rahim Uddin Ahmed ", "RAHIM1@example.com", null), user.Id);

        Assert.True(result.IsSuccess, result.Error.Message); // the same email in other letters isn't a change
        Assert.Equal("Rahim Uddin Ahmed", (await ReloadAsync(user.Id)).FullName);
    }

    [Fact]
    public async Task ChangingTheEmail_WithoutTheRightPassword_IsRefused_AndCounted()
    {
        var user = await CustomerWithPasswordAsync("rahim2@example.com");

        var noPassword = await sql.SendCommandAsync(new UpdateProfileCommand("Rahim Uddin", "thief@example.com", null), user.Id);
        var wrong = await sql.SendCommandAsync(new UpdateProfileCommand("Rahim Uddin", "thief@example.com", "guess"), user.Id);

        Assert.Equal(("current_password_wrong", "current_password_wrong"), (noPassword.Error.Code, wrong.Error.Code));
        var saved = await ReloadAsync(user.Id);
        Assert.Equal(("rahim2@example.com", (byte)2), (saved.Email, saved.AccessFailedCount)); // unchanged, and both guesses count
    }

    [Fact]
    public async Task ChangingTheEmail_WithThePassword_SavesIt_AsNotYetConfirmed()
    {
        var user = await CustomerWithPasswordAsync();

        var result = await sql.SendCommandAsync(new UpdateProfileCommand("Rahim Uddin", " rahim.new@example.com ", Password), user.Id);

        Assert.True(result.IsSuccess, result.Error.Message);
        var saved = await ReloadAsync(user.Id);
        Assert.Equal(("rahim.new@example.com", "RAHIM.NEW@EXAMPLE.COM", false), (saved.Email, saved.NormalizedEmail, saved.EmailConfirmed));
    }

    [Fact]
    public async Task AnEmailSomeoneElseHas_IsRefused()
    {
        await CustomerWithPasswordAsync("taken@example.com");
        var user = await CustomerWithPasswordAsync();

        var result = await sql.SendCommandAsync(new UpdateProfileCommand("Rahim Uddin", "Taken@Example.com", Password), user.Id);

        Assert.Equal("email_taken", result.Error.Code);
    }

    [Fact]
    public async Task AnEmptyName_IsAValidationError()
    {
        var user = await CustomerWithPasswordAsync();

        var result = await sql.SendCommandAsync(new UpdateProfileCommand("  ", null, null), user.Id);

        Assert.Equal("validation_failed", result.Error.Code);
    }
}
