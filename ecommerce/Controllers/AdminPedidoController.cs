using Microsoft.AspNetCore.Mvc;
using ecommerce.Models;
using ecommerce.Repository;

namespace ecommerce.Controllers
{
    [Route("/Admin/[controller]/{action=Index}/{id?}")]
    public class AdminPedidoController : Controller
    {
        private readonly IPedidoRepository _pedidoRepository;
        private readonly int pagTam = 8;

        public AdminPedidoController(IPedidoRepository pedidoRepository)
        {
            _pedidoRepository = pedidoRepository;
        }

        public async Task<IActionResult> Index(int pagIndex)
        {
            var pedidos = await _pedidoRepository.ObterTodosPedidos();

            if (pagIndex < 1) pagIndex = 1;

            // Total de itens e páginas
            int count = pedidos.Count();
            int totalPag = (int)Math.Ceiling((decimal)count / pagTam);

            if (totalPag == 0) totalPag = 1;
            if (pagIndex > totalPag) pagIndex = totalPag;

            // Pegar produtos da página atual
            var pedidosPaginados = pedidos
                .Skip((pagIndex - 1) * pagTam)
                .Take(pagTam)
                .ToList();


            // Dados para a view
            ViewData["PagIndex"] = pagIndex;
            ViewData["TotalPag"] = totalPag;
            return View(pedidosPaginados);
        }
        public async Task<IActionResult> Detalhes(int id)
        {
            // Obter o pedido pelo Id com itens e produtos
            var pedido = await _pedidoRepository.ObterPedidoPorIdAdm(id);

            if (pedido == null)
            {
                return RedirectToAction("Index");
            }

            // Contar todos os pedidos do mesmo usuário
            int numPedidos = 0;
            if (pedido.UsuarioId > 0)
            {
                var pedidosDoUsuario = await _pedidoRepository.ObterPedidosPorUsuario(pedido.UsuarioId);
                numPedidos = pedidosDoUsuario.Count;
            }

            ViewBag.NumPedidos = numPedidos;

            return View(pedido);
        }

        // salvar as alterações
        [HttpPost]
        public async Task<IActionResult> Editar(int id, string? status_Pedido)
        {
            var pedido = await _pedidoRepository.ObterPedidoPorIdAdm(id);

            if (pedido == null)
            {
                return RedirectToAction("Index");
            }

            // Atualiza somente os campos enviados
            if (!string.IsNullOrEmpty(status_Pedido))
            {
                await _pedidoRepository.AtualizarStatus(id, status_Pedido);
            }

            // Redireciona para detalhes
            return RedirectToAction("Detalhes", new { id });
        }
    }


}