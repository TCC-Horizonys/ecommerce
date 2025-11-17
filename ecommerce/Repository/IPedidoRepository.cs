using ecommerce.Models;

namespace ecommerce.Repository
{
    public interface IPedidoRepository
    {
        Task<int> AdicionarPedido(Pedido pedido);
        Task<Pedido?> ObterPedidoPorId(int pedidoId);
        Task<List<Pedido>> ObterPedidosPorUsuario(int usuarioId);
        Task<int> AtualizarStatusPagamento(Pedido pedido);
        Task<List<Pedido>> ObterTodosPedidos();
        Task<Pedido?> ObterPedidoPorIdAdm(int pedidoId);
        Task AtualizarStatus(int pedidoId, string statusPedido);
        Task<List<Pedido>> ObterPedidosDoUsuario(int usuarioId);
    }

}
