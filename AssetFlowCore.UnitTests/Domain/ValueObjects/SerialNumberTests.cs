using AssetFlowCore.Domain.ValueObjects;
using FluentAssertions;

namespace AssetFlowCore.UnitTests.Domain.ValueObjects;

public class SerialNumberTests
{
    [Theory]
    [InlineData("  srv-99999  ", "SRV-99999")]
    [InlineData("Network-Device-01", "NETWORK-DEVICE-01")]
    public void Create_WithValidValue_ShouldTrimAndUppercase(string input, string expected)
    {
        var serial = SerialNumber.Create(input);
        serial.Value.Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithEmptyOrNull_ShouldThrowArgumentException(string? input)
    {
        Action act = () => SerialNumber.Create(input!);
        act.Should().Throw<ArgumentException>().WithMessage("*ne peut pas être vide*");
    }

    [Theory]
    [InlineData("1234")] // Trop court
    [InlineData("this-serial-is-way-too-long-to-be-valid-more-than-50-chars")] // Trop long
    public void Create_WithInvalidLength_ShouldThrowArgumentException(string input)
    {
        Action act = () => SerialNumber.Create(input);
        act.Should().Throw<ArgumentException>().WithMessage("*entre 5 et 50 caractères*");
    }

    [Fact]
    public void Equals_WithIdenticalValues_ShouldReturnTrue()
    {
        var serialA = SerialNumber.Create("abcde12345");
        var serialB = SerialNumber.Create("ABCDE12345");

        serialA.Should().Be(serialB);
        (serialA == serialB).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentValues_ShouldReturnFalse()
    {
        var serialA = SerialNumber.Create("AAAAA12345");
        var serialB = SerialNumber.Create("BBBBB12345");

        serialA.Should().NotBe(serialB);
        (serialA != serialB).Should().BeTrue();
    }
}