using SevSharks.Identity.DataAccess.Models;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using SevSharks.Identity.DataAccess;
using SevSharks.Identity.BusinessLogic.Models;
using SevSharks.Identity.BusinessLogic.Mappers;
using Duende.IdentityModel;

namespace SevSharks.Identity.BusinessLogic;

public class UserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly Context _context;

    public UserService(UserManager<ApplicationUser> userManager, Context context)
    {
        _userManager = userManager;
        _context = context;
    }

    public async Task<(ApplicationUser user, List<string> roles)> GetUserInfoById(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            throw new Exception("Не найден пользователь");
        }

        var isCandidate = await _userManager.IsInRoleAsync(user, RolesConstants.Candidate);
        var isExpert = await _userManager.IsInRoleAsync(user, RolesConstants.Candidate);

        List<string> rolesResult = new List<string>();

        if (isCandidate)
        {
            rolesResult.Add(RolesConstants.Candidate);
        }

        if (isExpert)
        {
            rolesResult.Add(RolesConstants.Expert);
        }

        return (user, rolesResult);
    }

    public async Task<ApplicationUser> CreateUser(CreateUserDto userDto)
    {
        var user = await _userManager.FindByNameAsync(userDto.UserName);
        if (user != null)
        {
            var existingUser = _context.Users
                .FirstOrDefault(u => u.Id == user.Id);

            return await UpdateUser(existingUser, CreateUserDtoToUpdateUserDtoMapper.Map(userDto));
        }

        user = CreateUserDtoToApplicationUserMapper.Map(userDto);
        if (userDto.ExternalSystemIdentifier != null && userDto.ExternalSystemName != null)
        {
            user.ExternalLogins = new List<UserExternalLogin>
            {
                new UserExternalLogin
                {
                    UserId = user.Id,
                    ExternalSystemName = userDto.ExternalSystemName,
                    ExternalUserName = userDto.ExternalSystemIdentifier
                }
            };
        }

        IdentityResult result;
        if (string.IsNullOrEmpty(userDto.Password))
        {
            result = await _userManager.CreateAsync(user);
        }
        else
        {
            result = await _userManager.CreateAsync(user, userDto.Password);
        }
        if (!result.Succeeded)
        {
            throw new Exception(result.Errors.First().Description);
        }

        var claims = new List<Claim>
        {
            new Claim(JwtClaimTypes.Name, userDto.UserName),
            new Claim(JwtClaimTypes.GivenName, userDto.UserName),
            new Claim(JwtClaimTypes.FamilyName, userDto.UserName)
        };

        result = await _userManager.AddClaimsAsync(user, claims);
        if (!result.Succeeded)
        {
            throw new Exception(result.Errors.First().Description);
        }

        if (userDto.Roles != null && userDto.Roles.Any())
        {
            foreach (var role in userDto.Roles)
            {
                if (string.IsNullOrEmpty(role))
                {
                    continue;
                }
                result = await _userManager.AddToRoleAsync(user, role);
                if (!result.Succeeded)
                {
                    throw new Exception(result.Errors.First().Description);
                }
            }
        }

        return user;
    }

    public async Task<ApplicationUser> UpdateUser(ApplicationUser user, UpdateUserDto userDto)
    {
        UpdateUserDtoToApplicationUserMapper.MapTo(userDto, user);
        if (!string.IsNullOrEmpty(userDto.Password))
        {
            user.PasswordHash = _userManager.PasswordHasher.HashPassword(user, userDto.Password);
        }

        var result = await _userManager.UpdateAsync(user);
        if (result.Succeeded)
        {
            return user;
        }

        throw new Exception(result.Errors.First().Description);
    }

    public async Task<ApplicationUser> UpdateUserRoles(string userName, string[] roles)
    {
        var user = await _userManager.FindByNameAsync(userName);
        if (user == null)
        {
            throw new Exception("Не найден пользователь");
        }

        // Получаем текущие роли пользователя
        var currentRoles = await _userManager.GetRolesAsync(user);

        // Удаляем все текущие роли
        if (currentRoles.Any())
        {
            var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!removeResult.Succeeded)
            {
                throw new Exception(removeResult.Errors.First().Description);
            }
        }

        // Добавляем новые роли
        if (roles != null && roles.Any())
        {
            foreach (var role in roles)
            {
                if (string.IsNullOrEmpty(role))
                {
                    continue;
                }
                var addResult = await _userManager.AddToRoleAsync(user, role);
                if (!addResult.Succeeded)
                {
                    throw new Exception(addResult.Errors.First().Description);
                }
            }
        }

        return user;
    }
}
