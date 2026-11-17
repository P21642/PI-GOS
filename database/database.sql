-- =====================================================================
--  g.o.s. - esquema do banco de dados (mysql 8.0.16+)
--  parte 1: tabelas, chaves, restrições e dados iniciais.
--  os triggers (auditoria, integridade, segurança) vêm na parte 2.
--
--  tabelas (5): usuarios, clientes, servicos, ordem_servicos, log_auditoria
-- =====================================================================

create database if not exists gos character set utf8mb4 collate utf8mb4_unicode_ci;
use gos;

-- ---------------------------------------------------------------------
--  usuarios
--  senha_hash: hash bcrypt (nunca a senha). gerado e verificado no c#.
--  tentativas_falhas / bloqueado_ate: bloqueio temporário contra
--  força bruta no login (controlado pela aplicação).
-- ---------------------------------------------------------------------
create table usuarios (
  id int not null auto_increment primary key,
  nome varchar(255) not null,
  email varchar(255) not null unique,
  senha_hash varchar(255) not null,
  cargo enum('admin','padrao') not null default 'padrao',
  tentativas_falhas int not null default 0,
  bloqueado_ate datetime,
  criado_em datetime not null default current_timestamp,
);

-- ---------------------------------------------------------------------
--  clientes
--  email e telefone são dados pessoais: guardados cifrados (aes-256-gcm,
--  feito no c#). formato gravado: nonce(12) || texto cifrado || tag(16).
--  email_hash = hmac-sha256(chave_de_indice, email normalizado) em hex.
--  serve para garantir e-mail único e buscar por e-mail sem decifrar,
--  já que o texto cifrado muda a cada gravação (nonce aleatório).
-- ---------------------------------------------------------------------
create table clientes (
  id int not null auto_increment primary key,
  nome varchar(255) not null,
  email_cifrado varbinary(512) not null,
  email_hash char(64) not null unique,
  telefone_cifrado varbinary(128) not null,
);

-- ---------------------------------------------------------------------
--  servicos
-- ---------------------------------------------------------------------
create table servicos (
  id int not null auto_increment primary key,
  tipo_de_servico enum('manutencao','instalacao') not null unique,
);

-- ---------------------------------------------------------------------
--  ordem_servicos
--  os nomes das colunas são os mesmos de antes, para não quebrar os
--  repositórios. mudanças: status ganhou 'cancelado' e há restrições
--  de integridade.
--  regras que dependem do status (data_fechamento preenchida ao
--  concluir/cancelar, transições válidas) ficam nos triggers (parte 2).
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
  constraint fk_os_cliente foreign key (cliente_id) references clientes (id),
  constraint fk_os_responsavel foreign key (responsavel_id) references usuarios (id),
  constraint fk_os_servico foreign key (classe_servico_id) references servicos (id),
  constraint ck_os_datas check (data_fechamento is null or data_fechamento >= data_abertura)
);

-- ---------------------------------------------------------------------
--  log_auditoria
--  preenchida somente por triggers (parte 2). sem chaves estrangeiras
--  de propósito: o histórico precisa sobreviver à exclusão dos registros.
--  usuario_app_id: quem fez a ação no sistema. a aplicação informa isso
--  ao abrir a conexão (variável de sessão @gos_usuario_id); sem isso
--  só se conhece o usuário do banco (usuario_db).
-- ---------------------------------------------------------------------
create table log_auditoria (
  id bigint not null auto_increment primary key,
  data_hora datetime not null default current_timestamp,
  tabela varchar(64) not null,
  registro_id int not null,
  acao enum('insert','update','delete') not null,
  usuario_app_id int,
  usuario_db varchar(128) not null,
  dados_antes json,
  dados_depois json,
);

-- ---------------------------------------------------------------------
--  dados iniciais
-- ---------------------------------------------------------------------
insert into servicos (tipo_de_servico) values ('manutencao'), ('instalacao');

-- ---------------------------------------------------------------------
--  usuário do banco para a aplicação (menor privilégio).
--  não rodar com senha no repositório: execute à parte, com a senha
--  definida fora do código, e use este usuário no lugar do root.
--  a aplicação só lê o log; quem grava nele são os triggers.
-- ---------------------------------------------------------------------
-- create user 'gos_app'@'localhost' identified by '<definir_fora_do_repositorio>';
-- grant select, insert, update, delete on gos.usuarios       to 'gos_app'@'localhost';
-- grant select, insert, update, delete on gos.clientes       to 'gos_app'@'localhost';
-- grant select                         on gos.servicos       to 'gos_app'@'localhost';
-- grant select, insert, update, delete on gos.ordem_servicos to 'gos_app'@'localhost';
-- grant select                         on gos.log_auditoria  to 'gos_app'@'localhost';