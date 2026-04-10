using System.Collections.Generic;
using SevSharks.Identity.BusinessLogic.Mappers;
using SevSharks.Identity.DataAccess.Models;
using Xunit;

namespace SevSharks.Identity.Tests.Mappers
{
    /// <summary>
    /// Tests for ApplicationUserToUserInfoMapper
    /// </summary>
    public class ApplicationUserToUserInfoMapperTests
    {
        [Fact]
        public void Map_WithNullSource_ReturnsNull()
        {
            // Act
            var result = ApplicationUserToUserInfoMapper.Map(null);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Map_WithValidSource_ReturnsUserInfoWithCorrectProperties()
        {
            // Arrange
            var source = new ApplicationUser
            {
                Id = "user-id-123",
                UserName = "testuser",
                Email = "test@example.com",
                EmailConfirmed = true,
                PhoneNumber = "+1234567890",
                PhoneNumberConfirmed = true
            };

            // Act
            var result = ApplicationUserToUserInfoMapper.Map(source);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(source.Id, result.Id);
            Assert.Equal(source.UserName, result.Name);
            Assert.Equal(source.Email, result.Email);
            Assert.Equal(source.EmailConfirmed, result.EmailConfirmed);
            Assert.Equal(source.PhoneNumber, result.Phone);
            Assert.Equal(source.PhoneNumberConfirmed, result.PhoneConfirmed);
            Assert.NotNull(result.UserExternalLogins);
            Assert.Empty(result.UserExternalLogins);
        }

        [Fact]
        public void Map_WithExternalLogins_MapsExternalLoginsCorrectly()
        {
            // Arrange
            var source = new ApplicationUser
            {
                Id = "user-id-123",
                UserName = "testuser",
                Email = "test@example.com",
                EmailConfirmed = true,
                PhoneNumber = "+1234567890",
                PhoneNumberConfirmed = true,
                ExternalLogins = new List<UserExternalLogin>
                {
                    new UserExternalLogin
                    {
                        ExternalUserName = "external-user-1",
                        ExternalSystemName = "System1"
                    },
                    new UserExternalLogin
                    {
                        ExternalUserName = "external-user-2",
                        ExternalSystemName = "System2"
                    }
                }
            };

            // Act
            var result = ApplicationUserToUserInfoMapper.Map(source);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.UserExternalLogins);
            Assert.Equal(2, result.UserExternalLogins.Count);
            Assert.Equal("external-user-1", result.UserExternalLogins[0].ExternalUserName);
            Assert.Equal("System1", result.UserExternalLogins[0].ExternalSystemName);
            
            Assert.Equal("external-user-2", result.UserExternalLogins[1].ExternalUserName);
            Assert.Equal("System2", result.UserExternalLogins[1].ExternalSystemName);
        }

        [Fact]
        public void Map_WithNullExternalLogins_ReturnsEmptyList()
        {
            // Arrange
            var source = new ApplicationUser
            {
                Id = "user-id-123",
                UserName = "testuser",
                Email = "test@example.com",
                ExternalLogins = null
            };

            // Act
            var result = ApplicationUserToUserInfoMapper.Map(source);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.UserExternalLogins);
            Assert.Empty(result.UserExternalLogins);
        }

        [Fact]
        public void MapTo_WithNullSource_DoesNotThrow()
        {
            // Arrange
            var destination = new BusinessLogic.Models.UserInfo();

            // Act & Assert
            var exception = Record.Exception(() => ApplicationUserToUserInfoMapper.MapTo(null, destination));
            Assert.Null(exception);
        }

        [Fact]
        public void MapTo_WithNullDestination_DoesNotThrow()
        {
            // Arrange
            var source = new ApplicationUser
            {
                Id = "user-id-123",
                UserName = "testuser"
            };

            // Act & Assert
            var exception = Record.Exception(() => ApplicationUserToUserInfoMapper.MapTo(source, null));
            Assert.Null(exception);
        }

        [Fact]
        public void MapTo_WithValidSourceAndDestination_MapsPropertiesCorrectly()
        {
            // Arrange
            var source = new ApplicationUser
            {
                Id = "new-id",
                UserName = "newusername",
                Email = "newemail@example.com",
                EmailConfirmed = true,
                PhoneNumber = "+9999999999",
                PhoneNumberConfirmed = true
            };
            var destination = new BusinessLogic.Models.UserInfo
            {
                Id = "old-id",
                Name = "oldusername",
                Email = "old@example.com",
                EmailConfirmed = false,
                Phone = "+1111111111",
                PhoneConfirmed = false
            };

            // Act
            ApplicationUserToUserInfoMapper.MapTo(source, destination);

            // Assert
            Assert.Equal(source.Id, destination.Id);
            Assert.Equal(source.UserName, destination.Name);
            Assert.Equal(source.Email, destination.Email);
            Assert.Equal(source.EmailConfirmed, destination.EmailConfirmed);
            Assert.Equal(source.PhoneNumber, destination.Phone);
            Assert.Equal(source.PhoneNumberConfirmed, destination.PhoneConfirmed);
        }
    }
}
