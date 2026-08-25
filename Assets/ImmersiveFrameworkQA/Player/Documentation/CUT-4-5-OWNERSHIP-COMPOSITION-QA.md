# Cortes 4 e 5 — QA de ownership e composition root

## Estado auditado

Os dois cortes já existem no package oficial:

```text
Corte 4
Local Player Camera Publication Ownership

Corte 5
Explicit Local Player Provisioning Composition Root
```

Este registro histórico descreve QA técnico focado nos contratos que não estavam
cobertos pela consolidação do QAFramework. O corte atual altera o package e requer
nova execução manual da lane de runtime antes de qualquer nova certificação.

## Corte 4 — Camera publication ownership

### Contrato atual de authoring e runtime

`PlayerGameplayCameraAuthoring` é a única superfície normal de authoring para
participação de um Logical Player em Gameplay Camera. O request não é autorado
como um segundo componente: a lane de gameplay admission o materializa a partir
da intenção, do Player preparado e do output explícito.

O documento de Cut 4 antes descrevia um componente de plumbing e um
Scene Auto-Publisher opcional. Esse contrato foi removido; não há smoke de
authoring separado que deva reinstalá-lo.

### Runtime smoke histórico a reexecutar

Execute em uma sessão nova de Play Mode:

```text
Immersive Framework
  > QA
    > Camera
      > Cut 4 Run Local Player Camera Publication Ownership Runtime Smoke
```

Registro histórico pré-corte:

```text
[CUT4_LOCAL_PLAYER_CAMERA_PUBLICATION_OWNERSHIP_RUNTIME_SMOKE]
status='Passed'
cases='9'
```

Ao ser reexecutado, esse smoke deve reutilizar a lane real `P3K.7H` e provar:

```text
Player real é provisionado e admitido
PlayerGameplayCameraEligibilityRuntimeContext é o publisher canônico, dentro da
lane de Player gameplay admission
existe exatamente um request LocalPlayer no output para o Slot admitido
request e output mantêm identidade explícita
nenhum componente de cena publica em paralelo
release da admissão remove a publicação e o request do output
```

A lane é one-shot. Reentre em Play Mode antes de repetir.

### Fixture C9R preservada

`QaLocalPlayerCameraRequestBinding` permanece somente no `QAFramework` como
fixture sintética da arbitragem genérica de Camera. Ela não pertence ao package,
não possui `AddComponentMenu` e não participa de Player admission, Slot allocation
ou Actor readiness; portanto não é uma superfície alternativa de authoring.

## Corte 5 — Provisioning composition root

Execute em Edit Mode:

```text
Immersive Framework
  > QA
    > Player
      > Cut 5 Run Provisioning Composition Root Smoke
```

Resultado esperado:

```text
[CUT5_PROVISIONING_COMPOSITION_ROOT_SMOKE]
status='Passed'
cases='8'
```

Prova:

```text
authoring global sem Host Registration é ignorado
registro sem referência falha explicitamente
registro inválido bloqueia a composição
um registro resolve exatamente o authoring referenciado
registros duplicados falham
múltiplos roots de UIGlobal preservam uma única autoridade
um authoring não registrado não compete com o registrado
ausência de registro é explicitamente NotConfigured/unavailable
```

## Fora de escopo

```text
não altera o package
não muda cenas persistidas do QA
não cria fallback de discovery
não valida UX do FIRSTGAME
não duplica os smokes de arbitragem C9R
```
