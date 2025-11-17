using Dapper;
using MySql.Data.MySqlClient;
using ecommerce.Models;

namespace ecommerce.Repository
{
    public class PedidoRepository : IPedidoRepository
    {
        private readonly string _connectionString;

        public PedidoRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<int> AdicionarPedido(Pedido pedido)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            // Inserir o pedido
            var sqlInsert = @"
        INSERT INTO pedidos (UsuarioId, EnderecoId, MetodoPagamentoId, CartaoId, TaxaEntrega, StatusPagamento, StatusPedido, DataPedido, ValorTotal)
        VALUES (@UsuarioId, @EnderecoId, @MetodoPagamentoId, @CartaoId, @TaxaEntrega, @StatusPagamento, @StatusPedido, @DataPedido, @ValorTotal);
    ";


            await connection.ExecuteAsync(sqlInsert, pedido);

            // Obter o ID gerado
            int pedidoId = await connection.ExecuteScalarAsync<int>("SELECT LAST_INSERT_ID();");

            pedido.Id = pedidoId;

            // Inserir itens
            const string sqlItem = @"
        INSERT INTO itensPedido (PedidoId, ProdutoId, Quantidade, PrecoUnitario)
        VALUES (@PedidoId, @ProdutoId, @Quantidade, @PrecoUnitario);
    ";
            Console.WriteLine($"ID gerado (pedidoId): {pedidoId}");

            foreach (var item in pedido.Itens)
            {
                await connection.ExecuteAsync(sqlItem, new
                {
                    PedidoId = pedidoId,
                    item.ProdutoId,
                    item.Quantidade,
                    item.PrecoUnitario
                });
            }

            return pedidoId;
        }


        public async Task<Pedido?> ObterPedidoPorId(int pedidoId)
        {
            using var connection = new MySqlConnection(_connectionString);

            // Buscar pedido pelo Id
            var sqlPedido = "SELECT * FROM pedidos WHERE Id = @Id";
            var pedido = await connection.QueryFirstOrDefaultAsync<Pedido>(
                sqlPedido,
                new { Id = pedidoId }  // o nome do parâmetro deve coincidir com o @Id
            );

            if (pedido != null)
            {
                // Buscar itens do pedido junto com os produtos
                var sqlItens = @"
            SELECT i.*, p.*
            FROM itensPedido i
            INNER JOIN produtos p ON i.ProdutoId = p.Id
            WHERE i.PedidoId = @PedidoId
        ";

                var itens = await connection.QueryAsync<ItemPedido, Produto, ItemPedido>(
                    sqlItens,
                    (item, produto) => { item.Produto = produto; return item; },
                    new { PedidoId = pedidoId } 
                );

                pedido.Itens = itens.ToList();
            }

            return pedido;
        }

        public async Task<int> AtualizarStatusPagamento(Pedido pedido)
        {
            using var connection = new MySqlConnection(_connectionString);

            string sql = @"
        UPDATE pedidos 
        SET StatusPagamento = @StatusPagamento, DataPagamento = @DataPagamento 
        WHERE Id = @Id
    ";

            return await connection.ExecuteAsync(sql, new { pedido.StatusPagamento, pedido.DataPagamento, pedido.Id });
        }




        public async Task<List<Pedido>> ObterPedidosPorUsuario(int usuarioId)
        {
            using var connection = new MySqlConnection(_connectionString);

            var sql = "SELECT * FROM pedidos WHERE UsuarioId = @UsuarioId ORDER BY DataPedido DESC";
            var pedidos = await connection.QueryAsync<Pedido>(sql, new { UsuarioId = usuarioId });

            return pedidos.ToList();
        }


