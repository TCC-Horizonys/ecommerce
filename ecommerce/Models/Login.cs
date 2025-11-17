using System.ComponentModel.DataAnnotations;

namespace ecommerce.Models
{
    public class Login
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "O email é obrigatório."), EmailAddress, MaxLength(100)]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "A senha é obrigatória."), MaxLength(100)]
        public string Senha { get; set; } = "";

    }
}
