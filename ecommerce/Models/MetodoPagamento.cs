namespace ecommerce.Models
{
    public class MetodoPagamento
    {
        public int Id { get; set; }
        public string Descricao { get; set; } = string.Empty;

        // Relacionamentos
        public List<Cartao>? Cartoes { get; set; }
        public List<Pedido>? Pedidos { get; set; }
    }
}
