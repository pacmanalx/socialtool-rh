# 01. Visão Geral e Modelo de Negócio

## 🎯 Visão do Produto

O **SocialTool RH** é uma plataforma corporativa "all-in-one" de engajamento e gestão de pessoas, cujo propósito é transformar a cultura organizacional por meio do engajamento, transparência, escuta ativa e reconhecimento de equipes.

Diferente de redes sociais e comunicadores corporativos genéricos, o foco do SocialTool é estruturar as interações para que gerem **dados acionáveis para o RH** e **desenvolvimento real para as pessoas**.

---

## 👥 Personas do Sistema

```mermaid
mindmap
  root((Usuários do Sistema))
    Colaborador
      Postar e interagir no Feed
      Enviar e receber reconhecimentos
      Dar e solicitar feedbacks
      Registrar humor diário
      Acompanhar seus OKRs
      Participar de 1-on-1s e 360°
    Líder de Equipe
      Acompanhar engajamento do time
      Conduzir rituais de 1-on-1
      Avaliar liderados 360° e 9-box
      Acompanhar desdobramento de OKRs
      Ver termômetro de humor da equipe
    RH / People Ops
      Gerenciar ciclos de avaliação
      Criar pesquisas de clima e eNPS
      Moderar conteúdo e gerenciar valores
      Parametrizar moedas e loja virtual
      Relatórios de turnover e engajamento
    Administrador / C-Level
      Visão holística da saúde organizacional
      Alinhamento dos OKRs globais da empresa
      Configurações da assinatura e billing
```

---

## 💡 Dores de Mercado Resolvidas

| Problema Tradicional | Solução do SocialTool RH |
| :--- | :--- |
| **Comunicação interna dispersa** em e-mails e grupos informais sem registro ou retenção de cultura. | **Feed Social Corporativo** centralizado, categorizado por canais e valores da empresa. |
| **Falta de reconhecimento no dia a dia**, gerando desmotivação e turnover silencioso. | **Gamificação com moedas virtuais e badges** vinculados diretamente aos pilares da empresa. |
| **1-on-1s esquecidas ou improvisadas**, sem plano de ação ou histórico de alinhamento. | **Gestão de 1-on-1s** com pautas prévias, ata compartilhada, notas privadas e tarefas. |
| **Pesquisas de clima anuais demoradas**, onde os problemas já escalaram quando o resultado sai. | **Pesquisas de pulso ágeis e termômetro diário de humor**, capturando a variação em tempo real. |
| **Metas em planilhas desconectadas da rotina**, esquecidas após o início do trimestre. | **Gestão integrada de OKRs**, com check-ins semanais e vínculo com reconhecimentos. |
| **Avaliações de desempenho burocráticas**, com viés de recência e sem evidências históricas. | **Avaliação 360° e Matriz 9-Box** abastecida pelo histórico de feedbacks contínuos e 1-on-1s do ano. |

---

## 🏢 Modelo SaaS Multi-Tenant

A plataforma opera sob o modelo **Software as a Service (SaaS) Multi-tenant**:

1. **Isolamento Lógico Robusto**: Cada empresa cliente possui seu próprio `tenant_id`, garantindo que colaboradores de uma organização nunca acessem dados de outra.
2. **Customização por Empresa**:
   - Logotipo e cores institucionais (white-label leve).
   - Nome personalizado da moeda corporativa (ex: *FeedCoins*, *Estrelas*, *Pontos de Valor*).
   - Definição dos **Valores da Cultura** que guiam os reconhecimentos.
   - Organograma, departamentos, cargos e níveis hierárquicos.
3. **Planos & Escalabilidade**:
   - Cobrança baseada no número de colaboradores ativos mensais (Seat-based pricing).
   - Ativação modular: empresas podem iniciar apenas com Feed + Feedback e posteriormente habilitar OKRs e Avaliação 360°.
