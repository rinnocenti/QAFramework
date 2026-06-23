# F12-01 — ADR-INPUT-001 — Input Ownership

Status: Draft / Renumbered
Fase: F12
Ordem no Plano: F12-01
Tipo: Consumer / Input
Escopo: Input

---

## Contexto

Input é consumer de Route/Activity/Participation/Transition. Não deve ditar lifecycle core nem usar string solta como action map identity.

F10 já define transition input lock policy. F12 define o consumer de input real.

---

## Decisão

Input mode deve ser declarado por contrato e aplicado por consumer.

- Owner atual do input mode é explícito.
- Activity/Route/Pause/Transition declaram requirement.
- Consumer aplica e libera.
- Action map string pode ser implementation detail, não chave funcional pública.
- PlayerSlot/Participation vem de F11.

---

## Critérios de validação

- Activity enter muda input mode.
- Activity exit restaura/libera.
- Transition input lock bloqueia sem full Input System hard dependency no core.
- Sem global discovery de PlayerInput no core.
