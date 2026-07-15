# P3K.7E Multi-Slot Activity Handoff Group QA

Run in a fresh Play Mode session:

```text
Immersive Framework/QA/Player/P3K.7E Run Multi-Slot Activity Handoff Group Smoke
```

Expected:

```text
[P3K7E_MULTI_SLOT_ACTIVITY_HANDOFF_GROUP_SMOKE]
status='Passed'
cases='45'
```

The smoke uses a synthetic two-Slot GameplayReady Activity and proves ordered
Begin, reverse rollback, P3K.6 re-evaluation, global commit prevalidation,
committed idempotence and retryable post-commit cleanup.

P3K.7D already proves the physical per-Slot cutover in real Play Mode. P3K.7E
focuses on the missing local multiplayer atomicity contract.
