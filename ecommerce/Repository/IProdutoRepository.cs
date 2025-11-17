using System.Threading.Tasks;
using ecommerce.Models;

namespace ecommerce.Repository
{
    public interface IProdutoRepository
    {
        Task<List<Produto>> TodosProdutos();
        Task<List<Produto>> ProdutosOrdenados();
        Task<Produto?> ProdutosPorId(int id);
        Task<int> AdicionarProduto(Produto produto);
        Task AtualizarProduto(Produto produto);
        Task DeletarProduto(int id);
        Task DeletarImagem(int imagemId);
        Task<ProdutoImagem?> ImagemPorId(int id);
        Task<Dictionary<string, List<string>>> ObterCategoriasTipos();

    }
}
