namespace ecommerce.Models
{
    public class ProdutoImagem
    {
        public int ImagemId { get; set; }
        public int ProdutoId { get; set; }
        public string Url { get; set; } = string.Empty;
        public int OrdemImagem { get; set; }
    }
}
