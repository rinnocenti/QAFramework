# IF-ADR-044 — Consumer Gameplay Availability Certification

This focused Play Mode certification reuses the two-Slot Player Session, Slot Profiles, SharedGroup Camera Assignment and Output from IF-ADR-042. It owns its Route, Activity, Activity Content Profile and their scenes so the Activity can require `GameplayReady`; the IF-ADR-042 Activity intentionally requires only `LogicalActorsPrepared` and does not establish the current gameplay input binding needed by this certification. The setup also adds an IF-ADR-044 Host with the canonical `UnityPlayerInputGateAdapter`, a valid `PlayerPauseInput`, and an Actor-local `PlayerGameplayInputReader`. It clones the IF-ADR-042 Input Actions into a QA-044-owned asset and adds `Global/Pause` on Escape, leaving the source fixture unchanged. Actions use only public Player Session access and public provisioning commands. The smoke does not write `PlayerInput` state itself.

## Setup and run

1. Run **Immersive Framework → QA → IF-ADR-042 → Configure SharedGroup Certification** once if its generated assets are missing.
2. Run **Immersive Framework → QA → IF-ADR-044 → Configure Gameplay Availability Certification** twice. Both runs should log `setup='Configured' baseline='Valid'`; the second checks that the generated Input Actions, Player Host, GameplayReady Activity composition, application and scenes update in place. The 042 source assets remain unchanged.
3. Select `GameApplication_QaIfAdr044` in `ImmersiveFrameworkSettings` and enter a fresh Play Mode session.
4. Wait for the single terminal `[IF-ADR-044]` line. Send it with any preceding causal diagnostics for review.

The certification expects 8/8 cases and `cleanup='BaselineRestored'`. `Blocked` means an authored/runtime precondition prevented execution; `Failed` means observed behavior diverged from the contract. This smoke checks the Player's Session membership, `GameplayReady`, Actor identity, Slot revisions, paired devices and shared Camera Assignment/occurrence/Subject membership while the consumer block changes only gameplay input.

## Covered cases

1. Empty two-Slot Session baseline.
2. Public Join for P1, then P2, with gameplay input available.
3. Block P1 while P2 remains available and verify Player, device and Camera Assignment evidence is unchanged.
4. Acquire two independent blocks for P1; release the first and confirm the second still blocks gameplay.
5. Release the final block and confirm the canonical action map and gameplay reader are available again.
6. Leave P1 while blocked, confirm the old occurrence token is rejected, then Rejoin P1 and verify the new occurrence starts unblocked.
7. Public Leave cleanup and removal of QA-owned Keyboards.

## Additional manual Unity validation

The smoke does not claim to certify transition or Pause composition. With this application selected, request Pause/Resume with the Host's Escape action and verify that releasing a consumer block during Pause leaves gameplay blocked until Resume. Use the product's public Route request surface to keep a consumer block held through Route A → B → A and verify that its token still controls only the original Player occurrence; repeat with a Transition gate active. Confirm the block never changes participation, Actor, Slot, paired devices, Camera Assignment, Output or Subject.
