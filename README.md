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
> _(Descrição completa do sistema: a preencher.)_

## 2. Arquitetura

Aplicação desktop que conecta diretamente ao MySQL:

```
Usuário -> Telas (Windows Forms) -> Repositórios (C#) -> MySQL
```

_(Diagrama arquitetural com fronteiras de confiança: a preencher em `docs/`.)_

## 3. Tecnologias e dependências

- C# / .NET 8 (Windows Forms)
- MySQL 8.0.16 ou superior
- MySqlConnector (acesso ao MySQL)
- BCrypt.Net-Next (hash de senhas)
- System.Security.Cryptography (AesGcm, HMACSHA256 e RandomNumberGenerator)

## 4. Como compilar, instalar e executar

Pré-requisitos: Windows, .NET 8 SDK, MySQL 8.0.16+ (e MySQL Workbench, se quiser). Na primeira
compilação o `dotnet` baixa os pacotes (MySqlConnector e BCrypt.Net-Next), então é preciso internet.

1. **Criar o banco:** no Workbench, como `root`, executar `database/database.sql` e depois
   `database/triggers.sql`.
2. **Criar o usuário do banco da aplicação (`gos_app`):** copiar o bloco comentado no fim do
   `triggers.sql` para uma aba nova, remover os `--`, trocar `<definir_fora_do_repositorio>` por uma
   senha própria e executar como `root`. **Não salvar nem commitar a senha.**
   (Opcional: `database/testes_gos_app.sql` testa as permissões do `gos_app`.)
3. **Variáveis de ambiente** (PowerShell; valem só para a janela em que foram definidas):

   ```powershell
   $env:GOS_DB_USER     = "gos_app"
   $env:GOS_DB_PASSWORD = "a-senha-escolhida-no-passo-2"
   # opcionais (padrões: localhost, 3306, gos)
   # $env:GOS_DB_HOST = "localhost"; $env:GOS_DB_PORT = "3306"; $env:GOS_DB_NAME = "gos"
   ```

   > **Criptografia (pendente, responsável: Luan):** enquanto `src/GOS_Visual/Cripto.cs` for a versão
   > provisória (sem cifragem), a aplicação só inicia com `$env:GOS_PERMITIR_CRIPTO_PROVISORIA = "1"`.
   > Quando a criptografia real entrar, essa variável deixa de existir e este passo deve listar as
   > variáveis das chaves e como gerá-las.

4. **Compilar e executar** (na mesma janela do PowerShell):

   ```
   cd src/GOS_Visual
   dotnet run
   ```

   No primeiro acesso (banco sem usuários) abre a tela de cadastro do administrador; depois, o login.
   Para ver detalhes de erros do banco durante o desenvolvimento: `$env:GOS_DEBUG = "1"`.

## 5. Estrutura do repositório

| Pasta                | Conteúdo                                                         |
| -------------------- | ---------------------------------------------------------------- |
| `src/GOS_Visual`     | Aplicação desktop (versão final)                                 |
| `database`           | Esquema do banco (tabelas, triggers, view, procedures)           |
| `docs`               | Relatório técnico, modelagem de ameaças, relatórios de segurança |
| `legado/GOS_Console` | Primeira versão em console, só como referência                   |

## 6. Segurança

> [!IMPORTANTE]
> **Pendência:** Esta seção sobre a descrição do sistema deve ser preenchida por Cazz.
> _(Resumo dos controles e links para a modelagem de ameaças e para os relatórios
> em `docs/`: a preencher.)_

Regra do repositório: senhas, chaves e certificados **nunca** são commitados
(ver `.gitignore`).

## 7. Equipe

| Integrante                              | RA        | Área                 |
| --------------------------------------- | --------- | -------------------- |
| _(Matheus Hofstetter Trevisan Pereira)_ | 25021642  | Banco de dados       |
| _(Enrico Marcolini Rosselló)_           | 25021062  | Integração           |
| _(Luan Victor Fuzinatto)_               | 25005577  | Criptografia         |
| _(Gabriel Montagner Ferraz de Toledo)_  | preencham | Visual               |
| _(Caetano Bertoni Squarcini Vicco)_     | 25022360  | Análise de segurança |

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
