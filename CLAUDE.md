# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## O que é este projeto

**SocialTool RH** — plataforma opensource de engajamento e gestão de pessoas, multi-tenant. Repositório
standalone. Duas camadas convivem aqui e **não descrevem a mesma coisa**:

| Camada | Onde | O que é |
|---|---|---|
| Projeto de produto | `README.md` + `docs/01..07` | **documentos de design**, escopo completo (OKRs, 360°, 9-Box, eNPS, loja de prêmios). Nada disso existe em código |
| Código que roda | `backend/` + `frontend/` | fatia estreita da Fase 1: feed, reconhecimento com SocialCoins, humor diário, login demo |

Ler `docs/` como intenção, nunca como descrição do que está implementado. O `README.md` cita como
stack planejada MediatR/CQRS, Redis/RabbitMQ/Hangfire, Shadcn/UI e Zustand — **nenhum deles está
instalado hoje** (`grep -riE 'mediatr|zustand|redis|hangfire' backend/src frontend/src` não retorna nada).
`@tanstack/react-query` e `react-router-dom` estão instalados mas **não usados**: não há
`QueryClientProvider` em `main.tsx` e a navegação de `App.tsx` é `useState` com abas, não rotas.

Estado: **bancada de demonstração**, não produção. Não há Dockerfile de app publicado, pipeline de
release nem destino público — o `docker-compose.yml` da raiz é apenas para subir MySQL local para
desenvolvimento.

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
interfaces em `Common/Interfaces/` (`ITenantContext`, `ICurrentUserService`, `IJwtTokenService`,
`IPasswordHasher`). Não há handlers, commands nem services. `Api` referencia `Infrastructure`
diretamente e **os controllers injetam `ApplicationDbContext` e fazem acesso a dados inline** — essa é
a regra vigente, não um desvio. Regra de negócio nova entra no controller ou sobe para uma entidade de
`Domain`; inventar uma camada de aplicação aqui contraria todo o resto do código.

DTOs são `record`s posicionais em `Api/DTOs/` (`AuthDtos.cs`, `FeedDtos.cs`, `RecognitionDtos.cs`,
`RitualDtos.cs`), construídos por mapeamento manual (ver `FeedController.MapToPostDto`). Sem AutoMapper.

### Multi-tenancy — a parte que exige ler três arquivos

`TenantResolutionMiddleware` resolve o tenant na entrada e `ApplicationDbContext` aplica
`HasQueryFilter` em toda entidade `ITenantEntity`. Dois detalhes não óbvios:

- **O middleware roda antes de `UseAuthentication()`** (`Program.cs`). Logo o passo 2 dele — ler a claim
  `TenantId`/`Subdomain` do JWT — é **código morto**: `context.User.Identity.IsAuthenticated` é sempre
  `false` ali. Na prática o tenant vem do header `X-Tenant-Id` mandado pelo cliente (o front crava
  `'demo'` em `services/api.ts`), depois do subdomínio do Host, e por fim do literal `"demo"` no fallback.
- **O filtro degrada aberto, não fechado:** `!_tenantContext.HasTenant || e.TenantId == ...`. Sem tenant
  resolvido o filtro se desliga e a consulta atravessa todos os tenants — inclusive a busca de usuário do
  `Login`, que roda sem tenant garantido.

A maioria dos GETs é **anônima**: `/api/feed/posts`, `/api/users`, `/api/recognition/values`,
`/api/recognition/leaderboard`, `/api/auth/demo-users`. Só escrita e `/api/auth/me` levam `[Authorize]`.
Combinado com o item acima: o escopo de leitura é o header que o chamador mandar.

### Postura de demonstração (por desenho, não por descuido)

O seed dá a senha `123456` a todos, `GET /api/auth/demo-users` devolve o elenco sem autenticação com o
campo `DefaultPassword`, e `AuthContext.tsx` faz **login automático** de um usuário demo na subida do
app. É o mecanismo de troca de persona da bancada — não é o modelo de autenticação pretendido
(esse está em `docs/05-seguranca-lgpd-permissoes.md`).

### Regras de negócio que moram em um lugar só

- **SocialCoins:** `RecognitionController.SendRecognition` é onde a moeda circula — debita
  `Sender.CoinsAvailableToGive`, credita `Receiver.CoinsBalanceToSpend`, cria o `Post` do tipo
  `Recognition` e o registro `Recognition` apontando para ele. Tudo num `SaveChangesAsync`, sem
  transação explícita. Barreiras: não reconhecer a si mesmo, moedas > 0, saldo suficiente.
- **Humor diário:** `UsersController.SubmitDailyMood` faz upsert por `(UserId, Date)` — índice único em
  `(TenantId, UserId, Date)`. Um check-in por dia, editável.
