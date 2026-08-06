# QA-HARDENING-01 — Causal Boundary Migration Map

QA-03 não possui mais latch composto de Transition + Loading. O problema ativo era uma evidência passiva válida seguida por projeção cacheada; HARDENING-01 migra somente QA-03 e sua fixture.

Achado adicional: **foundation terminal observation contract** — corrigido em **QA-HARDENING-01-FIX1**. A reobservação terminal não altera fase, não repete completion callback e preserva o terminal original.

Achado adicional: **WaitCovered compound event predicate**; classificação: lost causal join. Corrigido em **QA-HARDENING-01-FIX2** com sinais Visible e Preparing independentes.

A API tipada expõe `FailedCommittedTargetReadinessCancelled`, mas não expõe publicamente o motivo específico de disposal; FIX2 registra o resultado integral sem fazer parsing textual.

| Arquivo / linhas | Família | Boundary atual | Classificação | Ação | Guard |
|---|---|---|---|---|---|
| QaDirectActivityReadinessPoliciesRegression.cs:124 | QA-03 | baseline antes de criar observer | Unity destruction/initial presentation propagation | retain with reason | estado inicial do host |
| QaDirectActivityReadinessPoliciesRegression.cs:176 | QA-03 | confirmação de Destroy(observerRoot) | Unity destruction propagation | retain with reason | root ausente na scene |
| QaDirectActivityReadinessPoliciesRegression.cs:310-470 | QA-03 | request → observer typed entry → readiness → public result | causal | fixed in HARDENING-01 | foundation + QA-03 |
| QaDirectActivityReadinessPoliciesRegression.cs:760-810 | QA-03 | Loading Show + Update* + Hide | causal / variable grammar | fixed in HARDENING-01 | foundation grammar cases |
| QaActivityEntryReadinessFixture.cs:496-513 | QA-01/03 fixture | disposal após operação owned terminal | pending-operation risk | fixed in HARDENING-01 para QA-03; QA-01 retain with reason | fixture terminal precondition |
| QaActivityEntryReadinessFoundationRegression.cs:55,104,137,163 | QA-01 | request/readiness/destruction | causal e Unity destruction propagation | migrate in HARDENING-02 | shared operation/registry |
| QaActivityEntryPresentationEvidenceRegression.cs:128,144,235 | QA-02 | adapter passive sample/frame | passive sample / arbitrary synchronization | migrate in HARDENING-02 | causal checkpoints |
| Demais TaskCompletionSource, Task.WhenAny, Awaitable.NextFrameAsync, async void, route/activity/pause/player/camera requests | demais famílias | específico de cada runner | pending-operation risk ou arbitrary synchronization | migrate in HARDENING-02 | catalogação e operação owned por família |
| Demais IsVisible, CurrentAlpha, LastStatus | todas as famílias | projeção/cache/amostra | cached projection ou passive sample | migrate in HARDENING-02 | boundary tipado/resultados públicos |

Pesquisa realizada para TaskCompletionSource, Task.WhenAny, Awaitable.NextFrameAsync, async void, RequestActivityAsync, RequestRouteAsync, DisposeAsync, IsVisible, CurrentAlpha e LastStatus. HARDENING-01 não altera QA-01, QA-02, Player, Pause, Camera, Reset, Restart, Route ou outras superfícies.

Os únicos NextFrameAsync restantes em QA-03 são: propagação do estado inicial antes de iniciar política e confirmação da destruição do observer. Nenhum é usado para ordenar observer, adapter, Loading ou request.
