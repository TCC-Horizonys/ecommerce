using Newtonsoft.Json;
using System.Text;
using ecommerce.Models;
using ecommerce.Repository;

namespace ecommerce.Services
{
    public static class AuxiliarCarrinho
    {
        /// <summary>
        /// Lê o cookie "shopping_cart" e retorna um dicionário com os produtos e suas quantidades.
        /// </summary>
        public static async Task<List<ItemPedido>> ObterItensCarrinho(HttpRequest requisicao, HttpResponse resposta, IProdutoRepository produtoRepository)
        {
            var itens = new List<ItemPedido>();
            var dicionario = ObterDicionarioCarrinho(requisicao, resposta);

            foreach (var par in dicionario)
            {
                var produto = await produtoRepository.ProdutosPorId(par.Key);
                if (produto == null) continue;

                itens.Add(new ItemPedido
                {
                    ProdutoId = produto.Id,
                    Produto = produto,
                    Quantidade = par.Value,
                    PrecoUnitario = produto.Preco,
                    Imagem = produto.Imagens.FirstOrDefault()
                });
            }

            return itens;
        }

        /// <summary>
        /// Retorna o total de itens no carrinho.
        /// </summary>
        public static int ObterTamanhoCarrinho(HttpRequest requisicao, HttpResponse resposta)
        {
            int tamanho = 0;
            var carrinho = ObterDicionarioCarrinho(requisicao, resposta);

            foreach (var item in carrinho)
            {
                tamanho += item.Value;
            }

            return tamanho;
        }

        /// <summary>
        /// Calcula o subtotal do carrinho.
        /// </summary>
        public static decimal ObterSubtotal(List<ItemPedido> itensCarrinho)
        {
            decimal subtotal = 0;

            foreach (var item in itensCarrinho)
            {
                subtotal += item.Quantidade * item.PrecoUnitario;
            }

            return subtotal;
        }

        /// <summary>
        /// Salva o estado atual do carrinho no cookie (JSON + Base64).
        /// </summary>
        public static void SalvarCarrinho(HttpResponse resposta, Dictionary<int, int> carrinho)
        {
            string json = JsonConvert.SerializeObject(carrinho);
            string valorBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));

            var opcoes = new CookieOptions
            {
                Expires = DateTime.Now.AddDays(365),
                Path = "/",
                SameSite = SameSiteMode.Strict,
                Secure = true
            };

            resposta.Cookies.Append("shopping_cart", valorBase64, opcoes);
        }

        /// <summary>
        /// Adiciona um item ao carrinho (ou incrementa se já existir).
        /// </summary>
        public static void AdicionarAoCarrinho(HttpRequest requisicao, HttpResponse resposta, int produtoId)
        {
            var carrinho = ObterDicionarioCarrinho(requisicao, resposta);

            if (carrinho.ContainsKey(produtoId))
                carrinho[produtoId]++;
            else
                carrinho[produtoId] = 1;

            SalvarCarrinho(resposta, carrinho);
        }

        /// <summary>
        /// Diminui a quantidade de um item no carrinho.
        /// </summary>
        public static void DiminuirDoCarrinho(HttpRequest requisicao, HttpResponse resposta, int produtoId)
        {
            var carrinho = ObterDicionarioCarrinho(requisicao, resposta);

            if (!carrinho.ContainsKey(produtoId))
                return;

            if (carrinho[produtoId] > 1)
                carrinho[produtoId]--;
            else
                carrinho.Remove(produtoId);

            SalvarCarrinho(resposta, carrinho);
        }

        /// <summary>
        /// Remove completamente um produto do carrinho.
        /// </summary>
        public static void RemoverDoCarrinho(HttpRequest requisicao, HttpResponse resposta, int produtoId)
        {
            var carrinho = ObterDicionarioCarrinho(requisicao, resposta);

            if (carrinho.ContainsKey(produtoId))
            {
                carrinho.Remove(produtoId);
                SalvarCarrinho(resposta, carrinho);
            }
        }

        /// <summary>
        /// Recupera o cookie e converte de Base64 + JSON para dicionário de produtos.
        /// </summary>
        private static Dictionary<int, int> ObterDicionarioCarrinho(HttpRequest requisicao, HttpResponse resposta)
        {
            try
            {
                // Verifica se o cookie existe
                if (!requisicao.Cookies.TryGetValue("shopping_cart", out string? valorBase64) || string.IsNullOrEmpty(valorBase64))
                {
                    return new Dictionary<int, int>();
                }

                // Decodifica de Base64 e depois desserializa de JSON
                string json = Encoding.UTF8.GetString(Convert.FromBase64String(valorBase64));
                var dicionario = JsonConvert.DeserializeObject<Dictionary<int, int>>(json);

                // Se deu erro ou está nulo, devolve dicionário vazio
                return dicionario ?? new Dictionary<int, int>();
            }
            catch
            {
                // Em caso de erro, retorna carrinho vazio
                return new Dictionary<int, int>();
            }
        }
    }
}
