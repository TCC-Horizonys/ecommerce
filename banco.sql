drop database dbEcommerce;
create database dbEcommerce;
use dbEcommerce;

CREATE TABLE produtos(
Id INT PRIMARY KEY AUTO_INCREMENT,
Nome VARCHAR(100),
Categoria VARCHAR(70),
Descricao varchar(300),
EstadoConservacao VARCHAR(500),
Preco DECIMAL(10,2),
DataCriada DATE DEFAULT (CURRENT_DATE)
);

CREATE TABLE produtoImagens (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    ProdutoId INT NOT NULL,
    Url VARCHAR(255) NOT NULL,
    OrdemImagem INT DEFAULT 1,
    FOREIGN KEY (ProdutoId) REFERENCES produtos(Id)
        ON DELETE CASCADE
        ON UPDATE CASCADE
);


CREATE TABLE usuarios(
Id INT PRIMARY KEY AUTO_INCREMENT,
Nome VARCHAR(100),
DataNasc varchar(10) not null,
Email VARCHAR(150) UNIQUE,
Senha VARCHAR(100),
CPF VARCHAR(20) UNIQUE,
Telefone VARCHAR(20),
Tipo ENUM  ('admin', 'cliente') NOT NULL DEFAULT 'cliente'
);

CREATE TABLE enderecos(
    Id INT AUTO_INCREMENT PRIMARY KEY,
    UsuarioId INT NOT NULL,
    NomeCompleto varchar(200),
    Logradouro VARCHAR(150) NOT NULL,
    Numero VARCHAR(10),
    Cidade VARCHAR(100),
    Estado VARCHAR(50),
    Bairro varchar(50),
    CEP VARCHAR(15),
    Complemento VARCHAR(100),
    FOREIGN KEY (UsuarioId) REFERENCES usuarios(Id)
        ON DELETE CASCADE ON UPDATE CASCADE
);

CREATE TABLE metodosPagamento (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Descricao VARCHAR(50) NOT NULL
);


CREATE TABLE cartoes (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    UsuarioId INT NOT NULL,
    Tipo varchar(20) NOT NULL,
    NomeTitular VARCHAR(100) NOT NULL,
    Numero VARCHAR(25) NOT NULL, 
    Bandeira VARCHAR(30) NOT NULL,
    Validade VARCHAR(10) NOT NULL,
    CVV VARCHAR(3) NOT NULL,
    FOREIGN KEY (UsuarioId) REFERENCES usuarios(Id)
        ON DELETE CASCADE ON UPDATE CASCADE
);


CREATE TABLE pedidos (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    UsuarioId INT NOT NULL,
    EnderecoId INT NOT NULL,
    MetodoPagamentoId INT NULL,
    CartaoId INT,
    DataPagamento datetime,
    TaxaEntrega DECIMAL(10,2) DEFAULT 0.00,
	StatusPagamento VARCHAR(50) DEFAULT 'Aguardando Pagamento',
	StatusPedido VARCHAR(50) DEFAULT 'Criado',
    DataPedido DATETIME DEFAULT CURRENT_TIMESTAMP,
    ValorTotal DECIMAL(10,2) NOT NULL,
    FOREIGN KEY (UsuarioId) REFERENCES usuarios(Id)
        ON DELETE CASCADE ON UPDATE CASCADE,
    FOREIGN KEY (EnderecoId) REFERENCES enderecos(Id)
        ON DELETE RESTRICT ON UPDATE CASCADE,
    FOREIGN KEY (MetodoPagamentoId) REFERENCES metodosPagamento(Id)
        ON DELETE SET NULL ON UPDATE CASCADE,
    FOREIGN KEY (CartaoId) REFERENCES cartoes(Id)
        ON DELETE SET NULL ON UPDATE CASCADE
);


CREATE TABLE itensPedido (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    PedidoId INT NOT NULL,
    ProdutoId INT NOT NULL,
    Quantidade INT NOT NULL,
    PrecoUnitario DECIMAL(10,2) NOT NULL,
    FOREIGN KEY (PedidoId) REFERENCES pedidos(Id)
        ON DELETE CASCADE ON UPDATE CASCADE,
    FOREIGN KEY (ProdutoId) REFERENCES produtos(Id)
        ON DELETE RESTRICT ON UPDATE CASCADE
);

INSERT INTO metodosPagamento (Descricao) 
VALUES ('Cartão'),
('Pix');

SELECT * FROM usuarios;

INSERT INTO produtos (Nome, Categoria, Descricao, EstadoConservacao, Preco)
VALUES ('Vestido Rococco', 'Roupa', 'Vestido da Princesa Isabel', 'Sem nenhum rasgo', 3000.00);

INSERT INTO produtoImagens (ProdutoId, Url) VALUES
(1, 'rococco.png');

select * from produtos;

-- INSERT INTO produtoImagens (ProdutoId, Url) VALUES
-- (1, '01.jpg'),(2, '02.jpg'),(3, '03.jpg'),(4, '04.jpg'),(5, '05.jpg'),(6, '06.jpg');

INSERT INTO usuarios(Nome, DataNasc, Email, Senha, CPF, Telefone, Tipo)
VALUES 
('Admin', '12121200','admin@gmail.com', 'admin@', '11111111111', '11999999999', 'admin'),
('Malu', '27072007', 'malu@gmail.com', '123', '11111111115', '11999999995', 'cliente');

INSERT INTO enderecos (
    UsuarioId,
    NomeCompleto,
    Rua,
    Numero,
    Bairro,
    Cidade,
    Estado,
    CEP,
    Complemento
) VALUES (
    1,
    'Pedro Henrique',
    'Rua das Flores',
    '123',
    'Jardim Primavera',
    'São Paulo',
    'SP',
    '01234-567',
    'Apartamento 45'
);
