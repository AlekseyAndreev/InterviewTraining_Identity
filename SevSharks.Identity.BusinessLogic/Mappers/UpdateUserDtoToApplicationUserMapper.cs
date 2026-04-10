using SevSharks.Identity.BusinessLogic.Models;
using SevSharks.Identity.DataAccess.Models;

namespace SevSharks.Identity.BusinessLogic.Mappers;

/// <summary>
/// Mapper from UpdateUserDto to ApplicationUser
/// </summary>
public static class UpdateUserDtoToApplicationUserMapper
{
    public static ApplicationUser Map(UpdateUserDto source)
    {
        if (source == null)
            return null;

        return new ApplicationUser
        {
            UserName = source.UserName,
            Email = source.Email,
            EmailConfirmed = source.EmailConfirmed,
            PhoneNumber = source.PhoneNumber,
            PhoneNumberConfirmed = source.PhoneNumberConfirmed
        };
    }

    public static void MapTo(UpdateUserDto source, ApplicationUser destination)
    {
        if (source == null || destination == null)
            return;

        destination.UserName = source.UserName;
        destination.Email = source.Email;
        destination.EmailConfirmed = source.EmailConfirmed;
        destination.PhoneNumber = source.PhoneNumber;
        destination.PhoneNumberConfirmed = source.PhoneNumberConfirmed;
    }
}
