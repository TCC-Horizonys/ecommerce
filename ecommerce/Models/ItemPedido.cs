namespace ecommerce.Models
{
    public class ItemPedido
    {
        public int Id { get; set; }
        public int PedidoId { get; set; }
        public int ProdutoId { get; set; }
        public int Quantidade { get; set; }
        public decimal PrecoUnitario { get; set; }

        // Navegação
        public Pedido? Pedido { get; set; }
        public Produto? Produto { get; set; }
        public ProdutoImagem? Imagem { get; set; }
    }
}
