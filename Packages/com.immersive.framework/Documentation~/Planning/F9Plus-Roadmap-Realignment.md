# F9+ Roadmap Realignment

Status: `APPLIED / DOCS ONLY`
Corte: `IF-FW-F9PLUS-REALIGNMENT`
Tipo: Roadmap / ADR alignment
Escopo: fases F9 em diante

---

## Decisão

A partir da consolidação pós-F8H/F8I, o roadmap foi reestruturado de F9 em diante para incorporar capacidades preservadas do `NewScripts` que estavam ausentes ou sub-representadas.

A decisão principal é:

```text
F9 continua sendo Content Anchor binding/runtime placement.
F10 passa a cobrir Transition, Loading e Activity Content Execution.
F11 passa a cobrir Participation, Live Capability Inventory e Local Lifecycle Participants.
F12 passa a cobrir Input, Snapshot/Save e Pause.
F13 passa a cobrir Camera, Audio, Actor, Pooling e presentation adapters.
F14 passa a cobrir Gameplay capabilities.
F15/FX passa a cobrir productization, tooling e hardening.
```

---

## Motivo

O roadmap anterior separou corretamente core e consumers, mas deixou algumas capacidades do `NewScripts` sem fase explícita:

```text
Scene Transition / Loading Presentation
Transition policy: fade, curtain, loading, progress, input lock
ActivityContentProfile execution
Activity reset
Participation boundary
Live capability inventory
Local reset/release/snapshot participants
Session persistent content
Save progression/migration
Editor simulation/visualizer/pre-build validation
Asset provider/Addressables/DLC boundary
Domain reload resilience
```

O erro de planejamento foi tratar transition/loading como presentation pura. A parte visual é presentation, mas a política de transição, progresso e bloqueio de input pertence ao framework de lifecycle.

---

## O que não muda

F8 foi fechado após o Runtime Content Smoke de F8K. F9+ pode iniciar porque request, guard e release-policy já provaram o caminho lógico sem stale scope ou orphan.

A primeira implementação de F9 deve permanecer contratual: binding request/result/handle, sem placement físico e sem consumer final.

---

## Decisões de escopo

### Transition/loading

```text
F6 = scene composition/release técnico.
F10 = transition/loading policy, progress and input lock.
F13 = visual presentation adapters, se necessários.
```

### Activity content

```text
F4 = Activity content/readiness baseline local.
F10 = ActivityContentProfile execution e Activity-owned content.
```

### Activity reset

```text
F10 = reset boundary/plan.
F11 = local reset participants.
F12 = snapshot-backed reset/restore, quando aplicável.
```

### Participation

```text
F11 cria Participation Boundary antes de Input/Actor/Camera/Save consumirem participação.
```

### Capability runtime

```text
LocalContributionSet continua snapshot/diagnóstico.
Live Capability Inventory entra em F11 com RuntimeCapabilityReference e stale/foreign rejection.
```

### Productization

```text
SettingsProvider, assembly split, build stripping, versioning, editor visualizer, Addressables/DLC e domain reload são FX/productization.
Eles são importantes, mas não bloqueiam F8/F9.
```

---

## Nova sequência F9+

```text
F9   Content Anchor binding/runtime placement
F10  Transition, loading and Activity content execution
F11  Participation, live capability inventory and local lifecycle participants
F12  Input, Snapshot/Save and Pause
F13  Advanced consumers: Camera, Audio, Actor, Pooling, transition presentation adapters
F14  Gameplay capabilities
F15/FX Productization, tooling and hardening
```

---

## Não objetivos deste corte

O realinhamento original foi documental. A implementação começou em F9A com contratos de binding, avançou em F9B com `RuntimeContentAnchorBinding` lógico, F9C adicionou e validou o smoke dedicado de binding no QA Canvas, F9D adicionou lifecycle cleanup/snapshots locais para bindings, e F9E torna o binding runtime owned pelo `FrameworkRuntimeHost` por API interna controlada.

Depois de F9E, ainda não existe:

```text
automatic Route/Activity binding cleanup integration
physical placement
TransitionRuntime
ActivityContentProfile execution
Participation runtime
Input/Save/Pause
Camera/Audio/Actor/Pooling
Gameplay capabilities
SettingsProvider
Addressables
Editor visualizer
```

---

## Resultado esperado

Roadmap, matriz e ADRs deixam de apontar F10/F11/F12 antigos como fases finais.

Os ADRs antigos de F10/F11 passam a ser superseded/renumbered e os novos ADRs F10/F11/F12/F13 passam a expressar a sequência atual.
