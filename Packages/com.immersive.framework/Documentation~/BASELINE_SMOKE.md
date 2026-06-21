# Immersive Framework — BASELINE_SMOKE

Status: F0B baseline hygiene  
Escopo: smoke manual mínimo do core atual

---

## Objetivo

Validar que a higiene de F0B não quebrou o baseline funcional existente.

Este smoke cobre somente:

```text
boot
route request
activity request
clear activity
QA canvas em Editor/development build
```

Este smoke não valida:

```text
CameraFlow
RouteContentRuntime callbacks
additive Route scenes
SessionContentSet
Surface
RuntimeSpawned
Input
Save
Pause
Audio
Actor
Pooling
Projectile
Damage
Attributes
```

---

## Pré-condições

1. O projeto compila sem erros `CS`.
2. `Project Settings > Immersive Framework` possui `Active Game Application`.
3. O `Game Application` possui `Startup Route`.
4. A `Startup Route` possui `Primary Scene` válida.
5. A cena usada no smoke possui, quando necessário, um `FrameworkQaCanvas` apenas para desenvolvimento.
6. O `FrameworkQaCanvas` está disponível no Editor ou em development build; ele não é API de produto.

---

## Smoke A — Boot

### Passos

```text
1. Abrir a Startup Scene ou uma cena qualquer que force o framework a carregar a Startup Route.
2. Entrar em Play Mode.
```

### Esperado

```text
Boot succeeded.
Application Runtime started.
Game Flow started with Startup Route.
Route Lifecycle started Route.
Scene Lifecycle resolved Primary Scene.
```

### Falha se aparecer

```text
FATAL
Exception
Startup Route is missing
Route Primary Scene is missing
Active Game Application is missing
```

---

## Smoke B — Route switch

### Passos

```text
1. Com Play Mode ativo, solicitar uma Route alternativa via FrameworkQaCanvas ou RouteRequestTrigger.
2. Solicitar retorno para a Route canônica.
```

### Esperado

```text
Route Request completed.
Route Lifecycle switched from Route '<previous>' to Route '<target>'.
Scene Lifecycle resolved Primary Scene '<scene>'.
```

### Observação

Route content callbacks não são critério de F0B. `RouteContentRuntime` está deferred até F3.

---

## Smoke C — Activity switch

### Passos

```text
1. Com uma Route ativa, solicitar Activity secundária.
2. Solicitar Activity primária.
```

### Esperado

```text
Activity Request completed.
Activity Flow switched from Activity '<previous>' to Activity '<target>'.
Activity Content applied ...
```

---

## Smoke D — Clear Activity

### Passos

```text
1. Com uma Activity ativa, solicitar Clear Activity.
2. Solicitar novamente a Activity primária.
3. Solicitar Clear Activity novamente.
```

### Esperado

```text
Activity Request completed.
Activity Flow cleared Activity '<previous>' by request.
```

Ausência de Activity ativa pode gerar request ignorado explícito quando o cenário negativo for intencional.

---

## Critérios de PASS

```text
1. Compile sem erros CS.
2. Boot passa.
3. Route switch passa.
4. Activity switch passa.
5. Clear Activity passa.
6. Sem FATAL.
7. Sem Exception.
8. Sem dependência obrigatória de Cinemachine no package core.
9. Sem uso de CameraFlow no smoke F0B.
```

---

## Critérios de FAIL

```text
1. Qualquer erro CS.
2. Boot falha em setup válido.
3. Route request válido falha.
4. Activity request válido falha.
5. Clear Activity válido falha.
6. CameraFlow/Cinemachine vira requisito para o core compilar.
7. RouteContentRuntime callbacks são tratados como requisito do baseline F0B.
```

---

## Próximo smoke esperado

F1 deve manter este smoke e adicionar validação para:

```text
API status
Typed Identity Policy
FrameworkFact mínimo
ValidationMode semantics
Content identity review
```