- **Reação é toggle:** mesma `(PostId, UserId, Type)` mandada de novo remove a reação (índice único).

### Modelado mas sem endpoint

`Feedback`, `OneOnOne`, `OneOnOnePoint` e `OneOnOneAction` já têm entidade, configuração no
`OnModelCreating`, DbSet, enums (`FeedbackVisibility`/`FeedbackStatus`/`OneOnOneStatus`) e tabela criada
pela migration `InitialCreate` — falta DTO, controller e tela. Já **OKRs, pesquisas/eNPS, 360° e 9-Box
não existem em camada alguma**: só em `docs/`. As abas de "Módulo em Desenvolvimento" do `App.tsx`
misturam os dois casos e o texto delas ("as tabelas já foram modeladas") só vale para o primeiro —
conferir se a entidade existe antes de assumir que é só plugar.

### Boot, migrations e seed

`Program.cs` chama `DbInitializer.SeedAsync`, que roda `Database.MigrateAsync()` e depois semeia — o seed
é idempotente pela guarda de existência do tenant demo. **O bloco inteiro está num
try/catch que só loga**: com o MySQL fora do ar o app sobe normalmente e só falha nas requisições. Se o
feed vier vazio sem erro visível, conferir o log da subida antes de suspeitar do front.

**Armadilha do EF:** as PKs são `Guid` gerados no cliente (`BaseEntity.Id = Guid.NewGuid()`).
Entidade nova com chave preenchida, descoberta por navegação, vira `UPDATE` de 0 linhas. O próprio seeder
contorna isso atribuindo `deptTi.LeaderId` **depois** do primeiro `SaveChangesAsync` (comentado no arquivo
como "para evitar ciclo no EF Core").

Migration nova:
```
cd backend && dotnet ef migrations add <Nome> \
  -p src/SocialTool.Infrastructure -s src/SocialTool.Api
```
Aplicar não é preciso — o app migra na subida.

## Frontend

React 19 + TS + Vite 8 + Tailwind 4 (plugin `@tailwindcss/vite`, sem `tailwind.config`), lint com
**oxlint**. Ícones: `lucide-react`. Sem biblioteca de componentes — tudo é Tailwind inline.

Estado global = **um único `AuthContext`**. Dados vêm de `useState` + `useEffect` chamando `services/api.ts`,
que é um wrapper `fetch` tipado agrupado por área (`api.feed.*`, `api.recognition.*`, `api.users.*`,
`api.auth.*`). Ele injeta `Authorization: Bearer` (token em `localStorage['socialtool_token']`) e um
header `X-Tenant-Id` fixo em **toda** requisição. Endpoint novo entra ali, não com `fetch` solto.

`App.tsx` é a aplicação inteira: abas por `useState` (`feed`, `recognitions`, `mood`; as demais caem num
cartão "Módulo em Desenvolvimento"), modal de reconhecimento e composição dos widgets. Componentes em
`components/{feed,layout,widgets}/`, tipos espelhados à mão em `types/index.ts`.

**Contrato de enums, meio torto:** `JsonStringEnumConverter` manda enums por
**nome** (`'Recognition'`, `'Heart'`) e `types/index.ts` os espelha como union de string literal — mas
`MoodScore` cruza como **int 1–5**, convertido por cast em `UsersController`. Ao mexer num enum, editar
os dois lados.

**SignalR só existe no servidor.** `SocialFeedHub` em `/hubs/feed` e os broadcasts dos controllers
(`ReceiveNewPost`, `ReceivePostUpdate`, `ReceivePostComment`) estão prontos, mas `@microsoft/signalr`
não está instalado no front — hoje ninguém escuta, e a atualização do mural é o botão "Atualizar".

## Rodar

```
# 1. banco local (uma vez)
cp .env.example .env
docker compose up -d db

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

Connection string e chave JWT estão em `appsettings.json` **e** repetidas como fallback `??` em
`Program.cs`. **Ainda não há leitura de variável de ambiente**: trocar o banco por outra credencial
exige editar os dois lugares (ou criar `appsettings.Development.json`, que já está no `.gitignore`).
Os valores default (`localhost:3306`, user `socialtool`, senha `socialtool_dev`) batem com o
`docker-compose.yml` — clonar, rodar `docker compose up -d db` e `dotnet run` deve funcionar sem edição.

A chave JWT default (`CHANGE-ME-IN-PRODUCTION-...`) é placeholder — trocar antes de qualquer deploy real.

OpenAPI (`AddOpenApi`/`MapOpenApi`) fica em `/openapi/v1.json` só em Development. O pacote **não** está
condicionado a `Debug` no `.csproj` — se este projeto ganhar imagem Docker um dia, revisar isso antes
(há CVEs conhecidos no `Microsoft.OpenApi` que valem checar).
