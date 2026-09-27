# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## O que é este projeto

**SocialTool RH** — plataforma opensource de engajamento e gestão de pessoas. **Uma instalação atende uma única
organização** (single-tenant). Repositório standalone. Duas camadas convivem aqui e **não descrevem a mesma coisa**:

| Camada | Onde | O que é |
|---|---|---|
| Projeto de produto | `README.md` + `docs/01..07` | **documentos de design**, escopo completo (OKRs, 360°, 9-Box, eNPS, loja de prêmios). Nada disso existe em código |
| Código que roda | `backend/` + `frontend/` | fatia estreita da Fase 1: feed, reconhecimento com SocialCoins, humor diário, autenticação (convite por e-mail, senha, Google Workspace) e gestão de usuários |

Ler `docs/` como intenção, nunca como descrição do que está implementado. O `README.md` cita como
stack planejada MediatR/CQRS, Redis/RabbitMQ/Hangfire, Shadcn/UI e Zustand — **nenhum deles está
instalado hoje** (`grep -riE 'mediatr|zustand|redis|hangfire' backend/src frontend/src` não retorna nada).
`@tanstack/react-query` e `react-router-dom` estão instalados mas **não usados**: não há
`QueryClientProvider` em `main.tsx` e a navegação de `App.tsx` é `useState` com abas, não rotas.

Estado: **Fase 1 em construção**, sem release. Não há Dockerfile de app publicado, pipeline de
release nem destino público — o `docker-compose.yml` da raiz sobe só a infraestrutura de desenvolvimento
(MySQL, Adminer, Mailpit).

## Convenções deste projeto

Convenções deliberadas — não "corrigir" para alinhar com outro estilo:

- **Identificadores em inglês** (`User`, `UserRole`, `FeedController`, `CoinsAvailableToGive`);
  comentários e strings de UI em português. Manter esse híbrido.
- **Controllers MVC clássicos** (`[ApiController]` + `[Route("api/[controller]")]`) em `Api/Controllers/`.
  Não há minimal API, não existe `Api/Endpoints/`.
- **Sem deploy formal.** Roda só localmente, `dotnet run` + `npm run dev`, com MySQL do
  `docker-compose.yml`.

## Arquitetura real do backend

`Domain → Application → Infrastructure → Api`, mas a **camada `Application` é uma casca**: só quatro
interfaces em `Common/Interfaces/` (`ICurrentUserService`, `IJwtTokenService`, `IPasswordHasher`,
`IEmailSender`). Não há handlers, commands nem services. `Api` referencia `Infrastructure`
diretamente e **os controllers injetam `ApplicationDbContext` e fazem acesso a dados inline** — essa é
a regra vigente, não um desvio. Regra de negócio nova entra no controller ou sobe para uma entidade de
`Domain`; inventar uma camada de aplicação aqui contraria todo o resto do código.

DTOs são `record`s posicionais em `Api/DTOs/` (`AuthDtos.cs`, `FeedDtos.cs`, `RecognitionDtos.cs`,
`RitualDtos.cs`), construídos por mapeamento manual (ver `FeedController.MapToPostDto`). Sem AutoMapper.

### Uma organização por instalação

Não existe tenant: nenhuma entidade tem `TenantId`, não há filtro global de consulta nem contexto de tenant.
A decisão foi deliberada (quem instala configura tudo para a sua própria organização) — não reintroduza
multi-tenancy "só por garantia"; ela traz de volta o risco de vazamento entre empresas em toda funcionalidade.

- **`Organization`** (tabela `organization`) é a configuração da instalação: nome, moeda, cota mensal e
  domínio Google Workspace. Tem **exatamente uma linha**, criada na primeira subida
  (`DbInitializer.EnsureOrganizationAsync`). Leia sempre por `db.GetOrganizationAsync()`.
- **`ActiveUserMiddleware`**, depois de `UseAuthentication()`, confere a cada requisição autenticada que o
  usuário continua ativo (uma consulta). É isso que faz a desativação cortar o acesso na hora, sem esperar o
  access token de 15 min vencer.
- **E-mail é único** (índice em `users.Email`, sempre gravado em minúsculas).
- **SignalR:** o hub só aceita conexões autenticadas e os controllers publicam para `Clients.All`.

