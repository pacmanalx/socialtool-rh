# Política de segurança

## Versões suportadas

Este projeto está em fase inicial de desenvolvimento (bancada de demonstração da Fase 1). Não há release estável ainda — atualizações de segurança se aplicam apenas ao branch `main`.

| Versão | Suporte |
| --- | --- |
| `main` | ✅ |
| Outras | ❌ |

## Reportando uma vulnerabilidade

**Não abra issue pública** para vulnerabilidades de segurança. Use um dos canais privados abaixo:

1. **GitHub Security Advisory** (preferencial): abra um advisory privado em [github.com/pacmanalx/socialtool-rh/security/advisories/new](https://github.com/pacmanalx/socialtool-rh/security/advisories/new).
2. **E-mail:** `pereira.guitar@gmail.com` — assunto começando com `[socialtool-rh security]`.

### O que incluir

- Descrição da vulnerabilidade e do impacto potencial.
- Passos para reproduzir (ou prova de conceito, se houver).
- Versão / commit afetado.
- Sugestão de mitigação, se souber.

### Prazos

- **Confirmação de recebimento:** até 48 horas úteis.
- **Análise inicial:** até 7 dias corridos.
- **Correção coordenada:** o cronograma depende da severidade; combinamos com o reportante antes de qualquer divulgação pública.

Créditos ao reportante são publicados no advisory, exceto quando pedido em contrário.

## Escopo

Estão em escopo vulnerabilidades no código deste repositório:

- Falhas de autenticação / autorização no backend (.NET).
- Vazamentos entre tenants no filtro multi-tenant.
- XSS, injeção, deserialização insegura, SSRF no backend ou frontend.
- Exposição de dados sensíveis por endpoints públicos.

**Fora de escopo** (não reportar como vulnerabilidade):

- A postura de demonstração documentada (senha `123456` no seed, `/api/auth/demo-users` anônimo, login automático no frontend) — isso é comportamento **intencional da bancada de demo** e está documentado em [CLAUDE.md](CLAUDE.md#postura-de-demonstração-por-desenho-não-por-descuido). Se você preparar o projeto para um ambiente real, é responsabilidade sua trocar esses padrões.
- Vulnerabilidades em dependências de terceiros — reporte diretamente ao mantenedor da dependência; se afetar este projeto, avise-nos para atualizarmos.
