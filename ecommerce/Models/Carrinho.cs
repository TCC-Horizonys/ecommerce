namespace ecommerce.Models
{
    public class Carrinho
    {
        public List<ItemPedido> Itens { get; set; } = new List<ItemPedido>();
        public List<Endereco> Enderecos { get; set; } = new List<Endereco>();
        public List<Cartao> Cartoes { get; set; } = new List<Cartao>();

        public Endereco NovoEndereco { get; set; } = new Endereco();
        public Cartao NovoCartao { get; set; } = new Cartao();
        public List<ProdutoImagem> Imagens { get; set; } = new List<ProdutoImagem>();

        // Id selecionados no form
        public int EnderecoSelecionadoId { get; set; }       // obrigatório
        public int? CartaoSelecionadoId { get; set; }       // opcional, se for Pix
        public string MetodoPagamento { get; set; } = string.Empty;

        public DateTime CriadoEm { get; set; }
        public DateTime Expiracao { get; set; }

        // Propriedades calculadas
        public int EnderecoId => EnderecoSelecionadoId;     // mapeia automaticamente
        public int? CartaoId => MetodoPagamento == "Cartão" ? CartaoSelecionadoId : null;
        public int MetodoPagamentoId => MetodoPagamento == "Pix" ? 2 : 1; // 1=Cartão, 2=Pix
        public decimal ValorTotal   { get; set; }

        public decimal Subtotal { get; set; }
        public decimal TaxaEntrega { get; set; } = 10m;
        public decimal Total => Subtotal + TaxaEntrega;
        public decimal Pix => (Subtotal + TaxaEntrega) * 0.95m;

        internal void Adicionar(Produto produto, int quantidade)
        {
            throw new NotImplementedException();
        }
    }

}
