# 02. Arquitetura Técnica (.NET 10 + React + MySQL)

## 🏗️ Visão da Arquitetura

O sistema é construído seguindo os princípios de **Clean Architecture** (Arquitetura Limpa) e **Domain-Driven Design (DDD)** no backend, combinado a uma aplicação Single Page Application (SPA) em **React com TypeScript** no frontend.

```mermaid
flowchart TB
    subgraph Frontend ["Frontend (React + TypeScript)"]
        UI[Tailwind CSS + Shadcn/UI]
        State[Zustand + TanStack Query]
        SignalRClient[SignalR Hub Client]
    end

    subgraph Gateway ["Nginx / Reverse Proxy / API Gateway"]
        Proxy[SSL, Rate Limiting]
    end

    subgraph Backend [".NET 10 Web API (Clean Architecture)"]
        API[Presentation Layer / Minimal APIs / Controllers]
        App[Application Layer / MediatR CQRS]
        Domain[Domain Layer / Entities, Enums, Rules]
        Infra[Infrastructure Layer / EF Core 10, Repositories, Jobs]
        Realtime[SignalR Hubs / Realtime Events]
    end

    subgraph Storage ["Persistência & Cache"]
        MySQL[(MySQL 8.4+ Database)]
        Redis[(Redis Cache & Pub/Sub)]
        S3[Object Storage / MinIO / S3 - Mídias e Anexos]
    end

    UI --> State
    State --> Proxy
    SignalRClient <--> Realtime
    Proxy --> API
    API --> App
    App --> Domain
    App --> Infra
    Infra --> MySQL
    Infra --> Redis
    Infra --> S3
```

---

## 💻 Backend: C# .NET 10

### 1. Estrutura de Camadas (Clean Architecture)

```
backend/
├── src/
│   ├── SocialTool.Domain/             # Entidades de domínio, Enums, Eventos de domínio, Interfaces
│   ├── SocialTool.Application/        # Casos de uso (Commands/Queries MediatR), DTOs, Validations (FluentValidation)
│   ├── SocialTool.Infrastructure/     # EF Core DbContext, Migrations, MySQL Repositories, Storage, E-mail
│   └── SocialTool.WebApi/             # Endpoints, Middlewares (Auth, Exception), SignalR Hubs
└── tests/
    ├── SocialTool.Domain.Tests/
    ├── SocialTool.Application.Tests/
    └── SocialTool.IntegrationTests/
```

### 2. Uma Organização por Instalação

Cada instalação atende **uma única organização** (single-tenant). Não há coluna discriminadora nem filtro global por empresa: quem instala tem o banco inteiro para si, e a configuração da organização (nome, moeda, cota mensal, integrações) vive numa tabela de linha única, editada pelo administrador na interface.

Consequências de desenho:
- **Isolamento** entre empresas é físico (instalações e bancos separados), não lógico — elimina a classe de bugs de vazamento entre clientes.
- **Integrações** (e-mail, Google Workspace) são configuradas por instalação, com as credenciais da própria organização.
- **Autorização** fica só por papel (Colaborador, Líder, RH, Admin) e por relação entre pessoas (ex.: líder e liderados), sem uma dimensão extra de empresa.

### 3. Tempo Real com SignalR Hubs
- `FeedHub`: Dispara atualizações de novos posts, reações e comentários instantaneamente no mural da organização para todas as conexões autenticadas.
- `NotificationHub`: Entrega notificações diretas ao usuário (quando recebe um feedback, reconhecimento, menção ou lembrete de 1-on-1).

---

## 🎨 Frontend: React + TypeScript

### 1. Estrutura de Pastas Orientada a Funcionalidades (Feature-driven)

```
frontend/
├── src/
│   ├── assets/              # Ícones e imagens estáticas
│   ├── components/          # Componentes globais de UI (Button, Modal, Avatar, Dropdown - Shadcn/UI)
│   ├── contexts/            # Contextos de Autenticação e Tema
│   ├── hooks/               # Custom hooks reutilizáveis (useSignalR, useDebounce)
│   ├── lib/                 # Axios client, utilitários de data/formatação
│   ├── features/            # Módulos organizados por contexto de negócio:
│   │   ├── auth/            # Login, ativação de conta, recuperação de senha
│   │   ├── feed/            # Mural, criação de posts, reações, comentários
│   │   ├── recognition/     # Envio de reconhecimentos, extrato de moedas, catálogo de prêmios
│   │   ├── feedback/        # Solicitação e envio de feedbacks 1:1
│   │   ├── one-on-one/      # Agendamentos, pautas colaborativas, anotações de reunião
│   │   ├── surveys/         # Pesquisas de pulso e termômetro diário de humor
│   │   ├── okrs/            # Árvore de objetivos, resultados-chave e check-ins
│   │   └── performance/     # Ciclos de avaliação 360°, matriz 9-box e PDI
│   ├── router/              # Rotas protegidas (React Router v6+)
│   └── App.tsx
```

### 2. Gerenciamento de Estado
- **TanStack Query (React Query)**: Cache de requisições de servidor, paginação infinita para o Feed, invalidação automática de cache em mutações.
- **Zustand**: Estados globais leves de cliente (usuário logado, modal aberto, filtros ativos, status da conexão SignalR).

---

## 🐬 Banco de Dados: MySQL 8.4+

- **Driver .NET**: `Pomelo.EntityFrameworkCore.MySql` versão compatível com .NET 10.
- **CharSet e Collation**: `utf8mb4` e `utf8mb4_unicode_ci` (suporte integral a emojis em posts, reações e comentários).
- **Estratégia de Índices**: Índices nas colunas de filtro e ordenação mais usadas (ex: `IX_posts_CreatedAt` para o mural, `(UserId, Date)` único para o humor diário).
- **Auditoria Padrão**: Todas as tabelas herdam campos de auditoria (`created_at`, `created_by`, `updated_at`, `updated_by`, `is_deleted`).
