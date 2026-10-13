using BenefitsStreamliner.Core.Rules;

namespace BenefitsStreamliner.Tests;

public class SinValidatorTests
{
    [Theory]
    [InlineData("046454286")]
    [InlineData("046-454-286")]
    [InlineData("046 454 286")]
    [InlineData("130692544")]
    public void Accepts_sins_that_pass_the_luhn_check(string sin) =>
        Assert.True(SinValidator.IsValid(sin));

    [Theory]
    [InlineData("046454287")]     // last digit wrong, fails Luhn
    [InlineData("123456789")]     // right length, fails Luhn
    [InlineData("12345678")]      // too short
    [InlineData("1234567890")]    // too long
    [InlineData("abcdefghi")]
    [InlineData("")]
    public void Rejects_everything_else(string sin) =>
        Assert.False(SinValidator.IsValid(sin));
}
