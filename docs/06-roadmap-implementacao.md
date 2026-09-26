# 06. Roadmap e Fases de Implementação

## 🗺️ Visão Geral do Roadmap de Desenvolvimento

O projeto é estruturado em **3 fases estratégicas**, permitindo lançar valor rápido para os colaboradores no dia a dia (MVP com foco social e conexão) e progressivamente introduzir os módulos de gestão estratégica e avaliação de desempenho.

```mermaid
gantt
    title Roadmap de Desenvolvimento - SocialTool RH
    dateFormat  YYYY-MM-DD
    section Fase 1: MVP Core
    Scaffold .NET 10 + React + MySQL   :a1, 2026-10-01, 14d
    Multi-tenant, Auth & Usuários       :a2, after a1, 14d
    Feed Social & Celebrações (SignalR) :a3, after a2, 21d
    Reconhecimento & SocialCoins        :a4, after a3, 21d
    Feedback Contínuo & 1-on-1s         :a5, after a4, 21d
    section Fase 2: Clima & OKRs
    Humor Diário & Pesquisas de Pulso  :b1, after a5, 21d
    eNPS & Anonimização                :b2, after b1, 14d
    Gestão de OKRs & Check-ins Semanais :b3, after b2, 28d
    section Fase 3: Performance 360°
    Ciclos de Avaliação 360°           :c1, after b3, 28d
    Matriz 9-Box & Calibração          :c2, after c1, 21d
    Módulo de PDI & Analytics Executivo :c3, after c2, 21d
```

---

## 🎯 Detalhamento das Fases

### 🚀 Fase 1: MVP Core (Foco em Conexão e Rituais)
- **Objetivo**: Fazer o colaborador acessar a plataforma diariamente e sentir impacto imediato na comunicação e reconhecimento.
- **Entregas**:
  - Infraestrutura básica (.NET 10 Web API, React + Vite + Tailwind, MySQL 8.4).
  - Autenticação JWT com isolamento multi-tenant (`TenantId`).
  - Organograma de departamentos e perfis de colaboradores.
  - **Feed Social em tempo real**:
    - Criação de postagens, reações ricas e comentários.
    - Automação de aniversários de vida, empresa e boas-vindas.
  - **Reconhecimento & SocialCoins**:
    - Elogios vinculados aos valores da empresa.
    - Carteira de moedas e extrato mensal de doação.
  - **Feedback Contínuo & 1-on-1**:
    - Envio e solicitação de feedback com controle de privacidade.
    - Agendamento de 1-on-1, pautas colaborativas e planos de ação.

---

### 📊 Fase 2: Clima, Escuta Ativa & Alinhamento Estratégico
- **Objetivo**: Fornecer ao RH e aos líderes visibilidade sobre a saúde emocional da equipe e direcionamento de metas.
- **Entregas**:
  - **Termômetro Diário de Humor**:
    - Popup diário de check-in emocional de 5 estados.
    - Dashboard de evolução de humor da equipe (com filtro de quórum >= 4).
  - **Pesquisas de Pulso e eNPS**:
    - Criação de campanhas de pesquisa pelo RH.
    - Motor de cálculo de eNPS (% Promotores - % Detratores).
    - Desassociação estrita de identidade nas respostas.
  - **Gestão de OKRs**:
    - Cadastro de Objetivos da Empresa, Departamentos e Indivíduos.
    - Key Results com métricas percentuais, monetárias e numéricas.
    - Fluxo de check-in quinzenal com status de confiança e cálculo em cascata.

---

### 🌟 Fase 3: Desempenho 360°, 9-Box & People Analytics
- **Objetivo**: Fechar o ciclo anual/semestral de desenvolvimento de talentos com ferramentas executivas e de calibração.
- **Entregas**:
  - **Ciclos de Avaliação 360 Graus**:
    - Matriz de avaliadores (autoavaliação, gestor, pares e liderados).
    - Banco de competências comportamentais e técnicas.
    - Relatório visual de concordâncias e divergências (gráfico radar).
  - **Matriz 9-Box Interativa**:
    - Posicionamento automático (Desempenho x Potencial).
    - Painel com drag-and-drop para comitê de calibração do RH.
  - **PDI (Plano de Desenvolvimento Individual)**:
    - Metas de desenvolvimento integradas ao histórico de 1-on-1s.
  - **Dashboard Executivo de People Analytics**:
    - Cruzamento de dados de engajamento no feed, humor, metas batidas e notas de avaliação de desempenho.
