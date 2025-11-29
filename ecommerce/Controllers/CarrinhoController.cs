using ecommerce.Models;
using ecommerce.Repository;
using ecommerce.Services;
using Microsoft.AspNetCore.Mvc;
using System.Linq; 

namespace ecommerce.Controllers
{
    public class CarrinhoController : Controller
    {
        private readonly IProdutoRepository _produtoRepository;
        private readonly IPedidoRepository _pedidoRepository;
        private readonly CarrinhoRepository _carrinhoRepository;

        public CarrinhoController(
            IProdutoRepository produtoRepository,
            CarrinhoRepository carrinhoRepository,
            IPedidoRepository pedidoRepository)
        {
            _produtoRepository = produtoRepository;
            _carrinhoRepository = carrinhoRepository;
            _pedidoRepository = pedidoRepository;
        }

        public async Task<IActionResult> Index()
        {
            var itensCarrinho = await AuxiliarCarrinho
                .ObterItensCarrinho(Request, Response, _produtoRepository)
                ?? new List<ItemPedido>();

            foreach (var it in itensCarrinho)
                it.Imagem ??= it.Produto?.Imagens?.FirstOrDefault();

            decimal subtotal = AuxiliarCarrinho.ObterSubtotal(itensCarrinho);
            decimal taxaEntrega = 10m;

            var carrinho = new Carrinho
            {
                Itens = itensCarrinho,
                Subtotal = subtotal,
                TaxaEntrega = taxaEntrega
            };

            return View(carrinho);
        }

        [HttpGet]
        public async Task<IActionResult> Infos()
        {
            // Verificar login
            var usuarioIdStr = HttpContext.Session.GetString("UsuarioId");
            if (string.IsNullOrEmpty(usuarioIdStr))
                return RedirectToAction("Login", "Usuario");
            int usuarioId = Convert.ToInt32(usuarioIdStr);

            // Itens
            var itensCarrinho = await AuxiliarCarrinho
                .ObterItensCarrinho(Request, Response, _produtoRepository)
                ?? new List<ItemPedido>();

            foreach (var it in itensCarrinho)
                it.Imagem ??= it.Produto?.Imagens?.FirstOrDefault();

            decimal subtotal = AuxiliarCarrinho.ObterSubtotal(itensCarrinho);
            decimal taxaEntrega = 10m;

            // Endereços e cartões do usuário
            var enderecos = _carrinhoRepository.ObterEnderecosPorUsuario(usuarioId) ?? new List<Endereco>();
            var cartoes = _carrinhoRepository.ObterCartoesPorUsuario(usuarioId)?.ToList() ?? new List<Cartao>();

            var carrinho = new Carrinho
            {
                Itens = itensCarrinho,
                Enderecos = enderecos,
                Cartoes = cartoes,
                Subtotal = subtotal,
                TaxaEntrega = taxaEntrega,
                NovoEndereco = new Endereco(),
                NovoCartao = new Cartao()
            };

            return View(carrinho);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Adicionar(int produtoId, int quantidade = 1, string? next = "index")
        {
            var produto = await _produtoRepository.ProdutosPorId(produtoId);
            if (produto == null)
            {
                TempData["Erro"] = "Produto não encontrado.";
                return RedirectToAction("Index", "Loja");
            }

            var itens = await AuxiliarCarrinho
                .ObterItensCarrinho(Request, Response, _produtoRepository)
                ?? new List<ItemPedido>();

            var mapa = itens
                .GroupBy(i => i.Produto.Id)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantidade));

            if (mapa.ContainsKey(produtoId)) mapa[produtoId] += quantidade;
            else mapa[produtoId] = quantidade;

            AuxiliarCarrinho.SalvarCarrinho(Response, mapa);

            return (next ?? "index").ToLower() switch
            {
                "infos" => RedirectToAction("Infos", "Carrinho"),
                _ => RedirectToAction("Index", "Carrinho")
            };
        }

