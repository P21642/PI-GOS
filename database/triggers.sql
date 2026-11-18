-- =====================================================================
--  g.o.s. - parte 2: view, procedures, triggers e permissões
--
--  convenções: vw_ = view, sp_ = procedure,
--  p_ = parâmetro, trg_ = trigger.
--
-- =====================================================================

use gos;

-- =====================================================================
--  view
-- =====================================================================
drop view if exists vw_ordens_detalhadas;

-- ordens com nome do cliente, do responsável e tipo de serviço.
create view vw_ordens_detalhadas as
select os.id,
       os.descricao,
       os.status,
       os.data_abertura,
       os.data_fechamento,
       os.cliente_id,
       pc.nome as cliente_nome,
       os.responsavel_id,
       pu.nome as responsavel_nome,
       os.classe_servico_id,
       s.tipo_de_servico
from ordem_servicos os
join pessoas  pc on pc.id = os.cliente_id
join pessoas  pu on pu.id = os.responsavel_id
join servicos s  on s.id  = os.classe_servico_id;

-- =====================================================================
--  procedures
--  cadastrar/excluir usuário ou cliente mexe em duas tabelas.
--  por isso usam transação: ou grava tudo, ou não grava nada.
--  o exit handler desfaz (rollback) e o resignal repassa o
--  erro original para a aplicação.
-- =====================================================================
drop procedure if exists sp_cadastrar_usuario;
drop procedure if exists sp_cadastrar_cliente;
drop procedure if exists sp_excluir_usuario;
drop procedure if exists sp_excluir_cliente;

delimiter //

create procedure sp_cadastrar_usuario (
    in p_nome          varchar(255),
    in p_email_cifrado varbinary(512),
    in p_email_hash    char(64),
    in p_senha_hash    varchar(255),
    in p_cargo         varchar(10)
)
begin
    declare v_id int;
    declare exit handler for sqlexception
    begin
        rollback;
        resignal;
    end;

    start transaction;

    insert into pessoas (nome, email_cifrado, email_hash)
    values (p_nome, p_email_cifrado, p_email_hash);

    set v_id = last_insert_id();

    insert into usuarios (pessoa_id, senha_hash, cargo)
    values (v_id, p_senha_hash, p_cargo);

    commit;

    select v_id as id;
end //

create procedure sp_cadastrar_cliente (
    in p_nome             varchar(255),
    in p_email_cifrado    varbinary(512),
    in p_email_hash       char(64),
    in p_telefone_cifrado varbinary(128)
)
begin
    declare v_id int;
    declare exit handler for sqlexception
    begin
        rollback;
        resignal;
    end;

    start transaction;

    insert into pessoas (nome, email_cifrado, email_hash)
    values (p_nome, p_email_cifrado, p_email_hash);

    set v_id = last_insert_id();

    insert into clientes (pessoa_id, telefone_cifrado)
    values (v_id, p_telefone_cifrado);

    commit;

    select v_id as id;
end //

-- exclui o usuário e, se a pessoa não for também cliente, a linha de pessoas.
create procedure sp_excluir_usuario (
    in p_pessoa_id int
)
begin
    declare exit handler for sqlexception
    begin
        rollback;
        resignal;
    end;

    start transaction;

    delete from usuarios where pessoa_id = p_pessoa_id;

    delete from pessoas
    where id = p_pessoa_id
      and not exists (select 1 from clientes where pessoa_id = p_pessoa_id);

    commit;
end //

-- exclui o cliente e, se a pessoa não for também usuário, a linha de pessoas.
create procedure sp_excluir_cliente (
    in p_pessoa_id int
)
begin
    declare exit handler for sqlexception
    begin
        rollback;
        resignal;
    end;

    start transaction;

    delete from clientes where pessoa_id = p_pessoa_id;

    delete from pessoas
    where id = p_pessoa_id
      and not exists (select 1 from usuarios where pessoa_id = p_pessoa_id);

    commit;
end //

delimiter ;

-- =====================================================================
--  triggers de integridade - ordem_servicos
--  garantem as regras do ciclo de vida da ordem, mesmo que alguém
--  altere o banco sem passar pela aplicação.
-- =====================================================================
drop trigger if exists trg_os_before_insert;
drop trigger if exists trg_os_before_update;

delimiter //

-- regra: toda ordem nasce "aberto" e sem data de fechamento.
create trigger trg_os_before_insert
before insert on ordem_servicos
for each row
begin
    if new.status <> 'aberto' then
        signal sqlstate '45000'
            set message_text = 'toda ordem de serviço deve ser criada com status aberto';
    end if;
    set new.data_fechamento = null;
end //

-- regras ao alterar uma ordem:
--  1) concluída ou cancelada não pode mais ser alterada;
--  2) a data de abertura não muda;
--  3) a data de fechamento é preenchida ao concluir/cancelar
--  e fica vazia nos outros status.
create trigger trg_os_before_update
before update on ordem_servicos
for each row
begin
    if old.status in ('concluido', 'cancelado') then
        signal sqlstate '45000'
            set message_text = 'ordem concluída ou cancelada não pode ser alterada';
    end if;

    if new.data_abertura <> old.data_abertura then
        signal sqlstate '45000'
            set message_text = 'a data de abertura não pode ser alterada';
    end if;

    if new.status in ('concluido', 'cancelado') then
        set new.data_fechamento = now();
    else
        set new.data_fechamento = null;
    end if;
end //

delimiter ;

-- =====================================================================
--  triggers de segurança - usuarios
--  nunca deixar o sistema sem nenhum administrador.
-- =====================================================================
drop trigger if exists trg_usuarios_before_delete;
drop trigger if exists trg_usuarios_before_update;

