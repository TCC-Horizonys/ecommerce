using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecommerce.Models
{
    public class Produto
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "O nome do produto é obrigatório.")]
        [StringLength(100)]
        public string Nome { get; set; }

        [Required(ErrorMessage = "A categoria do produto é obrigatória.")]
        [StringLength(70)]
        public string Categoria { get; set; }

        public string Descricao { get; set; }

        [Required(ErrorMessage = "O Estado de Conservação do produto é obrigatória.")]
        public string EstadoConservacao { get; set; }

        [Required(ErrorMessage = "O preço é obrigatório.")]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, double.MaxValue, ErrorMessage = "O preço deve ser maior que zero.")]
        public decimal Preco { get; set; }


        [Display(Name = "Data de Criação")]
        [DataType(DataType.Date)]
        public DateTime DataCriada { get; set; } = DateTime.Now;


        public List<ProdutoImagem> Imagens { get; set; } = new List<ProdutoImagem>();
    }
}
