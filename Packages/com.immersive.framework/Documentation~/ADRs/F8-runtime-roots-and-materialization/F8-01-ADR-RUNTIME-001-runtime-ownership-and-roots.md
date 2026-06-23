# F8-01 — ADR-RUNTIME-001 — Runtime Ownership and Roots

Status: Accepted in F8A / Implementation pending  
Fase: F8  
Ordem no Plano: F8-01  
Tipo: RuntimeSpawned  
Escopo: Runtime roots

---

## Contexto

F6 resolveu composição/release de cenas de Route. F7 resolveu declaração, discovery e validação de `ContentAnchor`. O framework ainda não possui ownership formal para GameObjects criados em runtime.

Sem essa fronteira, adapters físicos futuros tenderiam a criar roots ad hoc, depender de nomes de GameObject, usar `GameObject.Find` ou misturar ownership de Route/Activity/Session.

## Decisão

F8 deve criar ownership runtime por escopo antes de qualquer adapter físico avançado.

Scopes autorizados:

```text
Session
Route
Activity
Transient
```

`Runtime Root` é o container runtime de um scope. Ele não é `ContentAnchor` e não é conteúdo authored de cena.

`RuntimeRootRegistry` deve existir como registry interno/scoped do runtime atual. Ele não é service locator global e não deve virar API pública genérica para gameplay scripts.

Materializers não devem:

- usar `GameObject.Find`;
- criar roots por nome mágico;
- destruir conteúdo authored de cena;
- procurar Content Anchors diretamente;
- conhecer Actor/Pause/Camera/UI como consumers concretos.

## Semântica inicial dos scopes

| Scope | Lifetime |
|---|---|
| `Session` | Persiste entre Routes dentro da sessão runtime. |
| `Route` | Liberado no Route exit. |
| `Activity` | Liberado no Activity clear/switch/exit futuro. |
| `Transient` | Depende de release explícito do handle/request. |

`Application` não entra como root de conteúdo em F8. O Application Runtime é owner de boot/flow, não container de gameplay content.

## Consequências

### Positivas

- Evita roots órfãos.
- Prepara materializer, runtime handles, release policy e consumers futuros.
- Separa runtime-created content de scene-authored content.
- Impede que `ContentAnchor` vire root global ou spawn system.

### Trade-offs

- Exige estado e lifecycle explícitos.
- Exige cuidado para não criar service locator.
- Pode exigir GameObjects internos persistentes por scope.

## Fora do escopo

- Content Anchor runtime binding.
- Prefab placement por anchor.
- Pooling.
- Actor materialization.
- Camera/Pause/UI consumers.
- Save/snapshot.
- Fallback silencioso para root ausente.

## Critérios de validação futuros

- Root existe para o scope materializado.
- Route exit libera Route root/content.
- Activity exit/clear libera Activity root/content quando Activity runtime release existir.
- Session root não é destruído por troca de Route.
- Nenhum `GameObject.Find` para resolver roots.
- Nenhum conteúdo authored de cena é destruído por runtime root release.

## Relação com roadmap

F8A aceita a decisão e mantém implementação para cortes F8B+.

## Notas de implementação

O primeiro registry deve ser mínimo, interno e diagnosticável. A implementação deve favorecer APIs explícitas e resultados tipados em vez de acesso global.
