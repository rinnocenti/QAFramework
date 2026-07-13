# Immersive Framework QA

Root local de provas técnicas sintéticas do framework. Não contém FIRSTGAME nem documentação arquitetural canônica.

## Superfícies atuais

- `Hub/`: navegação das superfícies de cena canônicas.
- `Lifecycle/`: Application e Scene Lifetime.
- `UnityBuildSurface/`: superfícies Unity de transição e UI global.
- `Camera/`: C9R Camera Override Authority e C9M Follow Pipeline.
- `Pooling/` e `Audio/`: contratos técnicos próprios.
- `Player/Editor/`: P3B PlayerComposer e P3C Player Profile Authoring.
- `Player/SlotsProfiles/` e `Player/Templates/`: assets persistentes preparados para P3D.

Os menus P3B e P3C são smokes editor-only. Os demais domínios são acessados pelo Hub. A consolidação está documentada em `Documentation/QA-CLEANUP-1-DESTRUCTIVE-TEST-CONSOLIDATION.md`.
