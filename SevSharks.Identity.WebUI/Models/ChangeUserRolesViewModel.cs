using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SevSharks.Identity.WebUI.Models
{
    /// <summary>
    /// Модель регистрации пользователя
    /// </summary>
    public class ChangeUserRolesViewModel
    {
        /// <summary>
        /// Email пользователя.
        /// </summary>
        [DataType(DataType.EmailAddress)]
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Login { get; set; }

        /// <summary>
        /// Роли пользователя
        /// </summary>
        [Display(Name = "Роли пользователя")]
        public List<string> Roles { get; set; }

        /// <summary>
        /// ReturnUrl
        /// </summary>
        [Required]
        public string ReturnUrl { get; set; }

        /// <summary>
        /// Флаг, показывающий успешность авторизации
        /// </summary>
        public bool IsSucceed { get; set; }

        /// <summary>
        /// Сообщение об ошибках
        /// </summary>
        public List<string> ErrorMessages { get; set; }
    }
}
