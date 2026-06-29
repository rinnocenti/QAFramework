# F32 — InputMode Unity PlayerInput Application Plan


## F32F — InputMode Unity PlayerInput Request Application

Status: implemented.

This cut composes the full explicit path from `InputModeRequest` to `PlayerInput` application. It remains bounded to an explicit `PlayerInput` instance and does not own `PlayerInputManager`, join, spawn, movement or runtime host wiring.
