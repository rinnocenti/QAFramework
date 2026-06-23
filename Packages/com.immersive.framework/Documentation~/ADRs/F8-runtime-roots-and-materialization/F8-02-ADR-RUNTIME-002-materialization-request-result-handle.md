# F8-02 — ADR-RUNTIME-002 — Materialization Request Result Handle

Status: Accepted in F8A / Implementation pending  
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
→ IFrameworkContentMaterializer.Materialize
→ RuntimeMaterializationResult
→ RuntimeContentHandle
```

O materializer concreto inicial deve ser simples e local, como `PrefabContentMaterializer`.

`RuntimeContentHandle` é a referência canônica da instância criada. Ele deve carregar:

```text
identity
owner scope
state
resource name/path diagnostic only
release action/policy
release result/diagnostics
```

## Estados mínimos esperados

O handle deve distinguir pelo menos:

```text
Active
Released
ReleaseFailed
Invalid
```

Double-release deve ser seguro e diagnosticado. Ele não deve destruir duas vezes.

## Separação de F8 e F9

F8 cria conteúdo runtime com owner/lifetime/release.

F9 conecta runtime content a `ContentAnchorRoot`, `ContentAnchorSlot` ou `ContentAnchorPoint`.

Portanto, F8 não deve criar `RuntimeContentAnchorBinding`.

## Consequências

### Positivas

- Cria API testável.
- Desacopla prefab spawn de consumers.
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

- Prefab materializa e retorna `RuntimeContentHandle`.
- Handle tem identity e owner scope estáveis.
- Release muda estado e limpa o objeto criado.
- Double-release é seguro/diagnosticado.
- Route/Activity scope release não deixa orphan.
- Materializer não usa `GameObject.Find`.

## Relação com roadmap

F8A aceita a decisão e mantém implementação para cortes F8B+.

## Notas de implementação

A primeira implementação deve ser menor que o modelo final. Prefira um materializer local, explícito e diagnosticável antes de integrar Content Anchors ou consumers.
