using MySql.Data.MySqlClient;
using ecommerce.Models;
using Dapper;

namespace ecommerce.Repository
{
    public class ProdutoRepository : IProdutoRepository
    {
        private readonly string _connectionString;


        public ProdutoRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<List<Produto>> TodosProdutos()
        {
            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            var sql = @"
SELECT 
    p.Id, p.Nome, p.Categoria,  p.Descricao, p.EstadoConservacao, p.Preco,  p.DataCriada,
    pi.Id AS ImagemId, pi.ProdutoId AS ImagemProdutoId, pi.Url, pi.OrdemImagem
FROM produtos p
LEFT JOIN produtoImagens pi ON pi.ProdutoId = p.Id
ORDER BY p.Id, pi.OrdemImagem;
";

            var produtoDict = new Dictionary<int, Produto>();

            var produtos = await conn.QueryAsync<Produto, ProdutoImagem, Produto>(
                sql,
                (p, img) =>
                {
                    if (!produtoDict.TryGetValue(p.Id, out var produtoEntry))
                    {
                        produtoEntry = p;
                        produtoEntry.Imagens = new List<ProdutoImagem>();
                        produtoDict.Add(p.Id, produtoEntry);
                    }

                    // Note: ProdutoImagem.ImagemId mapped from pi.Id AS ImagemId
                    if (img != null && img.ImagemId != 0)
                        produtoEntry.Imagens.Add(img);

                    return produtoEntry;
                },
                splitOn: "ImagemId"
            );

            // já tem debug console — bom para confirmar
            foreach (var p in produtoDict.Values)
            {
                Console.WriteLine($"Produto {p.Nome} tem {p.Imagens.Count} imagens");
                foreach (var img in p.Imagens)
                {
                    Console.WriteLine($" - {img.Url} (ImagemId={img.ImagemId})");
                }
            }

            return produtoDict.Values.ToList();
        }

        public async Task<List<Produto>> ProdutosOrdenados()
        {
            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            var sql = @"
SELECT 
    p.Id, p.Nome, p.Categoria,  p.Descricao, p.EstadoConservacao, p.Preco,  p.DataCriada,
    pi.Id AS ImagemId, pi.ProdutoId AS ImagemProdutoId, pi.Url, pi.OrdemImagem
FROM produtos p
LEFT JOIN produtoImagens pi ON pi.ProdutoId = p.Id
ORDER BY p.Id DESC, pi.OrdemImagem;
";

            var produtoDict = new Dictionary<int, Produto>();

            var produtos = await conn.QueryAsync<Produto, ProdutoImagem, Produto>(
                sql,
                (p, img) =>
                {
                    if (!produtoDict.TryGetValue(p.Id, out var produtoEntry))
                    {
                        produtoEntry = p;
                        produtoEntry.Imagens = new List<ProdutoImagem>();
                        produtoDict.Add(p.Id, produtoEntry);
                    }

                    // Note: ProdutoImagem.ImagemId mapped from pi.Id AS ImagemId
                    if (img != null && img.ImagemId != 0)
                        produtoEntry.Imagens.Add(img);

                    return produtoEntry;
                },
                splitOn: "ImagemId"
            );

            // já tem debug console — bom para confirmar
            foreach (var p in produtoDict.Values)
            {
                Console.WriteLine($"Produto {p.Nome} tem {p.Imagens.Count} imagens");
                foreach (var img in p.Imagens)
                {
                    Console.WriteLine($" - {img.Url} (ImagemId={img.ImagemId})");
                }
            }

            return produtoDict.Values.ToList();
        }

