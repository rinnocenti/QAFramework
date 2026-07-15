# P3K.7F — Session Gameplay Runtime Composition QA

Run in a fresh Play Mode session:

```text
Immersive Framework
  > QA
    > Player
      > P3K.7F Run Session Gameplay Runtime Composition Smoke
```

Expected:

```text
[P3K7F_SESSION_GAMEPLAY_RUNTIME_COMPOSITION_SMOKE]
status='Passed'
cases='48'
```

The smoke proves the official `FrameworkRuntimeHost` composition, a real current
P3K.2-P3K.5 chain, one reversible group rollback, one committed group cutover,
and final cleanup through the same host module.
