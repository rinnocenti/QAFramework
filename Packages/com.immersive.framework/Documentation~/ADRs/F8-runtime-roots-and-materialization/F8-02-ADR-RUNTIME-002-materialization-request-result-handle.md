# F8-02 — ADR-RUNTIME-002 — Materialization Request Result Handle

Status: Accepted in F8A / Request-result contracts applied in F8G / Adapter boundary applied in F8I / Release pending  
Fase: F8  
Ordem no Plano: F8-02  
Tipo: RuntimeSpawned / Content  
Escopo: Materialization

---

## Contexto

O framework já tem scene composition, scene release, local contribution e Content Anchors. Ainda falta o contrato para criar instâncias runtime com ownership, diagnostics e release.

Sem request/result/handle, cada consumer futuro tenderia a instanciar prefab diretamente e espalhar cleanup.

## Decisão

Materialização runtime deve seguir um fluxo explícito:

```text
RuntimeMaterializationRequest
→ IRuntimeMaterializationAdapter.Materialize
→ RuntimeMaterializationResult
→ RuntimeContentHandle
```

`IRuntimeMaterializationAdapter` é a boundary canônica. Implementações físicas de prefab, cena, Addressables, pool ou gameplay vivem fora do RuntimeContent core; o core define apenas request/result/handle/guard/release lógico.

`RuntimeContentHandle` é a referência canônica da instância criada. Ele deve carregar:

```text
identity
owner scope
state
resource name/path diagnostic only
release state
release result/diagnostics
```

## Estados mínimos esperados

O handle deve distinguir pelo menos:

```text
Declared
Materialized
ReleaseRequested
Released
ReleaseFailed
```

Double-release deve ser seguro e diagnosticado. Ele não deve destruir duas vezes.

## Separação de F8 e F9

F8 cria conteúdo runtime com owner/lifetime/release.

F9 conecta runtime content a `ContentAnchorRoot`, `ContentAnchorSlot` ou `ContentAnchorPoint`.

Portanto, F8 não deve criar `RuntimeContentAnchorBinding`.

## Consequências

### Positivas

- Cria API testável.
- Desacopla materialização física de consumers.
- Permite release por scope sem código específico de Actor/Pause/Camera.
- Prepara Content Anchor binding em F9 sem capturar F8.

### Trade-offs

- Exige desenhar release callback com cuidado.
- Pode exigir evolução do estado runtime antes de consumers.
- Um materializer genérico demais pode virar service locator se não for contido.

## Fora do escopo

- Pooled materializer.
- Actor/projectile materialization.
- Pause/Camera/UI consumers.
- Save/snapshot de runtime content.
- Runtime Content Anchor binding.
- Activity Content Anchor.

## Critérios de validação futuros

- Adapter físico externo produz resultado/handle sem capturar ownership do core.
- Handle tem identity e owner scope estáveis.
- Release muda estado no core; cleanup físico pertence a adapter explícito ou consumer fora do core.
- Double-release é seguro/diagnosticado.
- Route/Activity scope release não deixa orphan.
- Materializer não usa `GameObject.Find`.

## Relação com roadmap

F8A aceita a decisão. F8B-F8F applied ownership, handle, logical root, runtime owner/context and lifecycle root integration. F8G applies request/result/resource/status contracts. F8H applies transition guard/scoped cancellation. F8I applies `IRuntimeMaterializationAdapter` as boundary. Concrete physical adapters and release execution remain pending.

## Notas de implementação

A primeira implementação física deve ficar fora do core. No core, mantenha somente a boundary explícita e diagnosticável antes de integrar Content Anchors ou consumers.
