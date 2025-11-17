namespace ecommerce.Models
{
    public class Cartao
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public int MetodoPagamentoId { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public string NomeTitular { get; set; } = string.Empty;
        public string Numero { get; set; } = string.Empty;
        public string Bandeira { get; set; } = string.Empty;
        public string Validade { get; set; } = string.Empty;
        public string CVV { get; set; } = string.Empty;

        // Navegação
        public Usuario? Usuario { get; set; }
    }
}
