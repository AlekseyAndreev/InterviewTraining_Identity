using System.ComponentModel.DataAnnotations;

namespace SevSharks.Identity.WebUI.Models
{
    public class EmailConfirmationRequiredViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        public string ReturnUrl { get; set; }
    }
}