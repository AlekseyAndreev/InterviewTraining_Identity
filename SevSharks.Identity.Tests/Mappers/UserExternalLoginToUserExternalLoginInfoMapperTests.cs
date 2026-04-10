using SevSharks.Identity.BusinessLogic.Mappers;
using SevSharks.Identity.DataAccess.Models;
using Xunit;

namespace SevSharks.Identity.Tests.Mappers
{
    /// <summary>
    /// Tests for UserExternalLoginToUserExternalLoginInfoMapper
    /// </summary>
    public class UserExternalLoginToUserExternalLoginInfoMapperTests
    {
        [Fact]
        public void Map_WithNullSource_ReturnsNull()
        {
            // Act
            var result = UserExternalLoginToUserExternalLoginInfoMapper.Map(null);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Map_WithValidSource_ReturnsUserExternalLoginInfoWithCorrectProperties()
        {
            // Arrange
            var source = new UserExternalLogin
            {
                ExternalUserName = "external-user-123",
                ExternalSystemName = "ExternalSystem"
            };

            // Act
            var result = UserExternalLoginToUserExternalLoginInfoMapper.Map(source);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(source.ExternalUserName, result.ExternalUserName);
            Assert.Equal(source.ExternalSystemName, result.ExternalSystemName);
        }

        [Fact]
        public void MapTo_WithNullSource_DoesNotThrow()
        {
            // Arrange
            var destination = new BusinessLogic.Models.UserExternalLoginInfo();

            // Act & Assert
            var exception = Record.Exception(() => UserExternalLoginToUserExternalLoginInfoMapper.MapTo(null, destination));
            Assert.Null(exception);
        }

        [Fact]
        public void MapTo_WithNullDestination_DoesNotThrow()
        {
            // Arrange
            var source = new UserExternalLogin
            {
                ExternalUserName = "external-user",
                ExternalSystemName = "ExternalSystem"
            };

            // Act & Assert
            var exception = Record.Exception(() => UserExternalLoginToUserExternalLoginInfoMapper.MapTo(source, null));
            Assert.Null(exception);
        }

        [Fact]
        public void MapTo_WithValidSourceAndDestination_MapsPropertiesCorrectly()
        {
            // Arrange
            var source = new UserExternalLogin
            {
                ExternalUserName = "new-external-user",
                ExternalSystemName = "NewExternalSystem"
            };
            var destination = new BusinessLogic.Models.UserExternalLoginInfo
            {
                ExternalUserName = "old-external-user",
                ExternalSystemName = "OldExternalSystem"
            };

            // Act
            UserExternalLoginToUserExternalLoginInfoMapper.MapTo(source, destination);

            // Assert
            Assert.Equal(source.ExternalUserName, destination.ExternalUserName);
            Assert.Equal(source.ExternalSystemName, destination.ExternalSystemName);
        }
    }
}
