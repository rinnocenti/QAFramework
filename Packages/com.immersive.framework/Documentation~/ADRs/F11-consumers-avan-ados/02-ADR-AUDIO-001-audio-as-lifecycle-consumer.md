# ADR-AUDIO-001 — Audio as Lifecycle Consumer

Status: Draft / Deferred  
Fase: F11  
Tipo: Consumer / Audio  
Escopo: Audio

---

## Contexto

Audio no `NewScripts` consome Route lifecycle, runtime config e pooling. Deve ser port/adapter, não owner de lifecycle.

## Decisão

Audio entra como consumer:

- Route/Activity lifecycle context dispara audio requests.
- Backend/engine audio fica em adapter.
- Global services não são descobertos por service locator público.
- SFX runtime/pooling só depois de RuntimeContentHandle/Pooling.

## Consequências

### Positivas

- Evita audio capturar route pipeline.
- Permite package/adapters.
- Mantém core sem dependência de audio concreto.

### Negativas / trade-offs

- Audio concreto chega tarde.
- Requer port bem definido.

## Fora do escopo

- Mixer final.
- Spatial audio completo.
- Audio pooling.

## Critérios de validação

- Route enter dispara audio request por port.
- Adapter pode ser substituído.
- Sem global service locator.

## Impacto esperado

Consumer avançado.

## Relação com roadmap

F11.

## Notas de implementação

SFX/pooling podem vir depois de Pooling ADR.
