using MovieDatabase.Api.Core.Utils;

using Shouldly;

namespace MovieDatabase.UnitTests.Core.Utils;

public class PasswordUtilsTests
{
    [Fact]
    public void HashPassword_ShouldReturnNonEmptyHash()
    {
        // Arrange
        const string password = "TestPassword123!";

        // Act
        var hashedPassword = PasswordUtils.HashPassword(password);

        // Assert
        hashedPassword.ShouldNotBeNullOrEmpty();
        hashedPassword.ShouldNotBe(password, "hashed password should differ from original");
    }

    [Fact]
    public void HashPassword_ShouldReturnDifferentHashesForSamePassword()
    {
        // Arrange
        const string password = "TestPassword123!";

        // Act
        var hash1 = PasswordUtils.HashPassword(password);
        var hash2 = PasswordUtils.HashPassword(password);

        // Assert
        hash1.ShouldNotBe(hash2, "BCrypt should use different salts for each hash");
    }

    [Fact]
    public void VerifyPassword_WithCorrectPassword_ShouldReturnTrue()
    {
        // Arrange
        const string password = "TestPassword123!";
        var hashedPassword = PasswordUtils.HashPassword(password);

        // Act
        var result = PasswordUtils.VerifyPassword(password, hashedPassword);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void VerifyPassword_WithIncorrectPassword_ShouldReturnFalse()
    {
        // Arrange
        const string correctPassword = "CorrectPassword123!";
        const string incorrectPassword = "WrongPassword456!";
        var hashedPassword = PasswordUtils.HashPassword(correctPassword);

        // Act
        var result = PasswordUtils.VerifyPassword(incorrectPassword, hashedPassword);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void VerifyPassword_WithEmptyPassword_ShouldReturnFalse()
    {
        // Arrange
        const string password = "TestPassword123!";
        var hashedPassword = PasswordUtils.HashPassword(password);

        // Act
        var result = PasswordUtils.VerifyPassword(string.Empty, hashedPassword);

        // Assert
        result.ShouldBeFalse();
    }

    [Theory]
    [InlineData("short")]
    [InlineData("verylongpasswordthatismorethan50characterslong12345")]
    [InlineData("P@ssw0rd!")]
    public void HashPassword_WithVariousPasswordLengths_ShouldSucceed(string password)
    {
        // Act
        var hashedPassword = PasswordUtils.HashPassword(password);

        // Assert
        hashedPassword.ShouldNotBeNullOrEmpty();
        hashedPassword.ShouldStartWith("$2");
    }

    [Fact]
    public void HashPassword_WithSpecialCharacters_ShouldHandleCorrectly()
    {
        // Arrange
        const string password = "P@$$w0rd!#%^&*()";

        // Act
        var hashedPassword = PasswordUtils.HashPassword(password);

        // Assert
        hashedPassword.ShouldNotBeNullOrEmpty();
        var isValid = PasswordUtils.VerifyPassword(password, hashedPassword);
        isValid.ShouldBeTrue();
    }

    [Fact]
    public void HashPassword_WithUnicodeCharacters_ShouldHandleCorrectly()
    {
        // Arrange
        const string password = "Пароль123!";

        // Act
        var hashedPassword = PasswordUtils.HashPassword(password);

        // Assert
        hashedPassword.ShouldNotBeNullOrEmpty();
        var isValid = PasswordUtils.VerifyPassword(password, hashedPassword);
        isValid.ShouldBeTrue();
    }
}