        [HttpPost]
        public async Task<IActionResult> SalvarEndereco(Endereco endereco)
        {
            var usuarioIdStr = HttpContext.Session.GetString("UsuarioId");
            if (string.IsNullOrEmpty(usuarioIdStr))
                return RedirectToAction("Login", "Usuario");
            int usuarioId = Convert.ToInt32(usuarioIdStr);
            endereco.UsuarioId = usuarioId;

            if (!ModelState.IsValid)
            {
                var itensCarrinho = await AuxiliarCarrinho.ObterItensCarrinho(Request, Response, _produtoRepository)
                                    ?? new List<ItemPedido>();
                foreach (var it in itensCarrinho) it.Imagem ??= it.Produto?.Imagens?.FirstOrDefault();

                decimal subtotal = AuxiliarCarrinho.ObterSubtotal(itensCarrinho);
                decimal taxaEntrega = 10m;
                var enderecos = _carrinhoRepository.ObterEnderecosPorUsuario(usuarioId)?.ToList() ?? new List<Endereco>();
                var cartoes = _carrinhoRepository.ObterCartoesPorUsuario(usuarioId)?.ToList() ?? new List<Cartao>();

                var carrinho = new Carrinho
                {
                    Itens = itensCarrinho,
                    Enderecos = enderecos,
                    Cartoes = cartoes,
                    Subtotal = subtotal,
                    TaxaEntrega = taxaEntrega,
                    NovoEndereco = endereco,
                    NovoCartao = new Cartao()
                };
                return View("Infos", carrinho);
            }

            try
            {
                _carrinhoRepository.AdicionarEndereco(endereco);
                return RedirectToAction("Infos");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Erro ao salvar o endereço: " + ex.Message);

                var itensCarrinho = await AuxiliarCarrinho.ObterItensCarrinho(Request, Response, _produtoRepository)
                                    ?? new List<ItemPedido>();
                foreach (var it in itensCarrinho) it.Imagem ??= it.Produto?.Imagens?.FirstOrDefault();

                decimal subtotal = AuxiliarCarrinho.ObterSubtotal(itensCarrinho);
                decimal taxaEntrega = 10m;
                var enderecos = _carrinhoRepository.ObterEnderecosPorUsuario(usuarioId)?.ToList() ?? new List<Endereco>();
                var cartoes = _carrinhoRepository.ObterCartoesPorUsuario(usuarioId)?.ToList() ?? new List<Cartao>();

                var carrinho = new Carrinho
                {
                    Itens = itensCarrinho,
                    Enderecos = enderecos,
                    Cartoes = cartoes,
                    Subtotal = subtotal,
                    TaxaEntrega = taxaEntrega,
                    NovoEndereco = endereco,
                    NovoCartao = new Cartao()
                };
                return View("Infos", carrinho);
            }
        }

