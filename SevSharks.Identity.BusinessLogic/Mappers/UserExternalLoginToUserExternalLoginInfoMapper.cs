using SevSharks.Identity.BusinessLogic.Models;
using SevSharks.Identity.DataAccess.Models;

namespace SevSharks.Identity.BusinessLogic.Mappers;

/// <summary>
/// Mapper from UserExternalLogin to UserExternalLoginInfo
/// </summary>
public static class UserExternalLoginToUserExternalLoginInfoMapper
{
    public static UserExternalLoginInfo Map(UserExternalLogin source)
    {
        if (source == null)
            return null;

        return new UserExternalLoginInfo
        {
            ExternalUserName = source.ExternalUserName,
            ExternalSystemName = source.ExternalSystemName
        };
    }

    public static void MapTo(UserExternalLogin source, UserExternalLoginInfo destination)
    {
        if (source == null || destination == null)
            return;

        destination.ExternalUserName = source.ExternalUserName;
        destination.ExternalSystemName = source.ExternalSystemName;
    }
}
