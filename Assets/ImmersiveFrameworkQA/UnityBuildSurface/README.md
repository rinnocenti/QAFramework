# Unity Build Surface QA

Workspace isolado para validar os elementos Unity-facing da F24.

Este espaco nao substitui o QA baseline do framework. Ele existe para testar surfaces, assets e authoring voltados a level/game designers sem contaminar `StartupScene`, `SecondScene` ou os smokes canonicos existentes.

## Estrutura

```text
Assets/ImmersiveFrameworkQA/UnityBuildSurface/
  README.md
  Scenes/
  ScriptableObjects/
  Prefabs/
  Materials/
  Sprites/
```

## Uso

Use este workspace para:

- Transition Surface QA;
- Loading Surface QA;
- Pause Surface QA;
- Save Moment Authoring QA;
- Preferences Authoring QA;
- exemplos de Inspector e authoring para designers.

## Regras

- Nao colocar assets finais de jogo aqui.
- Nao criar lifecycle paralelo aqui.
- Nao transformar cenas deste workspace em produto.
- Nao mover QA baseline para ca.
- Se algo for singular do projeto consumidor, mover para `Assets/_Project`.
- Se algo for generico/reutilizavel, avaliar entrada no framework.
