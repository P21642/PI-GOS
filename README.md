# G.O.S. - Gestão de Ordens de Serviço

Aplicação desktop (Windows Forms, C#/.NET 8) para registrar, consultar e acompanhar
ordens de serviço, com banco de dados MySQL.

> Trabalho de faculdade - integra POO, banco de dados, criptografia e
> desenvolvimento de software seguro. Entrega: 30/11/2026.

## 1. Escopo da solução

Funcionalidades: login, cadastro de usuários e clientes, abertura de ordens de
serviço e controle de status (`aberto`, `em andamento`, `concluido`, `cancelado`).

> [!IMPORTANTE]
> **Pendência:** Esta seção sobre a descrição do sistema deve ser preenchida por Gabriel.
_(Descrição completa do sistema: a preencher.)_

## 2. Arquitetura

Aplicação desktop que conecta diretamente ao MySQL:

```
Usuário -> Telas (Windows Forms) -> Repositórios (C#) -> MySQL
```
_(Diagrama arquitetural com fronteiras de confiança: a preencher em `docs/`.)_

## 3. Tecnologias e dependências
> [!IMPORTANTE]
> **Pendência:** Esta seção sobre a descrição do sistema deve ser preenchida por Luan.
- C# / .NET 8 (Windows Forms)
- MySQL 8.0.16 ou superior
- MySqlConnector (acesso ao MySQL)
- BCrypt.Net-Next (hash de senhas)
- _(Biblioteca de criptografia: a definir)_

## 4. Como compilar, instalar e executar
> [!IMPORTANTE]
> **Pendência:** Esta seção sobre a descrição do sistema deve ser preenchida por Enrico.

Pré-requisitos: Windows, .NET 8 SDK, MySQL 8.0.16+ (e MySQL Workbench, se quiser).

1. Criar o banco: abrir `database/database.sql` no Workbench e executar tudo.
2. Criar o usuário do banco da aplicação (comandos comentados no fim do
   `database.sql`), escolhendo uma senha própria. **Não colocar a senha no código.**
3. Configurar as variáveis de ambiente de conexão e de chaves.
   _(Nomes das variáveis: será definido na integração.)_
4. Compilar e executar:

   ```
   cd src/GOS_Visual
   dotnet run
   ```

_(NÃO PODEMOS ESQUECER DE REVISAR ESTES PASSOS EM UMA MÁQUINA LIMPA ANTES DA ENTREGA!!!.)_

## 5. Estrutura do repositório

| Pasta | Conteúdo |
|---|---|
| `src/GOS_Visual` | Aplicação desktop (versão final) |
| `database` | Esquema do banco (tabelas, triggers, view, procedures) |
| `docs` | Relatório técnico, modelagem de ameaças, relatórios de segurança |
| `legado/GOS_Console` | Primeira versão em console, só como referência |

## 6. Segurança
> [!IMPORTANTE]
> **Pendência:** Esta seção sobre a descrição do sistema deve ser preenchida por Cazz.
_(Resumo dos controles e links para a modelagem de ameaças e para os relatórios
em `docs/`: a preencher.)_

Regra do repositório: senhas, chaves e certificados **nunca** são commitados
(ver `.gitignore`).

## 7. Equipe

| Integrante | RA | Área |
|---|---|---|
| _(Matheus Hofstetter Trevisan Pereira)_ | 25021642 | Banco de dados |
| _(Enrico Marcolini Rosselló)_ | preencham | Integração |
| _(Luan Victor Fuzinatto)_ | preencham | Criptografia |
| _(Gabriel Montagner Ferraz de Toledo)_ | preencham | Visual |
| _(Caetano Bertoni Squarcini Vicco)_ | 25022360 | Análise de segurança |

## 8. Convenções de Git

- `main` só recebe código via Pull Request revisado por outra pessoa.
- Uma branch por área: `feat/banco`, `feat/integracao`, `feat/cripto`,
  `feat/visual`, `docs/seguranca`.
- Mensagens de commit começam pela área, por exemplo:
  `banco: adiciona trigger de auditoria em ordem_servicos`.
- Cada pessoa faz commit do próprio trabalho. Sem `push --force` e sem
  reescrever histórico.
- Nos Pull Requests, usar "Merge commit" ou "Rebase and merge" (não "Squash"),
  para manter os commits individuais.
