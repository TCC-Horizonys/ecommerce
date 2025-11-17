using System.ComponentModel.DataAnnotations;

namespace ecommerce.Models
{
    public class SenhaNova
    {
        [Required(ErrorMessage = "A senha atual é obrigatória."), MaxLength(100)]
        public string SenhaAtual { get; set; } = "";

        [Required(ErrorMessage = "A nova senha é obrigatória."), MaxLength(100)]
        public string NovaSenha { get; set; } = "";

        [Required(ErrorMessage = "A confirmação da senha é obrigatória.")]
        [Compare("NovaSenha", ErrorMessage = "As senhas não coincidem.")]
        public string ConfirmarSenha { get; set; } = "";
    }
}
