using System.ComponentModel.DataAnnotations;

namespace WarehouseReserveSystem.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Будь ласка, вкажіть ваш логін або email.")]
    [Display(Name = "Логін або Email")]
    public string Login { get; set; } = string.Empty;

    [Required(ErrorMessage = "Будь ласка, введіть пароль.")]
    [DataType(DataType.Password)]
    [Display(Name = "Пароль")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Запам'ятати мене")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}
