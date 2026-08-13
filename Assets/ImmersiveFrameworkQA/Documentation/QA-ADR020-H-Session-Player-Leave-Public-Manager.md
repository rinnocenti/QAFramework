# QA-ADR020-H — Session Player Leave Public / Manager-Provisioned

**Date:** 2026-08-13  
**ADR:** IF-ADR-020  
**Runner:** `Immersive Framework/QA/Regressions/Player/Run ADR020-H Session Player Leave Public Manager Regression`

## Objective

Prove the first behavioral closure of Session Player Leave through the real scoped consumer surface introduced by ADR020-G, using the existing authored Player Surface fixture and a Manager-Provisioned Player.

The Player contract path under test is public only:

```text
P1 scoped consumer access
-> Open Joining
-> Request Join
-> public default Actor selection
-> normal Activity preparation/materialization/gameplay admission
-> Close Joining
-> Request Leave (exact Slot + occurrence revision)
-> P2 public observation
```

QA helpers are arrangement only. The runner does not call Leave internals, Slot mutation, Actor preparation, gameplay admission, Host release or reconcile APIs.

## Required proof

The runner asserts:

- Leave succeeds while Joining is Closed.
- Leave does not reopen Joining.
- exact joined occurrence is used as the Leave target.
- Manager-Provisioned Activity representation releases before terminal completion.
- Manager-Provisioned Session Host authority is released.
- Slot reaches `Available` only after the successful terminal result.
- Session-scoped Actor selection is cleared.
- a required current Activity does not retain stale Ready/Player representation evidence.
- RequestJoin remains rejected while Joining is Closed.
- reopening Joining allows the same Slot to be reused as a newer occurrence.
- replaying Leave A after Join B is rejected as stale and does not affect B.
- a joined Player with no current Activity can Leave without a fake Activity representation.

## How to run

1. Exit Play Mode.
2. Run the existing Player Surface preparation menu used by Q1/Q2.
3. Enter a fresh Play Mode with no joined Players. The normal startup Activity may already be active; the QA requires no stale Player Host or Activity-representation authority.
4. Run the ADR020-H menu item.
5. Expect:

```text
[QA_ADR020_H_LEAVE] status='Passed' verdict='ADR020_H_PASS'
```

A failure at `activity-stale-ready-cleared` is a package-level ADR-020 readiness/reconcile gap, not a QA bypass opportunity.
The initial setup deliberately distinguishes GameFlow Activity authority from Player
Activity representation authority. A current startup Activity is valid before the first
Join; what must be absent is a previously joined Player occurrence and any stale
Player-owned Host/Activity representation evidence.

## Deliberate scope boundary

ADR020-H covers the public Manager-Provisioned path and occurrence safety. Scene-Provided physical ownership and induced partial-release/retry failures require a Session configured with Scene-Provided provisioning and are intentionally isolated into the next QA cut rather than mutating the shared Manager fixture.
