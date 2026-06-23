# F13-02 — ADR-AUDIO-001 — Audio as Lifecycle Consumer

Status: Draft / Renumbered
Fase: F13
Ordem no Plano: F13-02
Tipo: Consumer / Audio
Escopo: Audio

---

## Decisão

Audio recebe lifecycle context e events scoped. Backend/engine audio fica em adapter. AudioListener único deve ser tratado como Session persistent content futuro, não camera-owned.
