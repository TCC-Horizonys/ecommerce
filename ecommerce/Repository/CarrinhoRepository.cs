using Dapper;
using MySql.Data.MySqlClient;
using ecommerce.Models;

namespace ecommerce.Repository
{
    public class CarrinhoRepository
    {
        private readonly string _connectionString;


        public CarrinhoRepository(string connectionString)
        {
            _connectionString = connectionString;
        }



        public List<Endereco> ObterEnderecosPorUsuario(int usuarioId)
        {
            using var connection = new MySqlConnection(_connectionString);
            var sql = "SELECT * FROM enderecos WHERE UsuarioId = @UsuarioId";
            return connection.Query<Endereco>(sql, new { UsuarioId = usuarioId }).ToList();
        }

        public void AdicionarEndereco(Endereco endereco)
        {
            using var connection = new MySqlConnection(_connectionString);
            var sql = @"INSERT INTO enderecos 
                (UsuarioId, NomeCompleto, Logradouro, Numero, Bairro, Cidade, Estado, CEP, Complemento)
                VALUES (@UsuarioId, @NomeCompleto, @Logradouro, @Numero, @Bairro, @Cidade, @Estado, @CEP, @Complemento)";
            connection.Execute(sql, endereco);
        }
        public IEnumerable<Cartao> ObterCartoesPorUsuario(int usuarioId)
        {
            using var connection = new MySqlConnection(_connectionString);
            string sql = "SELECT * FROM cartoes WHERE UsuarioId = @UsuarioId";
            return connection.Query<Cartao>(sql, new { UsuarioId = usuarioId });
        }

        public async Task<Cartao> ObterCartaoPorId(int cartaoId)
        {
            using var connection = new MySqlConnection(_connectionString);
            string sql = "SELECT * FROM cartoes WHERE Id = @Id";
            return await connection.QueryFirstOrDefaultAsync<Cartao>(sql, new { Id = cartaoId });
        }


        public void AdicionarCartao(Cartao cartao)
        {
            using var connection = new MySqlConnection(_connectionString);
            var sql = @"INSERT INTO cartoes 
                (UsuarioId, Tipo, NomeTitular, Numero, Validade, CVV, Bandeira)
                VALUES (@UsuarioId, @Tipo, @NomeTitular, @Numero, @Validade, @CVV, @Bandeira)";
            connection.Execute(sql, cartao);
        }

    }
}
