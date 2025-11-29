using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

            const string sqlInsert = @"
INSERT INTO pedidos 
(UsuarioId, EnderecoId, MetodoPagamentoId, CartaoId, TaxaEntrega, StatusPagamento, StatusPedido, DataPedido, ValorTotal)
VALUES 
(@UsuarioId, @EnderecoId, @MetodoPagamentoId, @CartaoId, @TaxaEntrega, @StatusPagamento, @StatusPedido, @DataPedido, @ValorTotal);
";
            await connection.ExecuteAsync(sqlInsert, pedido);

            var pedidoId = await connection.ExecuteScalarAsync<int>("SELECT LAST_INSERT_ID();");
            pedido.Id = pedidoId;

            const string sqlItem = @"
INSERT INTO itensPedido (PedidoId, ProdutoId, Quantidade, PrecoUnitario)
VALUES (@PedidoId, @ProdutoId, @Quantidade, @PrecoUnitario);
";
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

            var pedido = await connection.QueryFirstOrDefaultAsync<Pedido>(
                "SELECT * FROM pedidos WHERE Id = @Id",
                new { Id = pedidoId }
            );
            if (pedido == null) return null;

            const string sqlItens = @"
SELECT 
    i.Id, i.PedidoId, i.ProdutoId, i.Quantidade, i.PrecoUnitario,
    p.Id AS ProdId, p.Nome, p.Categoria, p.Preco
FROM itensPedido i
INNER JOIN produtos p ON p.Id = i.ProdutoId
WHERE i.PedidoId = @PedidoId
ORDER BY i.Id;";
            var itens = await connection.QueryAsync<ItemPedido, Produto, ItemPedido>(
                sqlItens,
                (i, p) => { i.Produto = p; return i; },
                new { PedidoId = pedidoId },
                splitOn: "ProdId"
            );

            pedido.Itens = itens.ToList();
            return pedido;
        }
        public async Task<List<Pedido>> ObterPedidosDoUsuario(int usuarioId)
        {
            using var connection = new MySqlConnection(_connectionString);

            const string sqlPedidos = @"
SELECT 
    p.Id, p.UsuarioId, p.DataPedido, p.ValorTotal, p.StatusPedido, p.DataPagamento,
    p.MetodoPagamentoId,
    m.Id AS MpId, m.Descricao
FROM pedidos p
LEFT JOIN metodosPagamento m ON p.MetodoPagamentoId = m.Id
WHERE p.UsuarioId = @UsuarioId
ORDER BY p.DataPedido DESC;";
            var pedidos = await connection.QueryAsync<Pedido, MetodoPagamento, Pedido>(
                sqlPedidos,
                (p, m) => { p.MetodoPagamento = m ?? new MetodoPagamento { Descricao = "Desconhecido" }; return p; },
                new { UsuarioId = usuarioId },
                splitOn: "MpId"
            );

            var lista = pedidos.ToList();
            if (lista.Count == 0) return lista;

            var pedidoIds = lista.Select(p => p.Id).ToArray();

            const string sqlItens = @"
SELECT 
    ip.Id, ip.PedidoId, ip.ProdutoId, ip.Quantidade, ip.PrecoUnitario,
    pr.Id AS ProdId, pr.Nome, pr.Categoria, pr.Preco
FROM itensPedido ip
INNER JOIN produtos pr ON pr.Id = ip.ProdutoId
WHERE ip.PedidoId IN @PedidoIds
ORDER BY ip.PedidoId, ip.Id;";
            var itens = await connection.QueryAsync<ItemPedido, Produto, ItemPedido>(
                sqlItens,
                (item, prod) => { item.Produto = prod; return item; },
                new { PedidoIds = pedidoIds },
                splitOn: "ProdId"
            );

            var porPedido = itens.GroupBy(i => i.PedidoId)
                                 .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var p in lista)
                p.Itens = porPedido.TryGetValue(p.Id, out var lst) ? lst : new List<ItemPedido>();

            return lista;
        }

        public async Task<List<Pedido>> ObterPedidosPorUsuario(int usuarioId)
            => await ObterPedidosDoUsuario(usuarioId);

        public async Task<List<Pedido>> ObterTodosPedidos()
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            const string sqlPedidos = @"
SELECT 
    p.Id, p.UsuarioId, p.EnderecoId, p.MetodoPagamentoId, p.CartaoId, 
    p.TaxaEntrega, p.StatusPagamento, p.StatusPedido, p.DataPedido, p.DataPagamento, p.ValorTotal,

    u.Id AS UsuarioId2, u.Nome, u.DataNasc, u.Email, u.Telefone, u.CPF, u.Tipo,

    e.Id AS EnderecoId2, e.NomeCompleto, e.Logradouro, e.Numero, e.Bairro, e.Cidade, e.Estado, e.CEP, e.Complemento,

    m.Id AS MetodoId2, m.Descricao,

    c.Id AS CartaoId2, c.NomeTitular, c.Numero, c.Bandeira, c.Validade, c.Tipo
FROM pedidos p
LEFT JOIN usuarios         u ON u.Id = p.UsuarioId
LEFT JOIN enderecos        e ON e.Id = p.EnderecoId
LEFT JOIN metodosPagamento m ON m.Id = p.MetodoPagamentoId
LEFT JOIN cartoes          c ON c.Id = p.CartaoId
ORDER BY p.DataPedido DESC;";

            var pedidos = (await connection.QueryAsync<
                Pedido, Usuario, Endereco, MetodoPagamento, Cartao, Pedido>(
                sqlPedidos,
                (p, u, e, m, c) =>
                {
                    p.Usuario = u;
                    p.Endereco = e;
                    p.MetodoPagamento = m;
                    p.Cartao = c;
                    return p;
                },
                splitOn: "UsuarioId2,EnderecoId2,MetodoId2,CartaoId2"
            )).ToList();

            if (pedidos.Count == 0) return pedidos;

            var pedidoIds = pedidos.Select(p => p.Id).ToArray();

            const string sqlItens = @"
