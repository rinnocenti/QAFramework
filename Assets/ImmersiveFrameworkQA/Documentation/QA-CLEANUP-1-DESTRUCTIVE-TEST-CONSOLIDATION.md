# QA-CLEANUP-1 — Destructive Test Consolidation

## Resumo

O QA foi reduzido removendo provas incrementais absorvidas por superfícies canônicas atuais. Foram removidas as cadeias Player P2A/P2D/P2E e F49–F52, Camera C9C–C9M intermediária, Actors Readiness isolados, assets/cenas/rotas exclusivos, utilitários de setup e documentação histórica associada.

| Métrica | Antes | Depois |
| --- | ---: | ---: |
| Menus de smoke/setup | 48 | 12 |
| Smokes Player canônicos | 2 | 2 |
| Smokes Camera canônicos | 2 | 2 |
| Famílias intermediárias removidas | 3 | 0 |

## Matriz de decisão

| Id / família | Decisão | Contrato preservado / substituição | Justificativa |
| --- | --- | --- | --- |
| P2A-QA0 | DELETE | P3B | Produto PlayerComposer atual, materialização, falha explícita e idempotência agora são provados por P3B. |
| P2D | DELETE | P3B | Baseline de runtime intermediário não protege contrato público distinto. |
| P2E | DELETE | P3B | Gate e mapa de ação final são materializados e validados por P3B. |
| F49B–F50C | DELETE | P3B/P3C | Harnesses de readiness, topologia e authoring de modelo anterior foram absorvidos pelas superfícies finais. |
| F51A–F52C | DELETE | P3B | Adapters de binding e cadeia PlayerInput são implementação removida do produto atual. |
| C9C–C9I | DELETE | C9R | Contratos intermediários de output/publicação foram absorvidos pela prova integrada de autoridade. |
| C9L e C9M teardown anterior | CONSOLIDATE | C9R | C9R cobre precedência, restauração, idempotência e cleanup de Activity/Route. |
| C9R Camera Override Authority | KEEP | próprio | Única prova integrada de arbitration, restoration e teardown de lifecycle. |
| C9M Follow Pipeline | KEEP | próprio | Única prova editor-only de materialização Follow/framing e idempotência. |
| P3B PlayerComposer Minimal Materialization | KEEP | próprio | Contrato atual de materialização, falhas explícitas e remoção de rails antigos. |
| P3C Player Profile Authoring | KEEP | próprio | Contrato atual de perfis, validação não mutante e template authoring necessário para P3D. |
| PlayerSlotProfiles e Participation Requirements Profiles | KEEP | P3D | Assets persistentes protegidos para o próximo corte. |
| Lifecycle, Unity Build Surface, Pooling e Audio | KEEP | próprio | Cada domínio mantém responsabilidade atual distinta. |
| Legacy Lifecycle assets | DELETE | QA_Lifecycle atual | Não possuem consumidor operacional no baseline atual. |

## Arquivos

- Criados: este relatório e `REMOVED-FILES.txt` no pacote de entrega.
- Alterados: construtor do Hub e a nomenclatura dos scripts C9R.
- Removidos: consultar `REMOVED-FILES.txt`.

## QA mantido

| Menu / entrada | Responsabilidade |
| --- | --- |
| `Immersive Framework/QA/Player/P3B Run PlayerComposer Minimal Materialization Smoke` | Materialização Player final e idempotência. |
| `Immersive Framework/QA/Player/P3C Run Player Profile Authoring Smoke` | Authoring de Player Profile e requisitos de participação. |
| `Immersive Framework QA/Camera/C9R Install Camera Override Authority QA` | Preparação explícita da prova integrada C9R. |
| `Immersive Framework QA/Camera/C9M Run Follow Pipeline Smoke` | Follow/framing final. |
| `Immersive Framework QA/Hub/Create or Refresh QA Hub` | Hub curto de Application/Lifecycle, Unity Build Surface, Camera, Pooling e Audio. |

## Ordem manual de execução

1. Abrir o projeto e aguardar compile/import.
2. Executar `Create or Refresh QA Hub` para materializar a cena curta após a limpeza.
3. Executar Model Readiness atual do framework.
4. Executar P3B e P3C.
5. Executar C9R e conferir arbitration, restoration e teardown.
6. Executar C9M Follow Pipeline.
7. Executar Lifecycle, Unity Build Surface, Pooling e Audio pelo Hub.

## Riscos conhecidos

Sem executar Unity não é possível confirmar import, serialização de cena ou logs. C9R permanece dedicado porque lifecycle/arbitration não pode ser substituído com segurança pelo smoke editor-only de Follow.

## Fora de escopo

- Novos testes P3D.
- Alterações em `com.immersive.framework`.
- Alterações em FIRSTGAME.
- Runtime novo ou redesenho do framework.
