# Política de segurança

## Versões suportadas

Este projeto está em fase inicial de desenvolvimento (Fase 1). Não há release estável ainda — atualizações de segurança se aplicam apenas ao branch `main`.

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
- XSS, injeção, deserialização insegura, SSRF no backend ou frontend.
- Exposição de dados sensíveis por endpoints públicos.

**Fora de escopo** (não reportar como vulnerabilidade):

- Os usuários de exemplo com senha conhecida (`socialtool-dev`), criados **somente** quando o backend roda em `Development`.
- Os valores de exemplo em `appsettings.json` (segredo JWT, senha do MySQL local). Fora de `Development` o backend se recusa a subir com o segredo JWT de exemplo, e as credenciais reais devem vir de variáveis de ambiente.
- Vulnerabilidades em dependências de terceiros — reporte diretamente ao mantenedor da dependência; se afetar este projeto, avise-nos para atualizarmos.
