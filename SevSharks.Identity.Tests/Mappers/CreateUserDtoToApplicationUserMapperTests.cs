using SevSharks.Identity.BusinessLogic.Mappers;
using SevSharks.Identity.BusinessLogic.Models;
using Xunit;

namespace SevSharks.Identity.Tests.Mappers
{
    /// <summary>
    /// Tests for CreateUserDtoToApplicationUserMapper
    /// </summary>
    public class CreateUserDtoToApplicationUserMapperTests
    {
        [Fact]
        public void Map_WithNullSource_ReturnsNull()
        {
            // Act
            var result = CreateUserDtoToApplicationUserMapper.Map(null);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Map_WithValidSource_ReturnsApplicationUserWithCorrectProperties()
        {
            // Arrange
            var source = new CreateUserDto
            {
                UserName = "testuser",
                Email = "test@example.com",
                EmailConfirmed = true,
                PhoneNumber = "+1234567890",
                PhoneNumberConfirmed = true
            };

            // Act
            var result = CreateUserDtoToApplicationUserMapper.Map(source);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result.Id);
            Assert.Equal(source.UserName, result.UserName);
            Assert.Equal(source.Email, result.Email);
            Assert.Equal(source.EmailConfirmed, result.EmailConfirmed);
            Assert.Equal(source.PhoneNumber, result.PhoneNumber);
            Assert.Equal(source.PhoneNumberConfirmed, result.PhoneNumberConfirmed);
        }

        [Fact]
        public void Map_GeneratesNewIdForEachCall()
        {
            // Arrange
            var source = new CreateUserDto
            {
                UserName = "testuser",
                Email = "test@example.com"
            };

            // Act
            var result1 = CreateUserDtoToApplicationUserMapper.Map(source);
            var result2 = CreateUserDtoToApplicationUserMapper.Map(source);

            // Assert
            Assert.NotEqual(result1.Id, result2.Id);
        }

        [Fact]
        public void MapTo_WithNullSource_DoesNotThrow()
        {
            // Arrange
            var destination = new DataAccess.Models.ApplicationUser();

            // Act & Assert
            var exception = Record.Exception(() => CreateUserDtoToApplicationUserMapper.MapTo(null, destination));
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
            var exception = Record.Exception(() => CreateUserDtoToApplicationUserMapper.MapTo(source, null));
            Assert.Null(exception);
        }

        [Fact]
        public void MapTo_WithValidSourceAndDestination_MapsPropertiesCorrectly()
        {
            // Arrange
            var source = new CreateUserDto
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
            CreateUserDtoToApplicationUserMapper.MapTo(source, destination);

            // Assert
            Assert.Equal("existing-id", destination.Id); // Id should not change
            Assert.Equal(source.UserName, destination.UserName);
            Assert.Equal(source.Email, destination.Email);
            Assert.Equal(source.EmailConfirmed, destination.EmailConfirmed);
            Assert.Equal(source.PhoneNumber, destination.PhoneNumber);
            Assert.Equal(source.PhoneNumberConfirmed, destination.PhoneNumberConfirmed);
        }
    }
}