SELECT 
    ip.Id, ip.PedidoId, ip.ProdutoId, ip.Quantidade, ip.PrecoUnitario,
    pr.Id AS ProdId, pr.Nome, pr.Categoria, pr.Preco
FROM itensPedido ip
INNER JOIN produtos pr ON pr.Id = ip.ProdutoId
WHERE ip.PedidoId IN @PedidoIds
ORDER BY ip.PedidoId, ip.Id;";

            var itens = await connection.QueryAsync<ItemPedido, Produto, ItemPedido>(
                sqlItens,
                (item, prod) => { item.Produto = prod; return item; },
                new { PedidoIds = pedidoIds },
                splitOn: "ProdId"
            );

            var porPedido = itens.GroupBy(i => i.PedidoId)
                                 .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var p in pedidos)
                p.Itens = porPedido.TryGetValue(p.Id, out var lst) ? lst : new List<ItemPedido>();

            return pedidos;
        }

        public async Task<Pedido?> ObterPedidoPorIdAdm(int pedidoId)
        {
            using var connection = new MySqlConnection(_connectionString);

            const string sqlCab = @"
SELECT 
    p.Id, p.UsuarioId, p.EnderecoId, p.MetodoPagamentoId, p.CartaoId,
    p.ValorTotal, p.TaxaEntrega, p.StatusPagamento, p.StatusPedido, p.DataPedido, p.DataPagamento,

    u.Id AS UsuarioId2, u.Nome, u.DataNasc, u.Email, u.Telefone,

    e.Id AS EnderecoId2, e.NomeCompleto, e.Logradouro, e.Numero, e.Bairro, e.Cidade, e.Estado, e.CEP, e.Complemento,

    m.Id AS MetodoId2, m.Descricao,

    c.Id AS CartaoId2, c.NomeTitular, c.Numero AS NumeroCartao, c.Bandeira, c.Validade, c.Tipo AS TipoCartao
FROM pedidos p
LEFT JOIN usuarios         u ON u.Id = p.UsuarioId
LEFT JOIN enderecos        e ON e.Id = p.EnderecoId
LEFT JOIN metodosPagamento m ON m.Id = p.MetodoPagamentoId
LEFT JOIN cartoes          c ON c.Id = p.CartaoId
WHERE p.Id = @PedidoId;";
            var cab = await connection.QueryAsync<
                Pedido, Usuario, Endereco, MetodoPagamento, Cartao, Pedido>(
                sqlCab,
                (p, u, e, m, c) =>
                {
                    p.Usuario = u ?? new Usuario { Nome = "Desconhecido" };
                    p.Endereco = e ?? new Endereco { NomeCompleto = "Desconhecido" };
                    p.MetodoPagamento = m ?? new MetodoPagamento { Descricao = "Desconhecido" };
                    p.Cartao = c;
                    return p;
                },
                new { PedidoId = pedidoId },
                splitOn: "UsuarioId2,EnderecoId2,MetodoId2,CartaoId2"
            );

            var pedido = cab.FirstOrDefault();
            if (pedido == null) return null;

            const string sqlItens = @"
SELECT 
    i.Id, i.PedidoId, i.ProdutoId, i.Quantidade, i.PrecoUnitario,

    pr.Id AS ProdId, pr.Nome, pr.Categoria, pr.Preco,

    pi.Id AS ImagemId, pi.Url, pi.OrdemImagem
FROM itensPedido i
INNER JOIN produtos pr      ON pr.Id = i.ProdutoId
LEFT  JOIN produtoImagens pi ON pi.ProdutoId = pr.Id
WHERE i.PedidoId = @PedidoId
ORDER BY i.Id, pi.OrdemImagem;";
            var dict = new Dictionary<int, ItemPedido>();

            await connection.QueryAsync<ItemPedido, Produto, ProdutoImagem, ItemPedido>(
                sqlItens,
                (i, pr, img) =>
                {
                    if (!dict.TryGetValue(i.Id, out var item))
                    {
                        item = i;
                        item.Produto = pr;
                        item.Produto.Imagens = new List<ProdutoImagem>();
                        dict.Add(item.Id, item);
                    }
                    if (img != null)
                        item.Produto.Imagens!.Add(img);
                    return item;
                },
                new { PedidoId = pedidoId },
                splitOn: "ProdId,ImagemId"
            );

            pedido.Itens = dict.Values.ToList();

            if (pedido.ValorTotal == 0 && pedido.Itens.Any())
                pedido.ValorTotal = pedido.Itens.Sum(i => i.Quantidade * i.PrecoUnitario) + pedido.TaxaEntrega;

            return pedido;
        }

        public async Task<int> AtualizarStatusPagamento(Pedido pedido)
        {
            using var connection = new MySqlConnection(_connectionString);
            const string sql = @"
UPDATE pedidos 
SET StatusPagamento = @StatusPagamento, DataPagamento = @DataPagamento 
WHERE Id = @Id;";
            return await connection.ExecuteAsync(sql, new { pedido.StatusPagamento, pedido.DataPagamento, pedido.Id });
        }

        public async Task AtualizarStatus(int pedidoId, string statusPedido)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "UPDATE pedidos SET StatusPedido = @StatusPedido WHERE Id = @Id;",
                new { Id = pedidoId, StatusPedido = statusPedido }
            );
        }
    }
}
