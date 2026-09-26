# Contribuindo com o SocialTool RH

Obrigado pelo interesse em contribuir! Este documento reúne o essencial para você trocar bem com o projeto.

## Antes de tudo

Leia estes dois antes de começar:

- [**README.md**](README.md) — visão, stack e como rodar localmente.
- [**CLAUDE.md**](CLAUDE.md) — convenções técnicas, armadilhas conhecidas e o mapa entre o que está em `docs/` (intenção) e o que existe em `backend/`+`frontend/` (implementação real).

O `CLAUDE.md` evita 80% dos falsos desalinhamentos — por exemplo, "MediatR está no README mas não no código" é intencional, "Application é uma casca" é intencional, "identificadores em inglês com UI em português" é intencional.

## Como reportar bugs

Abra uma issue com:

1. **Título** descritivo do problema.
2. **Passos para reproduzir**, ambiente (SO, versão do .NET, versão do Node) e comportamento observado vs. esperado.
3. **Logs relevantes** (do backend `dotnet run`, do console do navegador, do MySQL).
4. **Se aplicável**, dump da tabela envolvida ou body da requisição que falhou.

Para vulnerabilidades de segurança, **não** abra issue pública — siga o [SECURITY.md](SECURITY.md).

## Como propor uma funcionalidade

- **Ideias novas fora do roadmap** ([docs/06-roadmap-implementacao.md](docs/06-roadmap-implementacao.md)): abra uma issue de discussão antes de codar. É rápido validar direção e evita PR grande sendo recusado por desalinhamento.
- **Módulos que já existem no roadmap mas ainda não têm código** (OKRs, pesquisas/eNPS, 360°, 9-Box): o spec está em `docs/03-modulos-funcionais/`. Confira também se a entidade já existe em `Domain/` (ver seção "Modelado mas sem endpoint" no [CLAUDE.md](CLAUDE.md)) antes de assumir que "é só plugar".

## Como enviar código

1. **Fork** o repositório e crie uma branch descritiva a partir de `main`:
   ```
   git checkout -b feat/nome-da-funcionalidade
   git checkout -b fix/descricao-do-bug
   ```
2. **Rode o projeto local** e valide manualmente o que mudou (ver `README.md`).
3. **Escreva commits pequenos e coerentes**, no imperativo curto (`add`, `fix`, `refactor`, `docs`, `chore`).
4. **Antes de abrir o PR**, garanta que:
   - `dotnet build` no `backend/` passa sem warning novo.
   - `npm run build` no `frontend/` passa (é o único type-check do projeto; `npm run lint` sozinho não pega erros de tipo).
   - Nenhum segredo, IP interno de rede pessoal ou credencial real vai no commit.
5. **Abra o PR** contra `main`, descrevendo:
   - O quê mudou e por quê.
   - Como testar.
   - Referência da issue relacionada, se houver.

## Estilo de código

Herda do `CLAUDE.md`. Em resumo:

- **Backend .NET:** controllers MVC clássicos, controllers podem injetar `ApplicationDbContext` diretamente (a `Application` layer é casca por decisão). Identificadores em inglês.
- **Frontend React:** endpoint novo entra em `services/api.ts`, não como `fetch` solto. Estado local com `useState`/`useEffect`; nada de biblioteca de estado global adicional sem discussão. Tailwind inline, sem biblioteca de componentes.
- **Enums:** ao mexer num enum, editar backend E `frontend/src/types/index.ts` (contrato torto — `MoodScore` cruza como int, o resto como string literal).

## Testes

Ainda não há suite de testes (`backend/tests/` está vazio, sem projeto configurado). Se você quiser contribuir com o esqueleto de testes (xUnit para backend, Vitest para frontend), abra uma issue de discussão primeiro — a decisão de framework e de estrutura precisa acontecer uma vez só.

## Licença

Ao contribuir, você concorda que sua contribuição será licenciada sob a [Licença MIT](LICENSE), a mesma do projeto.

## Código de conduta

Este projeto adota o [Contributor Covenant](CODE_OF_CONDUCT.md). Ao participar, você concorda em manter o ambiente respeitoso e inclusivo.
