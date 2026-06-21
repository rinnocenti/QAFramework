# F1B Closure — API Status Convention

Status: Closed / Compile-Smoke Pass  
Fase: F1  
Corte: F1B  
Tipo: Foundation / API Governance  

---

## Resultado

```text
F1B — CLOSED / COMPILE-SMOKE PASS
```

O corte F1B introduziu a convenção mínima de status de API e aplicou marcadores nas superfícies públicas/semi-públicas existentes.

## Evidência de smoke

Smoke enviado após aplicação do F1B:

```text
Boot succeeded.
QA Smoke completed. name='Route Smoke'.
QA Smoke completed. name='Activity Smoke'.
QA Smoke completed. name='Clear Activity Smoke'.
```

Não houve erro CS, falha explícita de smoke, FATAL ou Exception no log enviado.

## O que o F1B fechou

- `FrameworkApiStatus` criado.
- `FrameworkApiStatusAttribute` criado.
- Status aplicado às superfícies atuais.
- `API_STATUS_CONVENTION.md` criado.
- O baseline continuou compilando e passando smoke.

## O que o F1B não fechou

- `FrameworkFact`.
- Semântica real de `ValidationMode`.
- Primitivos tipados de identity.
- Revisão final de `FrameworkContentHandle`.
- Content identity final.

Esses itens permanecem dentro da F1 e devem ser tratados por cortes posteriores.

## Próximo corte autorizado

```text
F1C — FrameworkFact minimal model
```
