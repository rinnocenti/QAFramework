# F49G — PlayerView Passive QA

This smoke validates passive PlayerView evidence before CameraDirector, Cinemachine priority, ControlBinding or gameplay input integration exists.

## Expected route

`QA PlayerView Passive Route`

## Expected final log

```text
[F49G_PLAYER_VIEW_QA] status='Succeeded'
```

## Covered cases

- PlayerViewBehaviour component references exist.
- Declared snapshot is valid.
- IPlayerView exposure works.
- Active view requires PlayerEntry evidence in ViewBound or Active state.
- Bound view accepts ViewBound PlayerEntry evidence.
- Active view accepts Active PlayerEntry evidence.
- Suspended requires explicit reason.
- Camera and target evidence remain optional.
- Release and rebuild are explicit.
