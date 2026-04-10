using SevSharks.Identity.BusinessLogic.Mappers;
using SevSharks.Identity.BusinessLogic.Models;
using Xunit;

namespace SevSharks.Identity.Tests.Mappers;

/// <summary>
/// Tests for CreateUserDtoToUpdateUserDtoMapper
/// </summary>
public class CreateUserDtoToUpdateUserDtoMapperTests
{
    [Fact]
    public void Map_WithNullSource_ReturnsNull()
    {
        // Act
        var result = CreateUserDtoToUpdateUserDtoMapper.Map(null);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Map_WithValidSource_ReturnsUpdateUserDtoWithAllProperties()
    {
        // Arrange
        var source = new CreateUserDto
        {
            UserName = "testuser",
            Email = "test@example.com",
            EmailConfirmed = true,
            Password = "SecurePassword123!",
            PhoneNumber = "+1234567890",
            PhoneNumberConfirmed = true,
            ExternalSystemIdentifier = "external-id-123",
            ExternalSystemName = "ExternalSystem"
        };

        // Act
        var result = CreateUserDtoToUpdateUserDtoMapper.Map(source);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(source.UserName, result.UserName);
        Assert.Equal(source.Email, result.Email);
        Assert.Equal(source.EmailConfirmed, result.EmailConfirmed);
        Assert.Equal(source.Password, result.Password);
        Assert.Equal(source.PhoneNumber, result.PhoneNumber);
        Assert.Equal(source.PhoneNumberConfirmed, result.PhoneNumberConfirmed);
        Assert.Equal(source.ExternalSystemIdentifier, result.ExternalSystemIdentifier);
        Assert.Equal(source.ExternalSystemName, result.ExternalSystemName);
    }

    [Fact]
    public void MapTo_WithNullSource_DoesNotThrow()
    {
        // Arrange
        var destination = new UpdateUserDto();

        // Act & Assert
        var exception = Record.Exception(() => CreateUserDtoToUpdateUserDtoMapper.MapTo(null, destination));
        Assert.Null(exception);
    }

    [Fact]
    public void MapTo_WithNullDestination_DoesNotThrow()
    {
        // Arrange
        var source = new CreateUserDto
        {
            UserName = "testuser",
            Email = "test@example.com"
        };

        // Act & Assert
        var exception = Record.Exception(() => CreateUserDtoToUpdateUserDtoMapper.MapTo(source, null));
        Assert.Null(exception);
    }

    [Fact]
    public void MapTo_WithValidSourceAndDestination_MapsAllProperties()
    {
        // Arrange
        var source = new CreateUserDto
        {
            UserName = "newusername",
            Email = "newemail@example.com",
            EmailConfirmed = true,
            Password = "NewPassword123!",
            PhoneNumber = "+9999999999",
            PhoneNumberConfirmed = true,
            ExternalSystemIdentifier = "new-external-id",
            ExternalSystemName = "NewExternalSystem"
        };
        var destination = new UpdateUserDto
        {
            UserName = "oldusername",
            Email = "old@example.com",
            EmailConfirmed = false,
            Password = "OldPassword!",
            PhoneNumber = "+1111111111",
            PhoneNumberConfirmed = false,
            ExternalSystemIdentifier = "old-external-id",
            ExternalSystemName = "OldExternalSystem"
        };

        // Act
        CreateUserDtoToUpdateUserDtoMapper.MapTo(source, destination);

        // Assert
        Assert.Equal(source.UserName, destination.UserName);
        Assert.Equal(source.Email, destination.Email);
        Assert.Equal(source.EmailConfirmed, destination.EmailConfirmed);
        Assert.Equal(source.Password, destination.Password);
        Assert.Equal(source.PhoneNumber, destination.PhoneNumber);
        Assert.Equal(source.PhoneNumberConfirmed, destination.PhoneNumberConfirmed);
        Assert.Equal(source.ExternalSystemIdentifier, destination.ExternalSystemIdentifier);
        Assert.Equal(source.ExternalSystemName, destination.ExternalSystemName);
    }
}