        public async Task<List<Pedido>> ObterTodosPedidos()
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            var sqlPedidos = @"
SELECT 
    p.Id, p.UsuarioId, p.EnderecoId, p.MetodoPagamentoId, p.CartaoId, 
    p.TaxaEntrega, p.StatusPagamento, p.StatusPedido, p.DataPedido, p.DataPagamento, p.ValorTotal,
    u.Id, u.Nome, u.DataNasc, u.Email, u.CPF, u.Telefone, u.Tipo,
    e.Id, e.NomeCompleto, e.Logradouro, e.Numero, e.Bairro, e.Cidade, e.Estado, e.CEP, e.Complemento,
    m.Id, m.Descricao,
    c.Id, c.NomeTitular, c.Numero, c.Bandeira, c.Validade, c.Tipo
FROM pedidos p
LEFT JOIN usuarios u ON p.UsuarioId = u.Id
LEFT JOIN enderecos e ON p.EnderecoId = e.Id
LEFT JOIN metodosPagamento m ON p.MetodoPagamentoId = m.Id
LEFT JOIN cartoes c ON p.CartaoId = c.Id
ORDER BY p.DataPedido DESC;";

            var pedidos = (await connection.QueryAsync<Pedido, Usuario, Endereco, MetodoPagamento, Cartao, Pedido>(
                sqlPedidos,
                (pedido, usuario, endereco, metodoPagamento, cartao) =>
                {
                    pedido.Usuario = usuario;
                    pedido.Endereco = endereco;
                    pedido.MetodoPagamento = metodoPagamento;
                    pedido.Cartao = cartao;
                    return pedido;
                },
                splitOn: "Id,Id,Id,Id"
            )).ToList();

            if (!pedidos.Any()) return pedidos;

            // Obter itens e produtos
            var sqlItens = @"
SELECT 
    ip.Id AS ItemPedidoId,
    ip.PedidoId,
    ip.ProdutoId,
    ip.Quantidade,
    ip.PrecoUnitario,
    p.Id AS ProdutoId,
    p.Nome,
    p.Categoria,
    p.Preco,
    (
        SELECT pi.Url
        FROM produtoImagens pi
        WHERE pi.ProdutoId = p.Id
        ORDER BY pi.OrdemImagem ASC
        LIMIT 1
    ) AS ImagemUrl
FROM itensPedido ip
INNER JOIN produtos p ON ip.ProdutoId = p.Id
WHERE ip.PedidoId IN @PedidoIds;";

            var itens = await connection.QueryAsync<ItemPedido, Produto, ItemPedido>(
                sqlItens,
                (item, produto) =>
                {
                    item.Produto = produto;
                    return item;
                },
                new { PedidoIds = pedidos.Select(p => p.Id).ToArray() },
                splitOn: "ProdutoId"
            );

            // 3️⃣ Atribuir itens a cada pedido
            foreach (var pedido in pedidos)
            {
                pedido.Itens = itens.Where(i => i.PedidoId == pedido.Id).ToList();
            }