**Tudo exige login por padrão** (`FallbackPolicy` em `Program.cs`). Endpoint público precisa de
`[AllowAnonymous]` explícito — hoje só os de `AuthController` (config, login, google, refresh, logout,
convite, esqueci/redefinir senha) e o OpenAPI em Development.

### Autenticação

- **Access token** JWT de 15 min, devolvido no corpo e guardado **só em memória** no front
  (`services/api.ts`). **Refresh token** opaco em cookie `st_refresh` (`httpOnly`, `SameSite=Strict`,
  `Path=/api/auth`), rotacionado a cada `POST /api/auth/refresh` (`SessionService`). No banco ficam só os
  hashes SHA-256 (`refresh_tokens`).
- **Reuso de refresh token** já rotacionado derruba todas as sessões do usuário — exceto dentro de 30 s da
  rotação, para duas abas que renovam ao mesmo tempo não se derrubarem.
- **Todo e-mail sai pelo `GuardedEmailSender`**, o único `IEmailSender` registrado. O `SmtpTransport` (MailKit)
  não implementa `IEmailSender` de propósito, para ninguém enviar por fora. Duas travas no `appsettings.json`:
  `Email:Enabled` (geral, `false` por padrão) e `Email:Redirect` (desvio, ligado por padrão e também quando a
  chave não existe: tudo vai para `Email:Redirect:To`; destino vazio = nada sai). Só `Email:Redirect:Enabled=false`
  explícito entrega a usuários reais. O e-mail desviado leva "[Desviado de <original>]" no assunto e aviso no
  corpo. `CanSendTo`/`IsEnabled` são checados antes de gerar token (`AccountTokenService`). Sem envio: convite
  individual só cadastra (`Pending`), reenvio e lote respondem 409, "esqueci minha senha" responde 202 sem
  token e o link some do login (`/api/auth/config` `passwordResetAvailable`). Nunca coloque um destino de desvio
  real no `appsettings.json` versionado (o repositório é público): use `appsettings.Development.json` ou
  variável de ambiente.
- **Convite e redefinição de senha** usam tokens de uso único em `user_tokens` (só o hash), enviados por
  e-mail via `AccountTokenService` → `IEmailSender` (`GuardedEmailSender` → `SmtpTransport`, MailKit). Consumir um token
  invalida todos os outros pendentes do usuário.
- **Usuário convidado** nasce com `PasswordHash = null` e `ActivatedAt = null` ("Convite pendente" na UI)
  e passa a ativo ao definir a senha ou entrar pelo Google.
- **Google Workspace:** o front usa o Google Identity Services e manda o ID token para `POST /api/auth/google`;
  o backend valida com `GoogleJsonWebSignature` (audience = `Auth:Google:ClientId`) e exige que o `hd` da
  conta (ou o domínio do e-mail, para domínios secundários) esteja em `Organization.GoogleWorkspaceDomains`
  (lista separada por vírgula; um Workspace pode ter vários domínios). Conta pessoal (sem `hd`) é sempre recusada. Só entra quem já foi convidado — Google não cria usuário.
- **Papéis** (`UserRole`) vão na claim de role. RH convida e edita cadastro; mudar papel, desativar/reativar
  e mexer em administradores é só do Admin (`AdminUsersController`). Comunicado oficial (`PostType.Announcement`)
  só RH/Admin (`FeedController.CreatePost`).
- **Importação de usuários** (`WorkspaceUserImportService` + `WorkspaceExportParser`, endpoints
  `POST /api/admin/users/import/preview` e `/import`, multipart): lê o JSON do Admin Console em memória
  (nunca grava o arquivo) e só os campos usados (nome, e-mail, cargo, departamento, status, último login).
  Preview e aplicação recalculam o mesmo plano; a aplicação roda numa transação única e grava um registro
  em `user_imports`. Regras que não podem regredir: nunca sobrescreve campo preenchido localmente, nunca
  reativa, nunca envia convite, não desativa a própria conta, e desativar suspensos é opção só de Admin.
  Situação na lista: `Pending` (nunca acessou, sem convite válido), `Invited`, `Active`, `Inactive`.
