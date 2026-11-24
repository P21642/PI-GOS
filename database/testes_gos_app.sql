-- =====================================================================
--  g.o.s. - teste do usuário gos_app (privilégio mínimo)
--
--  como usar
--  1. banco limpo: drop database if exists gos; rode database.sql e
--     depois triggers.sql (como root).
--  2. como root, crie o gos_app (bloco comentado no fim do triggers.sql),
--     com a sua senha.
--  3. no workbench, crie uma conexão nova com o usuário gos_app e abra
--     este arquivo nela (não na conexão do root).
--  4. parte a: deve rodar sem nenhum erro (execute tudo).
--  5. parte b: cada comando deve dar erro. execute um por vez
--     (ctrl+enter), ou desligue "stop script execution on errors" na
--     barra do editor e execute tudo.
--  compare cada resultado com a linha "-- esperado".
-- =====================================================================

use gos;
-- a aplicação faz isto ao abrir cada conexão, com o id de quem está logado:
set @gos_usuario_id = 1;

-- =====================================================================
--  parte a - tudo isto deve funcionar com o gos_app
-- =====================================================================

-- a1. cadastros pelas procedures.
call sp_cadastrar_usuario('admin teste',    unhex('aa11'), repeat('a',64), 'hash_teste_1', 'admin');
call sp_cadastrar_usuario('carlos técnico', unhex('bb22'), repeat('b',64), 'hash_teste_2', 'padrao');
call sp_cadastrar_cliente('maria silva',    unhex('cc33'), repeat('c',64), unhex('dd44'));
-- esperado: cada call devolve o id novo (1, 2 e 3).

-- a2. login: busca pelo email_hash (o c# calcula o hash do e-mail digitado).
select p.id, p.nome, u.cargo
from pessoas p join usuarios u on u.pessoa_id = p.id
where p.email_hash = repeat('a',64);
-- esperado: 1 linha: 1, admin teste, admin.

-- a3. abrir uma ordem e mudar o status.
insert into ordem_servicos (descricao, status, cliente_id, responsavel_id, classe_servico_id)
values ('troca de fonte', 'aberto', 3, 2, 1);
update ordem_servicos set status = 'em andamento' where id = 1;
update ordem_servicos set status = 'concluido'    where id = 1;
select id, status, data_fechamento from ordem_servicos;
-- esperado: sem erro; status concluido e data_fechamento preenchida.

-- a4. view usada na listagem de ordens.
select id, status, cliente_nome, responsavel_nome, tipo_de_servico from vw_ordens_detalhadas;
-- esperado: 1 linha: maria silva (cliente) e carlos técnico (responsável).

-- a5. tentativas de login falhas (não gera linha no log).
update usuarios set tentativas_falhas = 1 where pessoa_id = 2;
-- esperado: sem erro.

-- a6. cadastrar e excluir um cliente sem ordens.
call sp_cadastrar_cliente('sem os', unhex('ee55'), repeat('e',64), unhex('ff66'));
call sp_excluir_cliente(4);
select count(*) as pessoas_restantes from pessoas;
-- esperado: o cadastro devolve id 4; depois da exclusão, pessoas_restantes = 3.

-- a7. o gos_app consegue ler o log, e os triggers gravaram nele.
select id, tabela, registro_id, acao, usuario_app_id, usuario_db
from log_auditoria order by id;
-- esperado: linhas de insert/update (usuarios e ordem_servicos),
--           usuario_app_id = 1 e usuario_db = gos_app@localhost.

-- =====================================================================
--  parte b - tudo isto deve dar erro
-- =====================================================================

-- b1. gravar direto no log.
insert into log_auditoria (tabela, registro_id, acao, usuario_db) values ('x', 1, 'insert', 'x');
-- esperado: error 1142: insert command denied

-- b2. alterar o log.
update log_auditoria set tabela = 'adulterado';
-- esperado: error 1142: update command denied

-- b3. apagar o log.
delete from log_auditoria;
-- esperado: error 1142: delete command denied

-- b4. esvaziar o log.
truncate table log_auditoria;
-- esperado: error 1142: drop command denied

-- b5. apagar uma tabela.
drop table servicos;
-- esperado: error 1142: drop command denied

-- b6. alterar a estrutura de uma tabela.
alter table usuarios add column extra int;
-- esperado: error 1142: alter command denied

-- b7. remover um trigger de auditoria.
drop trigger trg_os_after_update;
-- esperado: error 1142: trigger command denied

-- b8. criar outro usuário no banco.
create user 'intruso'@'localhost' identified by 'x';
-- esperado: error 1227: access denied; you need the create user privilege

-- b9. alterar a tabela de serviços (o gos_app só lê).
insert into servicos (tipo_de_servico) values ('manutencao');
-- esperado: error 1142: insert command denied

-- b10. ler as contas do servidor.
select user from mysql.user;
-- esperado: error 1142: select command denied

-- b11. as regras dos triggers valem para o gos_app: ordem concluída não muda.
update ordem_servicos set status = 'aberto' where id = 1;
-- esperado: error 1644: ordem concluída ou cancelada não pode ser alterada

-- b12. o último administrador não pode ser excluído.
call sp_excluir_usuario(1);
-- esperado: error 1644: não é possível excluir o último administrador