        [HttpPost]
        public async Task<IActionResult> SalvarCartao(Cartao cartao)
        {
            if (cartao == null)
            {
                ModelState.AddModelError("", "Não foi possível processar o cartão.");
                return RedirectToAction("Infos");
            }

            var usuarioIdStr = HttpContext.Session.GetString("UsuarioId");
            if (string.IsNullOrEmpty(usuarioIdStr))
                return RedirectToAction("Login", "Usuario");

            int usuarioId = Convert.ToInt32(usuarioIdStr);
            cartao.UsuarioId = usuarioId;

            cartao.Bandeira = DetectarBandeiraCartao(cartao.Numero);

            if (!ModelState.IsValid)
            {
                var itensCarrinho = await AuxiliarCarrinho.ObterItensCarrinho(Request, Response, _produtoRepository)
                                    ?? new List<ItemPedido>();
                foreach (var it in itensCarrinho) it.Imagem ??= it.Produto?.Imagens?.FirstOrDefault();

                decimal subtotal = AuxiliarCarrinho.ObterSubtotal(itensCarrinho);
                decimal taxaEntrega = 10m;
                var enderecos = _carrinhoRepository.ObterEnderecosPorUsuario(usuarioId)?.ToList() ?? new List<Endereco>();
                var cartoes = _carrinhoRepository.ObterCartoesPorUsuario(usuarioId)?.ToList() ?? new List<Cartao>();

                var carrinho = new Carrinho
                {
                    Itens = itensCarrinho,
                    Enderecos = enderecos,
                    Cartoes = cartoes,
                    Subtotal = subtotal,
                    TaxaEntrega = taxaEntrega,
                    NovoEndereco = new Endereco(),
                    NovoCartao = cartao
                };
                return View("Infos", carrinho);
            }

            try
            {
                _carrinhoRepository.AdicionarCartao(cartao);
                return RedirectToAction("Infos");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Erro ao salvar o cartão: " + ex.Message);

                var itensCarrinho = await AuxiliarCarrinho.ObterItensCarrinho(Request, Response, _produtoRepository)
                                    ?? new List<ItemPedido>();
                foreach (var it in itensCarrinho) it.Imagem ??= it.Produto?.Imagens?.FirstOrDefault();

                decimal subtotal = AuxiliarCarrinho.ObterSubtotal(itensCarrinho);
                decimal taxaEntrega = 10m;
                var enderecos = _carrinhoRepository.ObterEnderecosPorUsuario(usuarioId)?.ToList() ?? new List<Endereco>();
                var cartoes = _carrinhoRepository.ObterCartoesPorUsuario(usuarioId)?.ToList() ?? new List<Cartao>();

                var carrinho = new Carrinho
                {
                    Itens = itensCarrinho,
                    Enderecos = enderecos,
                    Cartoes = cartoes,
                    Subtotal = subtotal,
                    TaxaEntrega = taxaEntrega,
                    NovoEndereco = new Endereco(),
                    NovoCartao = cartao
                };
                return View("Infos", carrinho);
            }
        }

