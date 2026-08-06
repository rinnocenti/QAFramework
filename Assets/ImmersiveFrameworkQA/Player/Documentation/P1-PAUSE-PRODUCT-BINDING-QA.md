# P1 Pause Product Binding QA

## Estado da entrega

O commit inicial do P1 adicionou apenas esta documentação. A composição consumer-like não estava serializada.

Este complemento adiciona um **Setup/Rebuild idempotente** e um **smoke consumer-like** no padrão do QAFramework. O FIRSTGAME não participa.

## Instalação

Copie a pasta `QAFramework/Assets/...` do ZIP para a raiz do `QAFramework`.

## Gerar a cena e os assets

No Unity:

```text
Immersive Framework
  > QA
    > Player
      > Pause P1
        > Setup or Rebuild Consumer Scene
```

O setup:

```text
usa Assets/InputSystem_Actions.inputactions
usa Global/PauseToggle por GUID
cria QA_PauseToggle.inputactionreference.asset
cria QA_PauseProductBinding.unity
cria PlayerInput + PausePlayerInputBinding
cria exatamente um UnityPlayerInputGateAdapter
cria PauseRequestTrigger para a rota de UI
adiciona a cena ao Build Settings
```

A cena não contém Actor, Slot, Provisioning, PlayerInputManager ou Camera.

## Executar o smoke

1. Entre em Play Mode em qualquer cena de QA com o Editor estável.
2. Execute:

```text
Immersive Framework
  > QA
    > Player
      > Pause P1
        > Run Consumer Smoke
```

Resultado esperado:

```text
[QA][PAUSE-P1] PASS.
status='Passed'
cases='14'
```

## Cobertura

```text
binding scene-local admitido
postura inicial Global + Player
ação resolvida pelo GUID no clone PlayerInput.actions
Pause e Resume pelo PauseRequestTrigger
Pause e Resume por Escape
posturas Global + UI e Global + Player
Time.timeScale aplicado e restaurado
release enquanto pausado termina em Running
postura anterior ao bind restaurada
request sem binding rejeitada sem mutação
segundo binding rejeitado sem deslocar o primeiro
rebind não duplica callback
release final
```

## Limite

O smoke usa as implementações reais de:

```text
PausePlayerInputBinding
PauseProductBindingRuntimeContext
PauseProductBindingSceneLifecycleParticipant
PauseRequestTrigger
UnityPlayerInputGateAdapter
PlayerInput
```

A autoridade application-scoped é substituída apenas por uma porta QA pequena, porque o objetivo deste smoke é provar o contrato de binding/input/lifecycle isoladamente. Os smokes existentes continuam responsáveis pelo boot completo, UIGlobal e `FrameworkRuntimeHost`.
