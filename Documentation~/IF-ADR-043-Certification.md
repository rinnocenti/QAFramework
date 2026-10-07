# IF-ADR-043 — Physical Camera Participation Certification

**Status: CERTIFIED / PASS — Play Mode 7/7 on 2026-10-07.**

This certification proves the focused IF-ADR-043 physical-participation contract for an
`IndividualPerPlayer` Camera Assignment with two Manager-Provisioned local Players.

## Scope

The scenario executes the real GameApplication, Player Session, public Join/Leave APIs,
two persistent Camera Outputs and one startup `IndividualPerPlayer` Assignment.

It certifies:

- zero Players -> exactly one deterministic physical fallback Output;
- P1 -> only P1's mapped Output physically participates;
- P1 + P2 -> both mapped Outputs physically participate;
- P1 leaves -> only P2's Output remains physically participating;
- P2 leaves -> exactly one deterministic physical fallback Output;
- P1 rejoin -> exact P1 Output restored;
- P2 rejoin -> both Outputs restored;
- `PlayerInput.camera` matches each joined Slot's mapped Output;
- authored Assignment and Output identities remain stable;
- no observed frame has zero physically participating QA Outputs;
- cleanup uses public Leave requests and restores the zero-Player baseline.

Continuous coverage is observed directly from the two registered Output Cameras using
`Camera.enabled && gameObject.activeInHierarchy`. Render-pipeline callbacks are not
used as a proxy for physical participation.

The QA-owned virtual keyboards exist only to make Input System pairing deterministic.
They are not part of the Camera contract and are removed after public Leaves complete.

## Ordered cases

```text
0
-> P1
-> P1 + P2
-> P2
-> 0
-> P1
-> P1 + P2
```

## Executed certification — 2026-10-07

```text
[IF-ADR-043] status='Passed' verdict='PASS' cases='7/7' next='<none>' completed='7' missing='<none>' assignment='0303ca8e5a7b44758de8f7632f8f051b' framesObserved='10' framesWithoutCamera='0' firstFrameWithoutCamera='-1' execution='' unwind='public-leave-requests' cleanup='BaselineRestored' cleanupIssue=''.
```

Accepted evidence:

- 7/7 ordered checkpoints passed;
- 10 monitored frames contained at least one physically participating QA Output;
- no zero-camera frame was observed;
- no functional divergence was reported;
- public cleanup restored the zero-Player baseline;
- `cleanupIssue` is empty.

This closes the focused QAFramework validation gate for IF-ADR-043. Broader IF-ADR-038
recertification remains separate, including SharedGroup and transactional Assignment
failure-path coverage.
