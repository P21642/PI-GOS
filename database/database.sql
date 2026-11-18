-- =====================================================================
--  g.o.s. - esquema do banco de dados (mysql 8.0.16+)
--  parte 1: tabelas, chaves, restrições e dados iniciais.

--  tabelas (6): pessoas, usuarios, clientes, servicos, ordem_servicos,
--  log_auditoria
-- =====================================================================

create database if not exists gos character set utf8mb4 collate utf8mb4_unicode_ci;
use gos;

-- ---------------------------------------------------------------------
--  pessoas  (superclasse de usuarios e clientes)
--  guarda o que é comum aos dois: nome e e-mail.
--  email_cifrado: e-mail cifrado com aes-256-gcm (feito no c#).
--                 formato gravado: nonce(12) || texto cifrado || tag(16).
--                 é um dado em bytes, por isso varbinary.
--  email_hash:    hmac-sha256 do e-mail normalizado (minúsculas, sem
--                 espaços), em hexadecimal.
-- ---------------------------------------------------------------------
create table pessoas (
  id int not null auto_increment primary key,
  nome varchar(255) not null,
  email_cifrado varbinary(512) not null,
  email_hash char(64) not null unique
);

-- ---------------------------------------------------------------------
--  usuarios  (subclasse de pessoas)
--  a chave primária é também chave estrangeira: cada usuário é uma
--  linha de pessoas (mesmo id).
--  senha_hash: hash bcrypt (nunca a senha). gerado e verificado no c#.
--  tentativas_falhas / bloqueado_ate: bloqueio temporário contra
--  força bruta no login (controlado pela aplicação).
-- ---------------------------------------------------------------------
create table usuarios (
  pessoa_id int not null primary key,
  senha_hash varchar(255) not null,
  cargo enum('admin','padrao') not null default 'padrao',
  tentativas_falhas int not null default 0,
  bloqueado_ate datetime,
  criado_em datetime not null default current_timestamp,
  constraint fk_usuarios_pessoa foreign key (pessoa_id) references pessoas (id)
);

-- ---------------------------------------------------------------------
--  clientes  (subclasse de pessoas)
--  telefone_cifrado: aes-256-gcm, mesmo formato do e-mail.
-- ---------------------------------------------------------------------
create table clientes (
  pessoa_id int not null primary key,
  telefone_cifrado varbinary(128) not null,
  constraint fk_clientes_pessoa foreign key (pessoa_id) references pessoas (id)
);

-- ---------------------------------------------------------------------
--  servicos (podem ser atribuídos mais tipos de serviço ao longo do projeto)
-- ---------------------------------------------------------------------
create table servicos (
  id int not null auto_increment primary key,
  tipo_de_servico enum('manutencao','instalacao') not null unique
);

-- ---------------------------------------------------------------------
--  ordem_servicos
--  cliente_id e responsavel_id guardam o pessoa_id do cliente e do
--  usuário responsável
--  as regras que dependem do status (data_fechamento, transições)
--  ficam em triggers, no database_triggers.sql.
-- ---------------------------------------------------------------------
create table ordem_servicos (
  id int not null auto_increment primary key,
  descricao varchar(255) not null,
  status enum('aberto','em andamento','concluido','cancelado') not null default 'aberto',
  data_abertura datetime not null default current_timestamp,
  data_fechamento datetime,
  cliente_id int not null,
  responsavel_id int not null,
  classe_servico_id int not null,
  constraint fk_os_cliente foreign key (cliente_id) references clientes (pessoa_id),
  constraint fk_os_responsavel foreign key (responsavel_id) references usuarios (pessoa_id),
  constraint fk_os_servico foreign key (classe_servico_id) references servicos (id),
  constraint ck_os_datas check (data_fechamento is null or data_fechamento >= data_abertura)
);

-- ---------------------------------------------------------------------
--  log_auditoria
--  preenchida somente por triggers. sem chaves estrangeiras
--  de propósito: o histórico precisa sobreviver à exclusão dos registros.
--  dados_antes / dados_depois: texto no formato "campo=valor; campo=valor".
--  usuario_app_id: quem fez a ação no sistema. a aplicação informa isso
--  ao abrir a conexão (set @gos_usuario_id = ...); sem isso só se
--  conhece o usuário do banco (usuario_db).
-- ---------------------------------------------------------------------
create table log_auditoria (
  id bigint not null auto_increment primary key,
  data_hora datetime not null default current_timestamp,
  tabela varchar(64)  not null,
  registro_id int not null,
  acao enum('insert','update','delete') not null,
  usuario_app_id int,
  usuario_db varchar(128) not null,
  dados_antes text,
  dados_depois text
);

-- ---------------------------------------------------------------------
--  dados iniciais
--  o primeiro administrador não é criado aqui: o e-mail precisa ser
--  cifrado pelo c#. o sistema cria o primeiro admin no primeiro acesso.
-- ---------------------------------------------------------------------
insert into servicos (tipo_de_servico) values ('manutencao'), ('instalacao');