- **Limite de tentativas:** política `auth` (20/min por IP) nos endpoints anônimos de login/convite/senha.
- **Primeiro admin** de uma instalação nova: seção `Bootstrap` da configuração (`OrganizationBootstrapper`)
  nomeia a organização e, se ainda não houver nenhum usuário, cria o admin sem senha e manda o convite.
  Idempotente. Depois disso, nome, moeda, cota e domínio Google são editados pela UI
  (`AdminOrganizationController`, só Admin).
- **Dados de exemplo** (`DbInitializer.SeedSampleDataAsync`) só rodam em `Development` com
  `Seed:SampleData=true` e numa instalação sem usuários; senha em `Seed:SamplePassword`.

### Regras de negócio que moram em um lugar só

- **SocialCoins:** `RecognitionController.SendRecognition` é onde a moeda circula — debita
  `Sender.CoinsAvailableToGive`, credita `Receiver.CoinsBalanceToSpend`, cria o `Post` do tipo
  `Recognition` e o registro `Recognition` apontando para ele. Tudo num `SaveChangesAsync`, sem
  transação explícita. Barreiras: não reconhecer a si mesmo, moedas > 0, saldo suficiente.
- **Humor diário:** `UsersController.SubmitDailyMood` faz upsert por `(UserId, Date)` — índice único em
  `(UserId, Date)`. Um check-in por dia, editável.
- **Reação é toggle:** mesma `(PostId, UserId, Type)` mandada de novo remove a reação (índice único).

### Modelado mas sem endpoint

`Feedback`, `OneOnOne`, `OneOnOnePoint` e `OneOnOneAction` já têm entidade, configuração no
`OnModelCreating`, DbSet, enums (`FeedbackVisibility`/`FeedbackStatus`/`OneOnOneStatus`) e tabela criada
pela migration `InitialCreate` — falta DTO, controller e tela. Já **OKRs, pesquisas/eNPS, 360° e 9-Box
não existem em camada alguma**: só em `docs/`. As abas de "Módulo em Desenvolvimento" do `App.tsx`
misturam os dois casos — conferir se a entidade existe antes de assumir que é só plugar.

### Boot, migrations e seed

`Program.cs` roda `Database.MigrateAsync()`, depois os dados de exemplo (só em Development, e só com o banco
sem usuários) e o `OrganizationBootstrapper`. **O bloco inteiro está num try/catch que só
loga**: com o MySQL fora do ar o app sobe normalmente e só falha nas requisições. Se o feed vier vazio sem
erro visível, conferir o log da subida antes de suspeitar do front.

**Armadilha do EF:** as PKs são `Guid` gerados no cliente (`BaseEntity.Id = Guid.NewGuid()`).
Entidade nova com chave preenchida, descoberta por navegação, vira `UPDATE` de 0 linhas. O próprio seeder
contorna isso atribuindo `deptTi.LeaderId` **depois** do primeiro `SaveChangesAsync` (comentado no arquivo
como "para evitar ciclo no EF Core").

Migration nova (o `dotnet-ef` está fixado em `backend/dotnet-tools.json`):
```
cd backend && dotnet tool restore
dotnet tool run dotnet-ef migrations add <Nome> \
  -p src/SocialTool.Infrastructure -s src/SocialTool.Api
```
Aplicar não é preciso — o app migra na subida. **Revise a migration gerada no MySQL:** apagar um índice que
sustenta uma FK falha ("needed in a foreign key constraint"); crie o índice substituto antes do `DropIndex`
no `Up()` e inverta a ordem no `Down()`. Confira também se o EF gerou os arquivos em
`Persistence/Migrations/` (namespace `SocialTool.Infrastructure.Persistence.Migrations`) — com a pasta vazia
ele cai em `Migrations/` na raiz do projeto.

## Frontend

React 19 + TS + Vite 8 + Tailwind 4 (plugin `@tailwindcss/vite`, sem `tailwind.config`), lint com
**oxlint**. Ícones: `lucide-react`. Sem biblioteca de componentes — tudo é Tailwind inline.

