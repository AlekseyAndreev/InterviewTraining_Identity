using System;
using SevSharks.Identity.BusinessLogic.Models;
using SevSharks.Identity.DataAccess.Models;

namespace SevSharks.Identity.BusinessLogic.Mappers;

/// <summary>
/// Mapper from CreateUserDto to ApplicationUser
/// </summary>
public static class CreateUserDtoToApplicationUserMapper
{
    public static ApplicationUser Map(CreateUserDto source)
    {
        if (source == null)
            return null;

        return new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = source.UserName,
            Email = source.Email,
            EmailConfirmed = source.EmailConfirmed,
            PhoneNumber = source.PhoneNumber,
            PhoneNumberConfirmed = source.PhoneNumberConfirmed
        };
    }

    public static void MapTo(CreateUserDto source, ApplicationUser destination)
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