        [HttpPost]
        public async Task<IActionResult> FinalizarPedido(
            int EnderecoSelecionadoId,
            string MetodoPagamento,
            int? CartaoId,
            string? Logradouro, string? Numero, string? CEP,
            string? Cidade, string? Estado, string? Complemento)
        {
            if (string.IsNullOrWhiteSpace(MetodoPagamento))
            {
                TempData["Erro"] = "Por favor, selecione um método de pagamento antes de continuar.";
                return RedirectToAction("Infos");
            }

            var usuarioIdStr = HttpContext.Session.GetString("UsuarioId");
            if (string.IsNullOrEmpty(usuarioIdStr))
                return RedirectToAction("Login", "Usuario");
            int usuarioId = Convert.ToInt32(usuarioIdStr);

            var itensCarrinho = await AuxiliarCarrinho.ObterItensCarrinho(Request, Response, _produtoRepository);
            if (itensCarrinho == null || !itensCarrinho.Any())
            {
                TempData["Erro"] = "Seu carrinho está vazio.";
                return RedirectToAction("Index");
            }

            if (EnderecoSelecionadoId <= 0)
            {
                bool temAlgumCampo =
                    !string.IsNullOrWhiteSpace(Logradouro) ||
                    !string.IsNullOrWhiteSpace(Numero) ||
                    !string.IsNullOrWhiteSpace(CEP) ||
                    !string.IsNullOrWhiteSpace(Cidade) ||
                    !string.IsNullOrWhiteSpace(Estado) ||
                    !string.IsNullOrWhiteSpace(Complemento);

                if (temAlgumCampo)
                {
                    var novo = new Endereco
                    {
                        UsuarioId = usuarioId,
                        NomeCompleto = "(Entrega)",
                        Logradouro = Logradouro ?? "",
                        Numero = Numero ?? "",
                        CEP = CEP ?? "",
                        Cidade = Cidade ?? "",
                        Estado = Estado ?? "",
                        Complemento = Complemento ?? ""
                    };
                    _carrinhoRepository.AdicionarEndereco(novo);

                    if (novo.Id <= 0)
                    {
                        var todos = _carrinhoRepository.ObterEnderecosPorUsuario(usuarioId) ?? new List<Endereco>();
                        EnderecoSelecionadoId = todos.OrderByDescending(e => e.Id).Select(e => e.Id).FirstOrDefault();
                    }
                    else EnderecoSelecionadoId = novo.Id;
                }
                else
                {
                    var novo = new Endereco
                    {
                        UsuarioId = usuarioId,
                        NomeCompleto = "(Entrega)",
                        Logradouro = "Não informado",
                        Numero = "s/n",
                        Cidade = "–",
                        Estado = "–",
                        CEP = "–"
                    };
                    _carrinhoRepository.AdicionarEndereco(novo);
                    EnderecoSelecionadoId = novo.Id > 0 ? novo.Id :
                        (_carrinhoRepository.ObterEnderecosPorUsuario(usuarioId) ?? new List<Endereco>())
                        .OrderByDescending(e => e.Id).Select(e => e.Id).FirstOrDefault();
                }
            }

            decimal subtotal = itensCarrinho.Sum(i => i.PrecoUnitario * i.Quantidade);
            decimal taxaEntrega = 10m;
            decimal valorTotal = (MetodoPagamento == "Pix")
                ? (subtotal + taxaEntrega) * 0.95m
                : subtotal + taxaEntrega;

            int? cartaoIdFinal = (MetodoPagamento == "Pix") ? null : CartaoId;

            var pedido = new Pedido
            {
                UsuarioId = usuarioId,
                EnderecoId = EnderecoSelecionadoId,
                MetodoPagamentoId = (MetodoPagamento == "Pix") ? 2 : 1,
                CartaoId = cartaoIdFinal,
                TaxaEntrega = taxaEntrega,
                ValorTotal = valorTotal,
                StatusPagamento = "Aguardando Pagamento",
                DataPedido = DateTime.Now,
                Itens = itensCarrinho
            };

            try
            {
                int pedidoId = await _pedidoRepository.AdicionarPedido(pedido);
                AuxiliarCarrinho.SalvarCarrinho(Response, new Dictionary<int, int>());

                return (MetodoPagamento == "Pix")
                    ? RedirectToAction("Pix", "Carrinho", new { pedidoId })
                    : RedirectToAction("ConfirmarCartao", "Carrinho", new { pedidoId });
            }
            catch (Exception ex)
            {
                TempData["Erro"] = "Erro ao finalizar pedido: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Pix(int pedidoId)
        {
            var usuarioIdStr = HttpContext.Session.GetString("UsuarioId");
            if (string.IsNullOrEmpty(usuarioIdStr))
                return RedirectToAction("Login", "Usuario");
            int usuarioId = Convert.ToInt32(usuarioIdStr);

            var pedido = await _pedidoRepository.ObterPedidoPorId(pedidoId);
            if (pedido == null)
            {
                TempData["Erro"] = "Pedido não encontrado.";
                return RedirectToAction("Index");
            }

            var itensCarrinho = pedido.Itens?.ToList() ?? new List<ItemPedido>();
            foreach (var it in itensCarrinho) it.Imagem ??= it.Produto?.Imagens?.FirstOrDefault();

            decimal subtotal = itensCarrinho.Sum(i => i.PrecoUnitario * i.Quantidade);
            decimal taxaEntrega = pedido.TaxaEntrega;

            var enderecos = _carrinhoRepository.ObterEnderecosPorUsuario(usuarioId) ?? new List<Endereco>();
            var cartoes = _carrinhoRepository.ObterCartoesPorUsuario(usuarioId)?.ToList() ?? new List<Cartao>();

            var carrinho = new Carrinho
            {
                Itens = itensCarrinho,
                Enderecos = enderecos,
                Cartoes = cartoes,
                ValorTotal = pedido.ValorTotal,
                TaxaEntrega = taxaEntrega,
                NovoEndereco = new Endereco(),
                NovoCartao = new Cartao(),
                MetodoPagamento = "Pix",
                CriadoEm = pedido.DataPedido,
                Expiracao = pedido.DataPedido.AddMinutes(30)
            };

            ViewBag.CodigoPix = $"PIX-{pedido.Id}-{Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()}";
            ViewBag.PedidoId = pedido.Id;

            return View(carrinho);
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmarPagamento(int pedidoId)
        {
            var usuarioIdStr = HttpContext.Session.GetString("UsuarioId");
            if (string.IsNullOrEmpty(usuarioIdStr))
                return RedirectToAction("Login", "Usuario");

            try
            {
                var pedido = await _pedidoRepository.ObterPedidoPorId(pedidoId);
                if (pedido == null)
                {
                    TempData["Erro"] = "Pedido não encontrado.";
                    return RedirectToAction("Index");
                }

                pedido.StatusPagamento = "Pago";
                pedido.DataPagamento = DateTime.Now;

                var linhas = await _pedidoRepository.AtualizarStatusPagamento(pedido);
                if (linhas <= 0)
                {
                    TempData["Erro"] = "Não foi possível atualizar o status do pagamento.";
                    return RedirectToAction("Pix", new { pedidoId });
                }

                TempData["Sucesso"] = "Pagamento confirmado com sucesso!";
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                TempData["Erro"] = "Erro ao confirmar pagamento: " + ex.Message;
                return RedirectToAction("Pix", new { pedidoId });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ConfirmarCartao(int pedidoId)
        {
            if (pedidoId <= 0)
            {
                TempData["Erro"] = "Pedido inválido.";
                return RedirectToAction("Index");
            }

            var pedido = await _pedidoRepository.ObterPedidoPorId(pedidoId);
            if (pedido == null)
            {
                TempData["Erro"] = "Pedido não encontrado.";
                return RedirectToAction("Index");
            }

            var carrinho = new Carrinho
            {
                Itens = pedido.Itens,
                Subtotal = pedido.Itens.Sum(i => i.PrecoUnitario * i.Quantidade),
                TaxaEntrega = pedido.TaxaEntrega,
                CartaoSelecionadoId = pedido.CartaoId,
                Enderecos = new List<Endereco>(),
                Cartoes = new List<Cartao>()
            };

            ViewBag.PedidoId = pedido.Id;
            return View(carrinho);
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmarPagamentoCartao(int pedidoId)
        {
            var usuarioIdStr = HttpContext.Session.GetString("UsuarioId");
            if (string.IsNullOrEmpty(usuarioIdStr))
                return RedirectToAction("Login", "Usuario");

            var pedido = await _pedidoRepository.ObterPedidoPorId(pedidoId);
            if (pedido == null)
            {
                TempData["Erro"] = "Pedido não encontrado.";
                return RedirectToAction("Index");
            }

            pedido.StatusPagamento = "Pago";
            pedido.DataPagamento = DateTime.Now;

            var linhas = await _pedidoRepository.AtualizarStatusPagamento(pedido);
            if (linhas <= 0)
            {
                TempData["Erro"] = "Não foi possível atualizar o status do pagamento.";
                return RedirectToAction("ConfirmarCartao", new { pedidoId });
            }

            TempData["Sucesso"] = "Pagamento confirmado com sucesso!";
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult ConfirmPag(int pedidoId)
        {
            ViewBag.PedidoId = pedidoId;
            return View("ConfirmPag");
        }

        private static string? DetectarBandeiraCartao(string? numeroCartao)
        {
            if (string.IsNullOrWhiteSpace(numeroCartao))
                return null;

            var digits = new string(numeroCartao.Where(char.IsDigit).ToArray());
            if (digits.Length < 4)
                return null;

            // elo
            var eloPrefixes = new[]
            {
                "4011", "4312", "4389"
                
            };

            if (eloPrefixes.Any(p => digits.StartsWith(p)))
                return "Elo";

            // --- American Express ---
            if (digits.StartsWith("34") || digits.StartsWith("37"))
                return "American Express";

            // --- Hipercard ---
            if (digits.StartsWith("6062"))
                return "Hipercard";

            // --- Mastercard (51–55 ou 2221–2720) ---
            if (digits.Length >= 2)
            {
                var first2 = int.Parse(digits.Substring(0, 2));
                var first4 = digits.Length >= 4 ? int.Parse(digits.Substring(0, 4)) : -1;

                if ((first2 >= 51 && first2 <= 55) || (first4 >= 2221 && first4 <= 2720))
                    return "Mastercard";
            }

            // --- Visa ---
            if (digits.StartsWith("4"))
                return "Visa";

            return null;
        }
    }
}