Estado global = **um único `AuthContext`** (`status`: `loading` | `authenticated` | `anonymous`). Dados vêm
de `useState` + `useEffect` chamando `services/api.ts`, que é um wrapper `fetch` tipado agrupado por área
(`api.auth.*`, `api.admin.*`, `api.feed.*`, `api.recognition.*`, `api.users.*`). Endpoint novo entra ali,
não com `fetch` solto. O wrapper injeta `Authorization: Bearer` com o access token **em memória** (nunca
em `localStorage`) e, num 401, tenta **uma** renovação via `refreshSession()` — compartilhada entre
requisições simultâneas — antes de avisar o `AuthContext` que a sessão acabou. Endpoints de login/convite/
senha passam `skipRefresh: true`, porque lá o 401 é resposta de negócio.

`App.tsx` decide entre as telas públicas e o app: `/convite?token=` e `/redefinir-senha?token=` (links dos
e-mails, por `window.location.pathname` — não há roteador), login quando `anonymous`, e o app quando
`authenticated`. Dentro do app, abas por `useState` (`feed`, `recognitions`, `mood`, `admin-users` para
Admin/RH; as demais caem num cartão "Módulo em Desenvolvimento"). Componentes em
`components/{auth,account,admin,feed,layout,widgets}/`, tipos espelhados à mão em `types/index.ts`.

**Contrato de enums, meio torto:** `JsonStringEnumConverter` manda enums por
**nome** (`'Recognition'`, `'Heart'`) e `types/index.ts` os espelha como union de string literal — mas
`MoodScore` cruza como **int 1–5**, convertido por cast em `UsersController`. Ao mexer num enum, editar
os dois lados.

**SignalR só existe no servidor.** `SocialFeedHub` em `/hubs/feed` e os broadcasts dos controllers
(`ReceiveNewPost`, `ReceivePostUpdate`, `ReceivePostComment`) estão prontos, mas `@microsoft/signalr`
não está instalado no front — hoje ninguém escuta, e a atualização do mural é o botão "Atualizar".

## Rodar

```
# 1. banco local, Adminer (:8080) e Mailpit (:8025, recebe os e-mails)
cp .env.example .env
docker compose up -d

# 2. backend
cd backend && dotnet build
dotnet run --project src/SocialTool.Api      # http://localhost:5000 — migra e semeia na subida

# 3. frontend (outro terminal)
cd frontend && npm install && npm run dev    # http://localhost:3000
npm run build                                # tsc -b + vite build — único type-check do projeto
npm run lint                                 # oxlint (sem regras type-aware: lint verde != compila)
```
O Vite faz proxy de `/api` e `/hubs` para `:5000` (`vite.config.ts`), por isso o front usa caminhos
relativos e CORS não costuma ser o culpado.

**Não há projeto de teste** — `backend/tests/` existe e está vazio. Nenhum `dotnet test` encontra nada.

### Configuração

Toda a configuração mora em `appsettings.json` (sem fallback no código — chave ausente derruba a subida com
mensagem clara) e pode ser sobrescrita por variável de ambiente (`Seção__Chave`) ou por
`appsettings.Development.json` (no `.gitignore`). Os valores default (MySQL `localhost:3306`
`socialtool`/`socialtool_dev`, SMTP `localhost:1025`) batem com o `docker-compose.yml` — clonar,
`docker compose up -d` e `dotnet run` funciona sem edição. Fora de Development o backend **recusa subir**
com o `Jwt:SecretKey` de exemplo. Seções: `App:PublicUrl` (links dos e-mails), `Jwt`, `Auth`
(validade de convite/redefinição, `Google:ClientId`), `Email`, `Cors:AllowedOrigins` (vazio = sem CORS;
em dev o Vite faz proxy na mesma origem), `Bootstrap`, `Seed`.

A chave JWT default (`CHANGE-ME-IN-PRODUCTION-...`) é placeholder — trocar antes de qualquer deploy real.

OpenAPI (`AddOpenApi`/`MapOpenApi`) fica em `/openapi/v1.json` só em Development. O pacote **não** está
condicionado a `Debug` no `.csproj` — se este projeto ganhar imagem Docker um dia, revisar isso antes
(há CVEs conhecidos no `Microsoft.OpenApi` que valem checar).