delimiter //

create trigger trg_usuarios_before_delete
before delete on usuarios
for each row
begin
    if old.cargo = 'admin'
       and (select count(*) from usuarios where cargo = 'admin') <= 1 then
        signal sqlstate '45000'
            set message_text = 'não é possível excluir o último administrador';
    end if;
end //

create trigger trg_usuarios_before_update
before update on usuarios
for each row
begin
    if old.cargo = 'admin' and new.cargo <> 'admin'
       and (select count(*) from usuarios where cargo = 'admin') <= 1 then
        signal sqlstate '45000'
            set message_text = 'não é possível rebaixar o último administrador';
    end if;
end //

delimiter ;

-- =====================================================================
--  triggers de auditoria - ordem_servicos
--  cada insert/update/delete gera uma linha em log_auditoria com o
--  "antes" e o "depois". @gos_usuario_id é uma variável de sessão que
--  a aplicação preenche com o id de quem está logado (se não preencher,
--  fica null e só o usuario_db é registrado).
--  data_fechamento pode ser null; por isso o ifnull: sem ele o concat
--  devolveria null para o texto inteiro.
-- =====================================================================
drop trigger if exists trg_os_after_insert;
drop trigger if exists trg_os_after_update;
drop trigger if exists trg_os_after_delete;

delimiter //

create trigger trg_os_after_insert
after insert on ordem_servicos
for each row
begin
    insert into log_auditoria
        (tabela, registro_id, acao, usuario_app_id, usuario_db, dados_antes, dados_depois)
    values
        ('ordem_servicos', new.id, 'insert', @gos_usuario_id, current_user(), null,
         concat('status=', new.status,
                '; descricao=', new.descricao,
                '; cliente_id=', new.cliente_id,
                '; responsavel_id=', new.responsavel_id,
                '; servico_id=', new.classe_servico_id));
end //

create trigger trg_os_after_update
after update on ordem_servicos
for each row
begin
    insert into log_auditoria
        (tabela, registro_id, acao, usuario_app_id, usuario_db, dados_antes, dados_depois)
    values
        ('ordem_servicos', new.id, 'update', @gos_usuario_id, current_user(),
         concat('status=', old.status,
                '; descricao=', old.descricao,
                '; cliente_id=', old.cliente_id,
                '; responsavel_id=', old.responsavel_id,
                '; servico_id=', old.classe_servico_id,
                '; data_fechamento=', ifnull(old.data_fechamento, '-')),
         concat('status=', new.status,
                '; descricao=', new.descricao,
                '; cliente_id=', new.cliente_id,
                '; responsavel_id=', new.responsavel_id,
                '; servico_id=', new.classe_servico_id,
                '; data_fechamento=', ifnull(new.data_fechamento, '-')));
end //

create trigger trg_os_after_delete
after delete on ordem_servicos
for each row
begin
    insert into log_auditoria
        (tabela, registro_id, acao, usuario_app_id, usuario_db, dados_antes, dados_depois)
    values
        ('ordem_servicos', old.id, 'delete', @gos_usuario_id, current_user(),
         concat('status=', old.status,
                '; descricao=', old.descricao,
                '; cliente_id=', old.cliente_id,
                '; responsavel_id=', old.responsavel_id,
                '; servico_id=', old.classe_servico_id,
                '; data_fechamento=', ifnull(old.data_fechamento, '-')),
         null);
end //

delimiter ;

-- =====================================================================
--  triggers de auditoria - usuarios
--  registra criação, exclusão e mudanças sensíveis. nunca grava o hash
--  da senha nem o e-mail:
--  a troca de senha aparece só como "senha=alterada".
--  contar tentativas de login falhas não gera log, só quando o cargo, o
--  bloqueio ou a senha mudam.
--  "<=>" é a igualdade que também funciona com null.
-- =====================================================================
drop trigger if exists trg_usuarios_after_insert;
drop trigger if exists trg_usuarios_after_update;
drop trigger if exists trg_usuarios_after_delete;

delimiter //

create trigger trg_usuarios_after_insert
after insert on usuarios
for each row
begin
    insert into log_auditoria
        (tabela, registro_id, acao, usuario_app_id, usuario_db, dados_antes, dados_depois)
    values
        ('usuarios', new.pessoa_id, 'insert', @gos_usuario_id, current_user(), null,
         concat('cargo=', new.cargo));
end //

create trigger trg_usuarios_after_update
after update on usuarios
for each row
begin
    if old.cargo <> new.cargo
       or not (old.bloqueado_ate <=> new.bloqueado_ate)
       or old.senha_hash <> new.senha_hash then
        insert into log_auditoria
            (tabela, registro_id, acao, usuario_app_id, usuario_db, dados_antes, dados_depois)
        values
            ('usuarios', new.pessoa_id, 'update', @gos_usuario_id, current_user(),
             concat('cargo=', old.cargo,
                    '; bloqueado_ate=', ifnull(old.bloqueado_ate, '-')),
             concat('cargo=', new.cargo,
                    '; bloqueado_ate=', ifnull(new.bloqueado_ate, '-'),
                    if(old.senha_hash <> new.senha_hash, '; senha=alterada', '')));
    end if;
end //

create trigger trg_usuarios_after_delete
after delete on usuarios
for each row
begin
    insert into log_auditoria
        (tabela, registro_id, acao, usuario_app_id, usuario_db, dados_antes, dados_depois)
    values
        ('usuarios', old.pessoa_id, 'delete', @gos_usuario_id, current_user(),
         concat('cargo=', old.cargo), null);
end //

delimiter ;