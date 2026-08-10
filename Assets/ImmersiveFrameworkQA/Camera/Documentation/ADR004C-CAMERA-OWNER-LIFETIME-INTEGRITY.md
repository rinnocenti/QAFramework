# IF-ADR-004C — Camera Owner Lifetime Integrity

## Status

IMPLEMENTED — READY FOR MANUAL/ONE-BUTTON RETEST.

Triggered by ADR-004B case 16 on 2026-08-10.

## Trigger evidence

Canonical C9R composition and public Camera APIs reproduced an admitted Route
request surviving abnormal owner disable:

```text
[QA_CAMERA_ADR004B]
case='16-abnormal-owner-loss'
operation='DisableRouteOwner'
request='qa.camera.request.c9r.route'
owner='Route'
lifetime='Route'
output='camera.output.main'
admittedBefore='2'
admittedAfter='2'
orphan='True'
```

Normal Activity and Route lifecycle exits remained valid. ADR-004B therefore
isolated an abnormal Unity component-lifetime gap rather than a Route/Activity
lifecycle-composition failure.

## Ownership decision

The source comparison establishes two different lifetimes:

```text
logical owner lifetime
  Route    -> IRouteContentLifecycleReceiver enter/exit
  Activity -> IActivityContentLifecycleReceiver enter/exit
  Session  -> SessionCameraOverrideBinding enable/disable

publication/component lifetime
  ScopedCameraOverrideBinding -> publisher + overrideActive
```

`RouteCameraOverrideBinding` and `ActivityCameraOverrideBinding` must not treat a
temporary component disable as a synthetic Route/Activity exit. Their logical
owner can still be active after the component is re-enabled.

The correction therefore belongs in the existing scoped publication owner:

```text
ScopedCameraOverrideBinding.OnDisable
  -> release owned publication only

ScopedCameraOverrideBinding.OnDestroy
  -> final idempotent release safety net
```

These hooks do **not** set `ownerActive = false` and do not re-publish.

`SessionCameraOverrideBinding` remains different because the component itself is
the Session owner. It overrides both hooks and uses `EndOwnerScope(...)`.

No new service, manager, context, runtime host, lookup, fallback or lifecycle
orchestrator is introduced.

## Package correction

Files:

```text
Runtime/Camera/Bindings/ScopedCameraOverrideBinding.cs
Runtime/Camera/Bindings/SessionCameraOverrideBinding.cs
```

Behavior:

1. abnormal disable releases an admitted scoped request;
2. abnormal destruction has a final idempotent release path;
3. normal Route/Activity exit remains authoritative for logical owner end;
4. repeated cleanup is preserved/idempotent;
5. re-enable never silently publishes;
6. Route/Activity may explicitly publish again while their already-entered
   logical owner remains valid;
7. Session re-enable re-establishes Session owner availability through its
   existing `OnEnable`, but still does not auto-publish.

## QA reuse

No new fixture or setup is created.

The canonical C9R fixture remains the owner of real lifecycle composition and
now records additional ADR-004C evidence without changing its canonical 11-case
count.

The new Editor regression only consumes that evidence in the same Play Mode
session:

```text
Immersive Framework/QA/Regressions/Camera/
Run ADR-004C Owner Lifetime Integrity Certification
```

## Certification matrix

After source investigation the executable matrix is fixed at 10 cases:

```text
01 Activity normal exit releases owned request
02 Route normal exit releases owned request
03 Session disable releases request and re-enable remains explicit-only
04 Route abnormal disable releases request
05 Activity abnormal disable releases request without ending logical owner
06 Activity destruction releases request through shared scoped lifetime hook
07 non-winning Activity loss removes only Activity request and preserves Route
08 winning Activity loss restores next valid Player request
09 disable cleanup followed by explicit release is idempotent/Preserved
10 Route/Activity/Session re-enable never silently re-publishes
```

Route abnormal disable and Activity abnormal destruction exercise the same
inherited scoped publication hooks from two concrete lifecycle owners. Normal
Route exit remains independently covered by C9R, so the QA does not create a
second destructive Route fixture solely to duplicate the inherited callback.

## Timeline

```text
C9R starts with canonical Player winner
  -> normal precedence/restore/duplicate cases
  -> Activity disable probe
  -> non-winner Activity disable probe under Route winner
  -> Session disable probe
  -> normal Activity exit
  -> Activity re-enter
  -> destroy active Activity binding
  -> clear Activity
  -> Route abnormal disable probe (ADR-004B case 16)
  -> normal Route exit
  -> C9R 11/11 summary
  -> run ADR-004C certification in same Play Mode
  -> rerun ADR-004B certification in same Play Mode
```

The probes record evidence and perform explicit fallback cleanup only when a
negative invariant fails, so the canonical C9R lifecycle can still reach its
normal Route-exit proof and report the first causal divergence separately.

## Retest gate

Expected success:

```text
[CAMERA_RUNTIME_HOST_INTEGRATION_REGRESSION]
status='Passed'
cases='11'

[QA_CAMERA_ADR004C]
status='Passed'
cases='10/10'
failed='0'
verdict='ADR-004C CAMERA OWNER LIFETIME INTEGRITY CERTIFIED'

[QA_CAMERA_ADR004B]
status='Passed'
cases='18/18'
failed='0'
blocked='0'
verdict='ADR-004B CAMERA NEGATIVE INTEGRITY CERTIFIED'
```

If ADR-004C passes but ADR-004B case 16 still fails, the package correction is
not accepted. If C9R normal lifecycle regresses, the package correction is also
not accepted.
