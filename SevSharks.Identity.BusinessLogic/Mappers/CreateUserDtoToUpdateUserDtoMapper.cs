using SevSharks.Identity.BusinessLogic.Models;

namespace SevSharks.Identity.BusinessLogic.Mappers;

/// <summary>
/// Mapper from CreateUserDto to UpdateUserDto
/// </summary>
public static class CreateUserDtoToUpdateUserDtoMapper
{
    public static UpdateUserDto Map(CreateUserDto source)
    {
        if (source == null)
            return null;

        return new UpdateUserDto
        {
            UserName = source.UserName,
            Email = source.Email,
            EmailConfirmed = source.EmailConfirmed,
            Password = source.Password,
            PhoneNumber = source.PhoneNumber,
            PhoneNumberConfirmed = source.PhoneNumberConfirmed,
            ExternalSystemIdentifier = source.ExternalSystemIdentifier,
            ExternalSystemName = source.ExternalSystemName
        };
    }

    public static void MapTo(CreateUserDto source, UpdateUserDto destination)
    {
        if (source == null || destination == null)
            return;

        destination.UserName = source.UserName;
        destination.Email = source.Email;
        destination.EmailConfirmed = source.EmailConfirmed;
        destination.Password = source.Password;
        destination.PhoneNumber = source.PhoneNumber;
        destination.PhoneNumberConfirmed = source.PhoneNumberConfirmed;
        destination.ExternalSystemIdentifier = source.ExternalSystemIdentifier;
        destination.ExternalSystemName = source.ExternalSystemName;
    }
}
