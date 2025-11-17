using System.ComponentModel.DataAnnotations;

namespace ecommerce.Models
{
    public class Perfil
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "O nome é obrigatório."), MaxLength(100)]
        public string Nome { get; set; } = "";

        [Required(ErrorMessage = "A Data de Nascimento é obrigatório."), MaxLength(100)]
        public string DataNasc { get; set; } = "";

        [Required(ErrorMessage = "O email é obrigatório."), EmailAddress, MaxLength(100)]
        public string Email { get; set; } = "";


        [RegularExpression(@"^\d{11}$", ErrorMessage = "O CPF deve conter 11 números.")]
        public string CPF { get; set; }


        [RegularExpression(@"^\d{11}$", ErrorMessage = "O telefone deve conter 11 números (DDD + número).")]
        public string Telefone { get; set; }

        public string Tipo { get; set; } = "cliente";


    }
}