            return pedidos;
        }




        public async Task<Pedido?> ObterPedidoPorIdAdm(int pedidoId)
        {
            using var connection = new MySqlConnection(_connectionString);

            var sql = @"
    SELECT 
        p.Id, p.UsuarioId, p.EnderecoId, p.MetodoPagamentoId, p.CartaoId,
        p.ValorTotal, p.TaxaEntrega, p.StatusPagamento, p.StatusPedido, p.DataPedido, p.DataPagamento,
        u.Id AS UsuarioId, u.Nome, u.DataNasc, u.Email, u.Telefone,
        e.Id AS EnderecoId, e.NomeCompleto, e.Logradouro, e.Numero, e.Bairro, e.Cidade, e.Estado, e.CEP, e.Complemento,
        m.Id AS MetodoPagamentoId, m.Descricao,
        c.Id AS CartaoId, c.NomeTitular, c.Numero AS NumeroCartao, c.Bandeira, c.Validade, c.Tipo AS TipoCartao
    FROM pedidos p
    LEFT JOIN usuarios u ON p.UsuarioId = u.Id
    LEFT JOIN enderecos e ON p.EnderecoId = e.Id
    LEFT JOIN metodosPagamento m ON p.MetodoPagamentoId = m.Id
    LEFT JOIN cartoes c ON p.CartaoId = c.Id
    WHERE p.Id = @PedidoId;
";

            var pedido = await connection.QueryAsync<Pedido, Usuario, Endereco, MetodoPagamento, Cartao, Pedido>(
                sql,
                (p, u, e, m, c) =>
                {
                    p.Usuario = u ?? new Usuario { Nome = "Desconhecido" };
                    p.Endereco = e ?? new Endereco { NomeCompleto = "Desconhecido" };
                    p.MetodoPagamento = m ?? new MetodoPagamento { Descricao = "Desconhecido" };
                    p.Cartao = c; // pode ser null
                    return p;
                },
                new { PedidoId = pedidoId },
                splitOn: "UsuarioId,EnderecoId,MetodoPagamentoId,CartaoId"
            );

            var pedidoResult = pedido.FirstOrDefault();

            if (pedidoResult != null)
            {
                // Carregar itens e produtos
                var sqlItens = @"
SELECT 
    i.*, 
    pr.*, 
    pi.Id AS ImagemId, pi.Url, pi.OrdemImagem
FROM itensPedido i
INNER JOIN produtos pr ON i.ProdutoId = pr.Id
LEFT JOIN produtoImagens pi ON pr.Id = pi.ProdutoId
WHERE i.PedidoId = @PedidoId
ORDER BY pi.OrdemImagem ASC;
";

                var itemDict = new Dictionary<int, ItemPedido>();

                var itens = await connection.QueryAsync<ItemPedido, Produto, ProdutoImagem, ItemPedido>(
                    sqlItens,
                    (i, pr, pi) =>
                    {
                        if (!itemDict.TryGetValue(i.Id, out var item))
                        {
                            item = i;
                            item.Produto = pr;
                            item.Produto.Imagens = new List<ProdutoImagem>();
                            itemDict.Add(item.Id, item);
                        }

                        if (pi != null)
                            item.Produto.Imagens.Add(pi);

                        return item;
                    },
                    new { PedidoId = pedidoId },
                    splitOn: "Id,ImagemId"
                );

                pedidoResult.Itens = itemDict.Values.ToList();

                pedidoResult.Itens = itens.ToList();

                // Garante valor total consistente
                if (pedidoResult.ValorTotal == 0 && pedidoResult.Itens.Any())
                {
                    pedidoResult.ValorTotal = pedidoResult.Itens.Sum(x => x.Quantidade * x.PrecoUnitario) + pedidoResult.TaxaEntrega;
                }
            }

            return pedidoResult;
        }

        public async Task AtualizarStatus(int pedidoId, string statusPedido)
        {
            using var connection = new MySqlConnection(_connectionString);

            var sql = "UPDATE pedidos SET StatusPedido = @StatusPedido WHERE Id = @Id";
            var parameters = new { Id = pedidoId, StatusPedido = statusPedido };

            await connection.ExecuteAsync(sql, parameters);
        }

        public async Task<List<Pedido>> ObterPedidosDoUsuario(int usuarioId)
        {
            using var connection = new MySqlConnection(_connectionString);

            var sqlPedidos = @"
        SELECT 
            p.Id, p.UsuarioId, p.DataPedido, p.ValorTotal, p.StatusPedido, p.DataPagamento,
            p.MetodoPagamentoId,
            m.Id AS MpId, m.Descricao
        FROM pedidos p
        LEFT JOIN metodosPagamento m ON p.MetodoPagamentoId = m.Id
        WHERE p.UsuarioId = @UsuarioId
        ORDER BY p.DataPedido DESC";

            var pedidos = await connection.QueryAsync<Pedido, MetodoPagamento, Pedido>(
                sqlPedidos,
                (pedido, metodoPagamento) =>
                {
                    pedido.MetodoPagamento = metodoPagamento ?? new MetodoPagamento { Descricao = "Desconhecido" };
                    return pedido;
                },
                new { UsuarioId = usuarioId },
                splitOn: "MpId"
            );

            var listaPedidos = pedidos.ToList();

            if (listaPedidos.Count > 0)
            {
                var pedidoIds = listaPedidos.Select(p => p.Id).ToArray(); // <--- usa Id
                var sqlItens = @"
            SELECT PedidoId, Quantidade
            FROM itensPedido
            WHERE PedidoId IN @PedidoIds";

                var itens = await connection.QueryAsync<ItemPedido>(sqlItens, new { PedidoIds = pedidoIds });

                foreach (var pedido in listaPedidos)
                {
                    pedido.Itens = itens.Where(i => i.PedidoId == pedido.Id).ToList();
                }
            }

            return listaPedidos;
        }


    }
}
