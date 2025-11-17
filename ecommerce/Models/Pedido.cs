namespace ecommerce.Models
{
    public class Pedido
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public int EnderecoId { get; set; }
        public int? MetodoPagamentoId { get; set; }
        public int? CartaoId { get; set; }
        public decimal TaxaEntrega { get; set; }
        public string StatusPagamento { get; set; } = "Aguardando Pagamento";
        public string StatusPedido { get; set; } = "Criado";
        public decimal ValorTotal { get; set; }
        public DateTime DataPedido { get; set; }
        public DateTime DataPagamento { get; set; } 
        public DateTime Expiracao { get; set; }

        // Navegação
        public Usuario? Usuario { get; set; }
        public Endereco? Endereco { get; set; }
        public MetodoPagamento? MetodoPagamento { get; set; }
        public Cartao? Cartao { get; set; }
        public List<ItemPedido>? Itens { get; set; }
        public List<ProdutoImagem> Imagens { get; set; } = new List<ProdutoImagem>();
    }
}
