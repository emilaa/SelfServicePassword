using System.ComponentModel.DataAnnotations;

namespace SelfServicePassword.ViewModels
{
    public class UserVM
    {
        [Required(ErrorMessage = "İstifadəçi adınızı qeyd edin.")]
        public string? Username { get; set; }

        [Required(ErrorMessage = "Mövcud parolunuzu qeyd edin.")]
        [DataType(DataType.Password)]
        public string? CurrentPassword { get; set; }

        [Required(ErrorMessage = "Yeni parolunuzu qeyd edin.")]
        [DataType(DataType.Password)]
        public string? NewPassword { get; set; }

        [Required(ErrorMessage = "Parolunuzu təsdiq edin.")]
        [DataType(DataType.Password)]
        public string? ConfirmPassword { get; set; }
    }
}
