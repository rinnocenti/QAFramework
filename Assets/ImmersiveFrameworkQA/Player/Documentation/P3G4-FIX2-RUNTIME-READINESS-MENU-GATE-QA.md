# P3G.4 FIX2 — Runtime Readiness Menu Gate

## Problem

The real join smoke menu was enabled for the entire Play Mode lifetime. During asynchronous framework boot, the `LocalPlayerProvisioningAuthoring` could already exist in a loaded scene while its Session runtime bridge had not yet been bound.

This allowed the smoke to run between:

```text
Player participation runtime initialized
-> Route/UIGlobal loading still in progress
-> Local Player provisioning runtime not bound yet
```

The resulting failure was a QA timing error, not a provisioning runtime failure.

## Correction

The menu validation now requires all conditions below:

```text
Editor is in Play Mode
exactly one loaded LocalPlayerProvisioningAuthoring exists
LocalPlayerProvisioningAuthoring.RuntimeReady is true
```

The smoke remains synchronous and one-shot. It does not poll, wait for frames, schedule delayed execution or hide boot failures.

## Expected use

Enter Play Mode and wait until framework boot completes. The menu becomes enabled only after:

```text
Local Player provisioning Session runtime initialized. status='Ready'
```

Then run:

```text
Immersive Framework > QA > Player > P3G.4 Run Runtime Host Real Join Smoke
```

Expected result remains:

```text
[P3G4_RUNTIME_HOST_REAL_JOIN_SMOKE] status='Passed' cases='21'
```
