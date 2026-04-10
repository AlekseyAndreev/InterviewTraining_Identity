using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace SevSharks.Identity.DataAccess.Models;

public class ApplicationUser : IdentityUser
{
    /// <summary>
    /// Navigation property for the claims this user possesses.
    /// </summary>
    public virtual ICollection<IdentityUserClaim<string>> Claims { get; set; }

    /// <summary>
    /// UserExternalLogin
    /// </summary>
    public virtual List<UserExternalLogin> ExternalLogins { get; set; }
}