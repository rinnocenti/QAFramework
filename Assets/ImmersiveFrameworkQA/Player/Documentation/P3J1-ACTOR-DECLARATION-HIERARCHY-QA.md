# P3J.1 — Actor Declaration Hierarchy QA

Run outside Play Mode:

```text
Immersive Framework/QA/Player/P3J.1 Run Actor Declaration Hierarchy Smoke
```

Expected:

```text
[P3J1_ACTOR_DECLARATION_HIERARCHY_SMOKE] status='Passed' cases='14'
```

The smoke proves:

- `PlayerActorDeclaration : ActorDeclaration`;
- base extensibility and final Player specialization;
- generic descriptor preservation;
- same-object PlayerInput compatibility for P3G;
- one Actor identity component and one `IActor` authority;
- fixed Player classification through base descriptor virtual dispatch;
- specialized Player descriptor preservation;
- inherited serialization shape;
- ActorProfile-style declaration discovery compatibility;
- descriptor creation is non-mutating.

P3J.1 does not test Local Player Host composition or Logical Actor materialization. Those begin in P3J.2.
