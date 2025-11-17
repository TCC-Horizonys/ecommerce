using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mysqlx.Crud;
using ecommerce.Models;
using ecommerce.Repository;

namespace ecommerce.Controllers
{
    [Route("/Admin/[controller]/{action=Index}/{id?}")]
    public class ProdutoController : Controller
    {

        private readonly IProdutoRepository _produtoRepository;
        private readonly int pagTam = 5;

        public ProdutoController(IProdutoRepository produtoRepository)
        {
            _produtoRepository = produtoRepository;
        }

        public async Task<IActionResult> Index(int pagIndex, string? buscar = null, string? coluna = "Id", string? ordPor = "desc")
        {
            var produtos = await _produtoRepository.ProdutosOrdenados();

            // Busca
            if (!string.IsNullOrEmpty(buscar))
            {
                produtos = produtos
                    .Where(p => p.Nome.Contains(buscar, StringComparison.OrdinalIgnoreCase)
                             || p.Categoria.Contains(buscar, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                pagIndex = 1; // reset para página 1
            }

            // Normaliza coluna
            coluna ??= "Id";
            ordPor ??= "desc";

            // Ordenação
            produtos = coluna switch
            {
                "Nome" => ordPor == "asc" ? produtos.OrderBy(p => p.Nome).ToList() : produtos.OrderByDescending(p => p.Nome).ToList(),
                "Categoria" => ordPor == "asc" ? produtos.OrderBy(p => p.Categoria).ToList() : produtos.OrderByDescending(p => p.Categoria).ToList(),
                "Preco" => ordPor == "asc" ? produtos.OrderBy(p => p.Preco).ToList() : produtos.OrderByDescending(p => p.Preco).ToList(),
                "Descricao" => ordPor == "asc" ? produtos.OrderBy(p => p.Descricao).ToList() : produtos.OrderByDescending(p => p.Descricao).ToList(),
                "EstadoConservacao" => ordPor == "asc" ? produtos.OrderBy(p => p.EstadoConservacao).ToList() : produtos.OrderByDescending(p => p.EstadoConservacao).ToList(),
                "DataCriada" => ordPor == "asc" ? produtos.OrderBy(p => p.DataCriada).ToList() : produtos.OrderByDescending(p => p.DataCriada).ToList(),
                _ => ordPor == "asc" ? produtos.OrderBy(p => p.Id).ToList() : produtos.OrderByDescending(p => p.Id).ToList(),
            };

            if (pagIndex < 1) pagIndex = 1;

            // Total de itens e páginas
            int count = produtos.Count();
            int totalPag = (int)Math.Ceiling((decimal)count / pagTam);

            // Garantir que a página atual não ultrapasse o total de páginas (ou seja 1 se não houver registros)
            if (totalPag == 0) totalPag = 1;
            if (pagIndex > totalPag) pagIndex = totalPag;

            // Pegar produtos da página atual
            var produtosPaginados = produtos
                .Skip((pagIndex - 1) * pagTam)
                .Take(pagTam)
                .ToList();


            // Dados para a view
            ViewData["PagIndex"] = pagIndex;
            ViewData["TotalPag"] = totalPag;

            ViewData["Buscar"] = buscar ?? "";

            ViewData["Coluna"] = coluna;
            ViewData["OrdPor"] = ordPor;

            return View(produtosPaginados);
        }

        public IActionResult Criar()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Criar(Produto produto, IFormFile[] imagensArquivo)
        {
            if (!ModelState.IsValid)
                return View(produto);

            if (imagensArquivo == null || imagensArquivo.Length == 0)
            {
                ModelState.AddModelError("Imagens", "Pelo menos uma imagem é obrigatória.");
                return View(produto);
            }

            produto.Imagens = new List<ProdutoImagem>();
            int ordem = 1;

            foreach (var imagemArquivo in imagensArquivo)
            {
                if (imagemArquivo != null && imagemArquivo.Length > 0)
                {
                    // Define o caminho para salvar
                    var caminhoPasta = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img");

                    // Garante que a pasta exista
                    if (!Directory.Exists(caminhoPasta))
                        Directory.CreateDirectory(caminhoPasta);

                    // Gera um nome único para o arquivo
                    var nomeArquivo = Guid.NewGuid().ToString() + Path.GetExtension(imagemArquivo.FileName);
                    var caminhoCompleto = Path.Combine(caminhoPasta, nomeArquivo);

                    // Salva o arquivo
                    using (var stream = new FileStream(caminhoCompleto, FileMode.Create))
                    {
                        await imagemArquivo.CopyToAsync(stream);
                    }

                    // Adiciona à lista de imagens do produto com ordem
                    produto.Imagens.Add(new ProdutoImagem
                    {
                        Url = nomeArquivo,
                        OrdemImagem = ordem
                    });

                    ordem++;
                }
            }

            // Chama o repositório para salvar o produto e suas imagens
            await _produtoRepository.AdicionarProduto(produto);

            return RedirectToAction("Index");
        }



        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            var produto = await _produtoRepository.ProdutosPorId(id);
            if (produto == null)
                return RedirectToAction("Index", "Produto");

            return View(produto);
        }

        [HttpPost]
        public async Task<IActionResult> Editar(Produto produto, IFormFile[] imagensArquivo)
        {
            var produtoExistente = await _produtoRepository.ProdutosPorId(produto.Id);
            if (produtoExistente == null)
                return RedirectToAction("Index", "Produto");

            var caminhoPasta = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img");

            // Criar pasta se necessário
            if (!Directory.Exists(caminhoPasta))
                Directory.CreateDirectory(caminhoPasta);

            // Lista de imagens atualizada
            produto.Imagens = produtoExistente.Imagens ?? new List<ProdutoImagem>();
            int ordem = produto.Imagens.Any() ? produto.Imagens.Max(i => i.OrdemImagem) + 1 : 1;

            if (imagensArquivo != null && imagensArquivo.Length > 0)
            {
                foreach (var imagemArquivo in imagensArquivo)
                {
                    if (imagemArquivo != null && imagemArquivo.Length > 0)
                    {
                        // Salvar nova imagem
                        var nomeArquivo = Guid.NewGuid().ToString() + Path.GetExtension(imagemArquivo.FileName);
                        var caminhoCompleto = Path.Combine(caminhoPasta, nomeArquivo);

                        using (var stream = new FileStream(caminhoCompleto, FileMode.Create))
                        {
                            await imagemArquivo.CopyToAsync(stream);
                        }

                        // Adiciona à lista de imagens
                        produto.Imagens.Add(new ProdutoImagem
                        {
                            Url = nomeArquivo,
                            OrdemImagem = ordem
                        });

                        ordem++;
                    }
                }
            }

            // Atualiza os dados do produto (nome, preço, etc.) e mantém a lista de imagens
            await _produtoRepository.AtualizarProduto(produto);

            return RedirectToAction("Index");
        }


        [HttpPost]
        public async Task<IActionResult> Deletar(int id)
        {
            var produto = await _produtoRepository.ProdutosPorId(id);
            if (produto == null)
            {
                return RedirectToAction("Index");
            }

            // Apaga todas as imagens associadas (se existirem)
            if (produto.Imagens != null && produto.Imagens.Any())
            {
                foreach (var imagem in produto.Imagens)
                {
                    if (!string.IsNullOrWhiteSpace(imagem.Url))
                    {
                        // Garante que o caminho esteja correto (sem barras duplicadas)
                        var caminhoImagem = Path.Combine(
                            Directory.GetCurrentDirectory(),
                            "wwwroot",
                            imagem.Url.TrimStart('/', '\\')
                        );

                        if (System.IO.File.Exists(caminhoImagem))
                        {
                            try
                            {
                                System.IO.File.Delete(caminhoImagem);
                            }
                            catch (Exception ex)
                            {
                                // Logar ou ignorar falha ao deletar imagem específica
                                Console.WriteLine($"Erro ao deletar imagem {caminhoImagem}: {ex.Message}");
                            }
                        }
                    }
                }
            }

            // Exclui o produto e suas imagens do banco
            await _produtoRepository.DeletarProduto(id);

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> RemoverImagem(int id)
        {
            var imagem = await _produtoRepository.ImagemPorId(id); // funciona com ImagemId
            if (imagem == null)
                return Json(new { sucesso = false, mensagem = "Imagem não encontrada." });

            try
            {
                var caminho = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", imagem.Url.TrimStart('/'));
                if (System.IO.File.Exists(caminho))
                    System.IO.File.Delete(caminho);

                await _produtoRepository.DeletarImagem(id);
            }
            catch
            {
                return Json(new { sucesso = false, mensagem = "Erro ao remover imagem." });
            }

            return Json(new { sucesso = true });
        }

    }
}
