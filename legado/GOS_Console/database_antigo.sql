create database if not exists gos character set utf8mb4 collate utf8mb4_unicode_ci;
use gos;

create table usuarios (
  id integer primary key not null auto_increment,
  nome varchar(255) not null,
  email varchar(255) unique not null,
  senha_hash varchar(255) not null,
  cargo enum('admin','padrao') not null default 'padrao'
);

create table clientes (
  id integer primary key not null auto_increment,
  nome varchar(255) not null,
  email varchar(255) unique not null,
  telefone varchar(255) not null
);

create table servicos (
  id integer primary key not null auto_increment,
  tipo_de_servico enum('manutencao','instalacao') not null
);

create table ordem_servicos (
  id integer primary key not null auto_increment,
  descricao varchar(255) not null,
  status enum('aberto','em andamento','concluido') not null,
  data_abertura datetime not null,
  data_fechamento datetime,
  cliente_id integer not null,
  responsavel_id integer not null,
  classe_servico_id integer not null
);

alter table ordem_servicos add constraint ordem_servicos_usuarios foreign key (responsavel_id) references usuarios (id);

alter table ordem_servicos add constraint ordem_servicos_classes foreign key (classe_servico_id) references servicos (id);

alter table ordem_servicos add constraint ordem_servicos_clientes foreign key (cliente_id) references clientes (id);

-- tipos de serviço padrão
insert into servicos (tipo_de_servico) values ('manutencao'), ('instalacao');
