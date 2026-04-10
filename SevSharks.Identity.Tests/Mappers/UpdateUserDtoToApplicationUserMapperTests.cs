using SevSharks.Identity.BusinessLogic.Mappers;
using SevSharks.Identity.BusinessLogic.Models;
using Xunit;

namespace SevSharks.Identity.Tests.Mappers;

/// <summary>
/// Tests for UpdateUserDtoToApplicationUserMapper
/// </summary>
public class UpdateUserDtoToApplicationUserMapperTests
{
    [Fact]
    public void Map_WithNullSource_ReturnsNull()
    {
        // Act
        var result = UpdateUserDtoToApplicationUserMapper.Map(null);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Map_WithValidSource_ReturnsApplicationUserWithCorrectProperties()
    {
        // Arrange
        var source = new UpdateUserDto
        {
            UserName = "testuser",
            Email = "test@example.com",
            EmailConfirmed = true,
            PhoneNumber = "+1234567890",
            PhoneNumberConfirmed = true
        };

        // Act
        var result = UpdateUserDtoToApplicationUserMapper.Map(source);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(source.UserName, result.UserName);
        Assert.Equal(source.Email, result.Email);
        Assert.Equal(source.EmailConfirmed, result.EmailConfirmed);
        Assert.Equal(source.PhoneNumber, result.PhoneNumber);
        Assert.Equal(source.PhoneNumberConfirmed, result.PhoneNumberConfirmed);
    }

    [Fact]
    public void MapTo_WithNullSource_DoesNotThrow()
    {
        // Arrange
        var destination = new DataAccess.Models.ApplicationUser();

        // Act & Assert
        var exception = Record.Exception(() => UpdateUserDtoToApplicationUserMapper.MapTo(null, destination));
        Assert.Null(exception);
    }

    [Fact]
    public void MapTo_WithNullDestination_DoesNotThrow()
    {
        // Arrange
        var source = new UpdateUserDto
        {
            UserName = "testuser",
            Email = "test@example.com"
        };

        // Act & Assert
        var exception = Record.Exception(() => UpdateUserDtoToApplicationUserMapper.MapTo(source, null));
        Assert.Null(exception);
    }

    [Fact]
    public void MapTo_WithValidSourceAndDestination_MapsPropertiesCorrectly()
    {
        // Arrange
        var source = new UpdateUserDto
        {
            UserName = "newusername",
            Email = "newemail@example.com",
            EmailConfirmed = true,
            PhoneNumber = "+9999999999",
            PhoneNumberConfirmed = true
        };
        var destination = new global::SevSharks.Identity.DataAccess.Models.ApplicationUser
        {
            Id = "existing-id",
            UserName = "oldusername",
            Email = "old@example.com",
            EmailConfirmed = false,
            PhoneNumber = "+1111111111",
            PhoneNumberConfirmed = false
        };

        // Act
        UpdateUserDtoToApplicationUserMapper.MapTo(source, destination);

        // Assert
        Assert.Equal("existing-id", destination.Id); // Id should not change
        Assert.Equal(source.UserName, destination.UserName);
        Assert.Equal(source.Email, destination.Email);
        Assert.Equal(source.EmailConfirmed, destination.EmailConfirmed);
        Assert.Equal(source.PhoneNumber, destination.PhoneNumber);
        Assert.Equal(source.PhoneNumberConfirmed, destination.PhoneNumberConfirmed);
    }
}