        public async Task<int> AdicionarProduto(Produto produto)
        {
            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            using var transaction = conn.BeginTransaction();

            try
            {
                // Inserir produto (sem ImageUrl)
                var sqlProduto = @"
            INSERT INTO Produtos (Nome, Categoria, Descricao, Preco, EstadoConservacao, DataCriada)
            VALUES (@Nome, @Categoria, @Descricao, @Preco, @EstadoConservacao, @DataCriada);
            SELECT LAST_INSERT_ID();";

                produto.Id = await conn.ExecuteScalarAsync<int>(sqlProduto, produto, transaction);

                // Inserir imagens (se existirem)
                if (produto.Imagens != null && produto.Imagens.Count > 0)
                {
                    var sqlImagem = @"
                INSERT INTO ProdutoImagens (ProdutoId, Url, OrdemImagem)
                VALUES (@ProdutoId, @Url, @OrdemImagem);";

                    foreach (var img in produto.Imagens)
                    {
                        img.ProdutoId = produto.Id; // garante FK correta
                                                    // Se OrdemImagem não estiver setada, coloca 1 como default
                        if (img.OrdemImagem == 0) img.OrdemImagem = 1;

                        await conn.ExecuteAsync(sqlImagem, new
                        {
                            ProdutoId = img.ProdutoId,
                            Url = img.Url,
                            OrdemImagem = img.OrdemImagem
                        }, transaction);
                    }
                }

                transaction.Commit();
                return produto.Id;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }


        public async Task<Produto?> ProdutosPorId(int id)
        {
            using var connection = new MySqlConnection(_connectionString);

            var sql = @"
        SELECT p.*, pi.Id AS ImagemId, pi.ProdutoId, pi.Url, pi.OrdemImagem
        FROM produtos p
        LEFT JOIN produtoImagens pi ON pi.ProdutoId = p.Id
        WHERE p.Id = @Id
        ORDER BY pi.OrdemImagem;";

            var produtoDict = new Dictionary<int, Produto>();

            var resultado = await connection.QueryAsync<Produto, ProdutoImagem, Produto>(
                sql,
                (p, img) =>
                {
                    if (!produtoDict.TryGetValue(p.Id, out var produtoEntry))
                    {
                        produtoEntry = p;
                        produtoEntry.Imagens = new List<ProdutoImagem>();
                        produtoDict.Add(p.Id, produtoEntry);
                    }

                    if (img != null && img.ImagemId != 0)
                        produtoEntry.Imagens.Add(img);

                    return produtoEntry;
                },
                new { Id = id },
                splitOn: "ImagemId"
            );

            return produtoDict.Values.FirstOrDefault();
        }


        public async Task AtualizarProduto(Produto produto)
        {
            using var connection = new MySqlConnection(_connectionString);

            // Atualiza os campos do produto
            var sql = @"
        UPDATE Produtos
        SET Nome = @Nome, Categoria = @Categoria, Descricao=@Descricao, EstadoConservacao = @EstadoConservacao, 
            Preco = @Preco
        WHERE Id = @Id;
    ";

            await connection.ExecuteAsync(sql, produto);

            // Atualiza / insere imagens
            if (produto.Imagens != null && produto.Imagens.Count > 0)
            {
                foreach (var img in produto.Imagens)
                {
                    // Se a imagem já existe no banco (tem Id), podemos atualizar a URL ou ordem
                    if (img.ImagemId > 0)
                    {
                        var sqlImgUpdate = @"
                    UPDATE ProdutoImagens SET Url = @Url WHERE Id = @ImagemId";
                        await connection.ExecuteAsync(sqlImgUpdate, img);
                    }
                    else
                    {
                        // Nova imagem
                        var sqlImgInsert = @"
                    INSERT INTO ProdutoImagens (ProdutoId, Url, OrdemImagem)
                    VALUES (@ProdutoId, @Url, @OrdemImagem)";
                        img.ProdutoId = produto.Id;
                        await connection.ExecuteAsync(sqlImgInsert, img);
                    }
                }
            }
        }


        public async Task DeletarProduto(int id)
        {
            using var connection = new MySqlConnection(_connectionString);

            var sql = "DELETE FROM Produtos WHERE Id = @Id;";

            await connection.ExecuteAsync(sql, new { Id = id });
        }

        public async Task<Dictionary<string, List<string>>> ObterCategoriasTipos()
        {
            const string sql = @"
                SELECT Categoria, Tipo
                FROM produtos
                WHERE Categoria IS NOT NULL
                  AND Tipo IS NOT NULL;
            ";

            await using var connection = new MySqlConnection(_connectionString);
            // QueryAsync sem tipagem -> retorna IEnumerable<dynamic>
            var rows = await connection.QueryAsync(sql);

            // Transformar explicitamente em strings e agrupar
            var mapped = rows
                .Select(r => new
                {
                    Categoria = (r.Categoria == null) ? string.Empty : (string)r.Categoria,
                    Tipo = (r.Tipo == null) ? string.Empty : (string)r.Tipo
                })
                .Where(x => !string.IsNullOrWhiteSpace(x.Categoria) && !string.IsNullOrWhiteSpace(x.Tipo));

            var resultado = mapped
                .GroupBy(x => x.Categoria)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.Tipo).Distinct().OrderBy(t => t).ToList()
                );

            return resultado;
        }

        public async Task<ProdutoImagem?> ImagemPorId(int id)
        {
            using var connection = new MySqlConnection(_connectionString);
            var sql = "SELECT * FROM produtoImagens WHERE Id = @Id";
            return await connection.QueryFirstOrDefaultAsync<ProdutoImagem>(sql, new { Id = id });
        }

        public async Task DeletarImagem(int imagemId)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                string sql = "DELETE FROM produtoImagens WHERE Id = @Id";
                await connection.ExecuteAsync(sql, new { Id = imagemId });
            }
        }


    }
}
