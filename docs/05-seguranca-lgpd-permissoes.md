# 05. Segurança, RBAC & Conformidade LGPD

## 🛡️ Matriz de Controle de Acesso (RBAC)

O sistema conta com 4 perfis fundamentais de permissão, garantindo o princípio do menor privilégio:

| Funcionalidade / Recurso | Colaborador | Líder / Gestor | RH / People Ops | Admin Geral |
| :--- | :---: | :---: | :---: | :---: |
| **Visualizar & Interagir no Feed** | ✅ | ✅ | ✅ | ✅ |
| **Criar Comunicados Oficiais / Fixados** | ❌ | ❌ | ✅ | ✅ |
| **Enviar / Receber Reconhecimentos** | ✅ | ✅ | ✅ | ✅ |
| **Gerenciar Valores e Loja de Prêmios** | ❌ | ❌ | ✅ | ✅ |
| **Realizar 1-on-1 com Liderados** | Participante | ✅ (Com seu time) | Visão Agregada | Configuração |
| **Acessar Notas Privadas de 1-on-1** | Apenas suas | Apenas suas | ❌ | ❌ |
| **Visualizar Humor do Time** | ❌ | ✅ (Apenas se >= 4) | ✅ (Apenas se >= 4) | ✅ |
| **Criar Ciclos de Avaliação 360°** | ❌ | ❌ | ✅ | ✅ |
| **Acessar Matriz 9-Box e Calibração** | ❌ | Do seu time | Toda a empresa | Toda a empresa |
| **Gerenciar Assinatura, Billing e Usuários** | ❌ | ❌ | ❌ | ✅ |

---

## 🔒 Garantia de Isolamento Multi-Tenant

Para impedir qualquer vazamento de dados entre empresas clientes (*Cross-Tenant Data Leak*):

1. **Validação em Camada Dupla**:
   - **Camada de Aplicação (Middleware)**: O `tenant_id` é extraído do JWT e do subdomínio verificado. Se houver discrepância entre o token e o subdomínio, a requisição é rejeitada imediatamente (`HTTP 403 Forbidden`).
   - **Camada de Banco de Dados (EF Core 10)**: O `HasQueryFilter` aplica `WHERE tenant_id = @currentTenantId` em todas as consultas SQL geradas automaticamente.
2. **Prevenção de Injeção de Tenant**:
   - As operações de escrita (`INSERT` / `UPDATE`) preenchem o `tenant_id` automaticamente a partir do contexto de sessão do usuário autenticado, ignorando qualquer `tenant_id` enviado no corpo da requisição pelo cliente.

---

## ⚖️ Conformidade com a LGPD (Lei 13.709/2018)

### 1. Anonimização em Pesquisas e Humor
- Comentários e notas de pesquisas de clima nunca guardam relação física ou lógica com a identidade do respondente.
- Nenhum relatório exibe recortes de grupos com menos de 4 colaboradores, inviabilizando a identificação por dedução demográfica.

### 2. Tratamento de Dados de Ex-Colaboradores (Offboarding & Direito de Esquecimento)
- Quando um colaborador é desligado:
  - Sua conta é inativada imediatamente (`is_active = false`).
  - Reconhecimentos e posts históricos no mural permanecem para manter o histórico da comunidade, mas o nome pode ser anonimizado caso solicitado pelo titular (`"Ex-colaborador"`).
  - Anotações privadas e rascunhos são permanentemente excluídos.

### 3. Criptografia e Armazenamento Seguro
- **Senhas**: Armazenadas com algoritmo **Argon2id** ou **BCrypt** com alto fator de custo e salt aleatório.
- **Campos Ultrassensíveis**: Anotações privadas de 1-on-1 são criptografadas em repouso no banco de dados usando **AES-256-GCM**.
- **Comunicação Segura**: Tráfego 100% criptografado com TLS 1.3 (HTTPS e WSS para SignalR).

### 4. Trilha de Auditoria (*Audit Trail*)
Todas as operações críticas (alteração de permissões, calibração no 9-box, exclusão de usuários e resgate de recompensas) são registradas em log imutável de auditoria com:
- `timestamp_utc`
- `actor_user_id`
- `ip_address`
- `action_type`
- `entity_type` e `entity_id`
- `diff_payload` (estado anterior vs. novo estado)
