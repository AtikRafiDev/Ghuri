using Ghuri.Domain.ValueObjects;

namespace Ghuri.Domain.Tests.ValueObjects;

public class PhoneNumberTests
{
    [Theory]
    [InlineData("01712345678", "+8801712345678")] // local format
    [InlineData("8801712345678", "+8801712345678")] // no plus sign
    [InlineData("+8801712345678", "+8801712345678")] // already normalized
    [InlineData("+880 171-234-5678", "+8801712345678")] // spaces and dashes
    public void Create_WithAcceptedFormat_NormalizesToPlus880Form(string input, string expected)
    {
        var phone = PhoneNumber.Create(input);

        Assert.Equal(expected, phone.Value);
    }

    [Theory]
    [InlineData("01212345678")] // "2" is not a real operator prefix digit
    [InlineData("0171234567")]  // too short (10 digits)
    [InlineData("017123456789")] // too long (12 digits)
    [InlineData("not-a-phone")]
    [InlineData("")]
    public void IsValid_WithBadInput_ReturnsFalse(string input)
    {
        Assert.False(PhoneNumber.IsValid(input));
    }

    [Fact]
    public void Create_WithInvalidInput_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => PhoneNumber.Create("not-a-phone"));
    }

    [Fact]
    public void TwoPhoneNumbers_WithSameValue_AreEqual()
    {
        var a = PhoneNumber.Create("01712345678");
        var b = PhoneNumber.Create("+8801712345678");

        Assert.Equal(a, b);
    }
}
