using System.ComponentModel.DataAnnotations;

namespace ecommerce.Models
{
    public class Usuario
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "O nome é obrigatório."), MaxLength(100)]
        public string Nome { get; set; } = "";


        [Required(ErrorMessage = "O email é obrigatório."), EmailAddress, MaxLength(100)]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "A senha é obrigatória."), MaxLength(100)]
        public string Senha { get; set; } = "";

        [Required(ErrorMessage = "Confirmar a senha é obrigatório.")]
        [Compare("Senha", ErrorMessage = "As senhas não coincidem")]
        public string ConfirmarSenha { get; set; } = "";

        [Required(ErrorMessage = "Informe a data de nascimento.")]
        public string DataNasc { get; set; }

        // Somente dígitos (11 = DDD + número celular)
        [Required(ErrorMessage = "Telefone deve ter 10 ou 11 dígitos.")]
        public string Telefone { get; set; }

        // Somente dígitos do CPF (11)
        [Required(ErrorMessage = "CPF deve ter 11 dígitos.")]
        public string CPF { get; set; }

        public string Tipo { get; set; } = "cliente";

        public List<Endereco>? Enderecos { get; set; }
        public List<Cartao>? Cartoes { get; set; }
        public List<Pedido>? Pedidos { get; set; }
    }
}
