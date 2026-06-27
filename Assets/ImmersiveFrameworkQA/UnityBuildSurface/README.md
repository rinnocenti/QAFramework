# Unity Build Surface QA Workspace

This workspace isolates QA assets for the Unity-facing phase of the Immersive Framework.

It is not product content.

## Purpose

Use this area to test Unity Build Surfaces without polluting the baseline Framework QA scenes.

Examples:

- Transition surfaces
- Loading surfaces
- Pause surfaces
- Save moment authoring
- Preferences authoring

## Current QA workspace

### F24A3

Created the workspace folders:

- `Scenes/`
- `ScriptableObjects/`
- `Prefabs/`
- `Materials/`
- `Sprites/`

### F24A4

Added the editor command:

`Immersive Framework > QA > Unity Build Surface > Create QA Scene`

This creates/selects:

`Assets/ImmersiveFrameworkQA/UnityBuildSurface/Scenes/UnityBuildSurfaceQA.unity`

### F24A6

Added the editor command:

`Immersive Framework > QA > Unity Build Surface > Create Transition QA Routes and Scenes`

This creates/repairs:

- `Scenes/TransitionRouteA.unity`
- `Scenes/TransitionRouteB.unity`
- `Routes/QA_TransitionRouteA.asset`
- `Routes/QA_TransitionRouteB.asset`
- `Activities/QA_TransitionActivityA.asset`
- `Activities/QA_TransitionActivityB.asset`

The generated transition scenes are also added/enabled in Build Settings by the editor tool.

## Rules

- Keep this workspace isolated from `Assets/ImmersiveFrameworkQA/Scenes` baseline smokes.
- Do not place product-specific content here.
- Do not place reusable framework runtime code here.
- Use this area for QA assets, scenes and temporary authoring fixtures only.
- Transition/Loading/Pause visuals can be tested here before becoming framework-level surfaces.

## Manual validation

1. Open Unity and wait for import/compile.
2. Run `Immersive Framework > QA > Unity Build Surface > Create Transition QA Routes and Scenes`.
3. Confirm the scenes, routes and activities were created under this workspace.
4. Confirm `TransitionRouteA.unity` and `TransitionRouteB.unity` are in Build Settings.
5. Run the command again and confirm it is idempotent.
