using ecommerce.Models;
using ecommerce.Repository;
using ecommerce.Services;
using Microsoft.AspNetCore.Mvc;

namespace ecommerce.Controllers
{
    public class CarrinhoController : Controller
    {
        private readonly IProdutoRepository _produtoRepository;
        private readonly IPedidoRepository _pedidoRepository;
        private readonly CarrinhoRepository _carrinhoRepository;

        public CarrinhoController(IProdutoRepository produtoRepository,
            CarrinhoRepository carrinhoRepository, IPedidoRepository pedidoRepository)
        {
            _produtoRepository = produtoRepository;
            _carrinhoRepository = carrinhoRepository;
            _pedidoRepository = pedidoRepository;

        }

        // Página do carrinho
        public async Task<IActionResult> Index()
        {
            // Obter itens do carrinho
            var itensCarrinho = await AuxiliarCarrinho.ObterItensCarrinho(Request, Response, _produtoRepository)
                                ?? new List<ItemPedido>();

            decimal subtotal = AuxiliarCarrinho.ObterSubtotal(itensCarrinho);
            decimal taxaEntrega = 10m; // valor fixo, pode ajustar se necessário

            // Montar o model Carrinho
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
            // Verificar se usuário está logado
            var usuarioIdStr = HttpContext.Session.GetString("UsuarioId");
            if (string.IsNullOrEmpty(usuarioIdStr))
            {
                return RedirectToAction("Login", "Usuario");
            }
            int usuarioId = Convert.ToInt32(usuarioIdStr);

            // Obter itens do carrinho (ItemPedido)
            var itensCarrinho = await AuxiliarCarrinho.ObterItensCarrinho(Request, Response, _produtoRepository)
                                ?? new List<ItemPedido>();

            decimal subtotal = AuxiliarCarrinho.ObterSubtotal(itensCarrinho);
            decimal taxaEntrega = 10m; // Valor fixo

            // Obter endereços do usuário
            var enderecos = _carrinhoRepository.ObterEnderecosPorUsuario(usuarioId) ?? new List<Endereco>();

            // Obter cartões do usuário
            var cartoes = _carrinhoRepository.ObterCartoesPorUsuario(usuarioId)?.ToList() ?? new List<Cartao>();


            // Montar o model Carrinho
            var carrinho = new Carrinho
            {
                Itens = itensCarrinho,   // <== agora é List<ItemPedido>
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
            // valida produto
            var produto = await _produtoRepository.ProdutosPorId(produtoId);
            if (produto == null)
            {
                TempData["Erro"] = "Produto não encontrado.";
                return RedirectToAction("Index", "Loja");
            }

            var itens = await AuxiliarCarrinho.ObterItensCarrinho(Request, Response, _produtoRepository)
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
            // Verifica se o usuário está logado
            var usuarioIdStr = HttpContext.Session.GetString("UsuarioId");
            if (string.IsNullOrEmpty(usuarioIdStr))
                return RedirectToAction("Login", "Usuario");

            int usuarioId = Convert.ToInt32(usuarioIdStr);
            endereco.UsuarioId = usuarioId;

            // Se o model estiver inválido
            if (!ModelState.IsValid)
            {
                // Recarrega o carrinho
                var itensCarrinho = await AuxiliarCarrinho.ObterItensCarrinho(Request, Response, _produtoRepository)
                                    ?? new List<ItemPedido>();
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
                    NovoEndereco = endereco,   // mantém os dados preenchidos
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

                // Recarrega o carrinho em caso de erro
                var itensCarrinho = await AuxiliarCarrinho.ObterItensCarrinho(Request, Response, _produtoRepository)
                                    ?? new List<ItemPedido>();
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
            // Garante que o model foi recebido
            if (cartao == null)
            {
                ModelState.AddModelError("", "Não foi possível processar o cartão.");
                return RedirectToAction("Infos");
            }

            // Verifica se o usuário está logado
            var usuarioIdStr = HttpContext.Session.GetString("UsuarioId");
            if (string.IsNullOrEmpty(usuarioIdStr))
                return RedirectToAction("Login", "Usuario");

            int usuarioId = Convert.ToInt32(usuarioIdStr);
            cartao.UsuarioId = usuarioId;

            // Se o ModelState for inválido
            if (!ModelState.IsValid)
            {
                // Recarrega o carrinho completo
                var itensCarrinho = await AuxiliarCarrinho.ObterItensCarrinho(Request, Response, _produtoRepository)
                                    ?? new List<ItemPedido>();
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
                    NovoCartao = cartao // mantém os dados preenchidos do cartão
                };

                return View("Infos", carrinho);
            }

            try
            {
                // Salvar cartão no banco
                _carrinhoRepository.AdicionarCartao(cartao);

                return RedirectToAction("Infos");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Erro ao salvar o cartão: " + ex.Message);

                // Recarrega o carrinho completo em caso de erro
                var itensCarrinho = await AuxiliarCarrinho.ObterItensCarrinho(Request, Response, _produtoRepository)
                                    ?? new List<ItemPedido>();
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

    // campos do endereço digitado na própria tela
    string? Logradouro, string? Numero, string? CEP,
    string? Cidade, string? Estado, string? Complemento)
        {
            // valida método
            if (string.IsNullOrWhiteSpace(MetodoPagamento))
            {
                TempData["Erro"] = "Por favor, selecione um método de pagamento antes de continuar.";
                return RedirectToAction("Infos");
            }

            // usuário
            var usuarioIdStr = HttpContext.Session.GetString("UsuarioId");
            if (string.IsNullOrEmpty(usuarioIdStr)) return RedirectToAction("Login", "Usuario");
            int usuarioId = Convert.ToInt32(usuarioIdStr);

            // itens do carrinho
            var itensCarrinho = await AuxiliarCarrinho.ObterItensCarrinho(Request, Response, _produtoRepository);
            if (itensCarrinho == null || !itensCarrinho.Any())
            {
                TempData["Erro"] = "Seu carrinho está vazio.";
                return RedirectToAction("Index");
            }

            // **NOVA LÓGICA**: se não veio endereço selecionado, tenta criar a partir dos campos da tela
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

                    // salva e recupera o Id
                    _carrinhoRepository.AdicionarEndereco(novo);

                    // se o repositório não popula o Id, pegue o último do usuário:
                    if (novo.Id <= 0)
                    {
                        var todos = _carrinhoRepository.ObterEnderecosPorUsuario(usuarioId) ?? new List<Endereco>();
                        EnderecoSelecionadoId = todos.OrderByDescending(e => e.Id).Select(e => e.Id).FirstOrDefault();
                    }
                    else
                    {
                        EnderecoSelecionadoId = novo.Id;
                    }
                }
                else
                {
                    // ainda sem endereço => cria um mínimo “rápido”
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

            // cálculo
            decimal subtotal = itensCarrinho.Sum(i => i.PrecoUnitario * i.Quantidade);
            decimal taxaEntrega = 10m;
            decimal valorTotal = MetodoPagamento == "Pix"
                ? (subtotal + taxaEntrega) * 0.95m
                : subtotal + taxaEntrega;

            int? cartaoIdFinal = MetodoPagamento == "Pix" ? null : CartaoId;

            var pedido = new Pedido
            {
                UsuarioId = usuarioId,
                EnderecoId = EnderecoSelecionadoId,     // agora SEM BLOQUEIO
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
            {
                return RedirectToAction("Login", "Usuario");
            }
            int usuarioId = Convert.ToInt32(usuarioIdStr);

            var pedido = await _pedidoRepository.ObterPedidoPorId(pedidoId);
            if (pedido == null)
            {
                TempData["Erro"] = "Pedido não encontrado.";
                return RedirectToAction("Index");
            }

            var itensCarrinho = pedido.Itens?.ToList() ?? new List<ItemPedido>();
            decimal subtotal = itensCarrinho.Sum(i => i.PrecoUnitario * i.Quantidade);
            decimal taxaEntrega = pedido.TaxaEntrega;

            // Buscar endereços e cartões do usuário
            var enderecos = _carrinhoRepository.ObterEnderecosPorUsuario(usuarioId) ?? new List<Endereco>();
            var cartoes = _carrinhoRepository.ObterCartoesPorUsuario(usuarioId)?.ToList() ?? new List<Cartao>();

            // Montar o carrinho completo
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

            // Gerar código Pix fictício
            ViewBag.CodigoPix = $"PIX-{pedido.Id}-{Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()}";
            ViewBag.PedidoId = pedido.Id;


            return View(carrinho);
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmarPagamento(int pedidoId)
        {

            var usuarioIdStr = HttpContext.Session.GetString("UsuarioId");
            if (string.IsNullOrEmpty(usuarioIdStr))
            {
                return RedirectToAction("Login", "Usuario");
            }

            int usuarioId = Convert.ToInt32(usuarioIdStr);

            try
            {
                var pedido = await _pedidoRepository.ObterPedidoPorId(pedidoId);
                if (pedido == null)
                {
                    TempData["Erro"] = "Pedido não encontrado.";
                    return RedirectToAction("Index");
                }

                // Atualizar status e data do pagamento
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

            // Obter pedido do banco
            var pedido = await _pedidoRepository.ObterPedidoPorId(pedidoId);
            if (pedido == null)
            {
                TempData["Erro"] = "Pedido não encontrado.";
                return RedirectToAction("Index");
            }

            // Converter Pedido em Carrinho
            var carrinho = new Carrinho
            {
                Itens = pedido.Itens,
                Subtotal = pedido.Itens.Sum(i => i.PrecoUnitario * i.Quantidade),
                TaxaEntrega = pedido.TaxaEntrega,
                CartaoSelecionadoId = pedido.CartaoId,
                Enderecos = new List<Endereco>(), // opcional, se quiser exibir endereços
                Cartoes = new List<Cartao>() // opcional, se quiser exibir cartões
            };

            ViewBag.PedidoId = pedido.Id; // necessário para o POST

            return View(carrinho); // continua usando @model Carrinho
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmarPagamentoCartao(int pedidoId)
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
            // monte o model que a view precisa, se houver
            return View("ConfirmPag");
        }


    }

}