using System.Collections.Generic;
using System.Linq;
using SevSharks.Identity.BusinessLogic.Models;
using SevSharks.Identity.DataAccess.Models;

namespace SevSharks.Identity.BusinessLogic.Mappers;

/// <summary>
/// Mapper from ApplicationUser to UserInfo
/// </summary>
public static class ApplicationUserToUserInfoMapper
{
    public static UserInfo Map(ApplicationUser source)
    {
        if (source == null)
            return null;

        return new UserInfo
        {
            Id = source.Id,
            Name = source.UserName,
            Email = source.Email,
            EmailConfirmed = source.EmailConfirmed,
            Phone = source.PhoneNumber,
            PhoneConfirmed = source.PhoneNumberConfirmed,
            UserExternalLogins = source.ExternalLogins?.Select(UserExternalLoginToUserExternalLoginInfoMapper.Map).ToList() ?? new List<UserExternalLoginInfo>()
        };
    }

    public static void MapTo(ApplicationUser source, UserInfo destination)
    {
        if (source == null || destination == null)
            return;

        destination.Id = source.Id;
        destination.Name = source.UserName;
        destination.Email = source.Email;
        destination.EmailConfirmed = source.EmailConfirmed;
        destination.Phone = source.PhoneNumber;
        destination.PhoneConfirmed = source.PhoneNumberConfirmed;
        destination.UserExternalLogins = source.ExternalLogins?.Select(UserExternalLoginToUserExternalLoginInfoMapper.Map).ToList() ?? new List<UserExternalLoginInfo>();
    }
}
