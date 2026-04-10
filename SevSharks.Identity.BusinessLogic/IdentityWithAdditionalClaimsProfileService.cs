using SevSharks.Identity.DataAccess.Models;
using Microsoft.AspNetCore.Identity;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Extensions;
using Duende.IdentityModel;

namespace SevSharks.Identity.BusinessLogic
{
    public class IdentityWithAdditionalClaimsProfileService : IProfileService
    {
        private readonly IUserClaimsPrincipalFactory<ApplicationUser> _claimsFactory;
        private readonly UserManager<ApplicationUser> _userManager;

        public IdentityWithAdditionalClaimsProfileService(UserManager<ApplicationUser> userManager,
            IUserClaimsPrincipalFactory<ApplicationUser> claimsFactory)
        {
            _userManager = userManager;
            _claimsFactory = claimsFactory;
        }

        public async Task GetProfileDataAsync(ProfileDataRequestContext context)
        {
            var subjectId = context.Subject.GetSubjectId();
            var user = await _userManager.Users
                .SingleOrDefaultAsync(x => x.Id == subjectId);

            var principal = await _claimsFactory.CreateAsync(user);

            var claims = principal.Claims.ToList();

            if (context.RequestedClaimTypes != null && context.RequestedClaimTypes.Any())
            {
                claims = claims.Where(x => context.RequestedClaimTypes.Where(r => r != CustomJwtClaimTypes.Permission && r != JwtClaimTypes.Role).Contains(x.Type)).ToList();

                claims.AddRange(new[]
                {
                    new Claim(JwtClaimTypes.Name, $"{user.UserName}")
                });

                if (_userManager.SupportsUserRole)
                {
                    var roleClaims =
                        from role in await _userManager.GetRolesAsync(user)
                        select new Claim(JwtClaimTypes.Role, role);
                    claims.AddRange(roleClaims);
                }

                if (_userManager.SupportsUserEmail)
                {
                    var email = await _userManager.GetEmailAsync(user);
                    if (!string.IsNullOrWhiteSpace(email))
                    {
                        claims.AddRange(new[]
                        {
                            new Claim(JwtClaimTypes.Email, email),
                            new Claim(JwtClaimTypes.EmailVerified,
                                await _userManager.IsEmailConfirmedAsync(user) ? "true" : "false",
                                ClaimValueTypes.Boolean)
                        });
                    }
                }

                if (_userManager.SupportsUserPhoneNumber)
                {
                    var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
                    if (!string.IsNullOrWhiteSpace(phoneNumber))
                    {
                        claims.AddRange(new[]
                        {
                            new Claim(JwtClaimTypes.PhoneNumber, phoneNumber),
                            new Claim(JwtClaimTypes.PhoneNumberVerified,
                                await _userManager.IsPhoneNumberConfirmedAsync(user) ? "true" : "false",
                                ClaimValueTypes.Boolean)
                        });
                    }
                }

                context.IssuedClaims = claims;
            }
        }

        public async Task IsActiveAsync(IsActiveContext context)
        {
            var sub = context.Subject.GetSubjectId();
            var user = await _userManager.FindByIdAsync(sub);
            context.IsActive = user != null;
        }
    }
}