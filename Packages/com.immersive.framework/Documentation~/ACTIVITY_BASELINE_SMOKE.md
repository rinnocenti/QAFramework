# Activity Baseline Smoke

Status: CLOSED / COMPILE-SMOKE PASS  
Fase: F4  
Corte: F4F  
Roadmap: IF-FW-ROAD-4F — Activity smoke

---

## Contexto

F4A-F4E estabilizaram o baseline mínimo de Activity:

```text
ActivityRuntimeState
ActivityContentSet
ActivityContentLifecycleResult
ActivityReadinessState
Activity Local Visibility Adapter
```

O `Standard Smoke` já exercitava troca de Route, troca de Activity e clear, mas não validava explicitamente que o resultado de Activity carregava ContentSet e readiness coerentes.

## Decisão

F4F adiciona um smoke dedicado no QA:

```text
Run Activity Baseline Smoke
```

Esse smoke valida a sequência:

```text
1. validar/preparar Primary Activity como baseline inicial sem gerar warning quando ela já estiver ativa
2. trocar para Secondary Activity
3. validar ActivityContentSet com handle local
4. validar ActivityReadinessState Ready/BaselineReady
5. voltar para Primary Activity
6. validar saída do conteúdo local anterior sem falha
7. limpar Activity
8. validar ActivityState None e readiness None/NoActiveActivity
9. restaurar Primary Activity
```

## Critérios de validação

O smoke especializado registra steps explícitos:

```text
QA Activity Baseline Smoke step completed. step='secondary'
QA Activity Baseline Smoke step completed. step='primary'
QA Activity Baseline Smoke step completed. step='clear'
QA Activity Baseline Smoke step completed. step='restore-primary'
QA Smoke completed. name='Activity Baseline Smoke'
```

E valida internamente:

```text
activityReadiness='Ready'
activityReadinessIssues='0'
activityContentHandles='1' no step secondary
activityContentEnterFailed='0'
activityContentExitFailed='0'
activityState='None' no step clear
activityReadiness='None' no step clear
```

## Não entra

```text
ActivityContentProfile loading
canonical Activity materialization
actor readiness
input readiness
camera readiness
async gates
release/unload
snapshot/reset
Surface
RuntimeMaterialization
LocalContributionSet
```


## Evidência de fechamento

F4F foi fechado com smoke final contendo:

```text
QA Smoke completed. name='Activity Baseline Smoke'
QA Activity Baseline Smoke step completed. step='secondary'
QA Activity Baseline Smoke step completed. step='primary'
QA Activity Baseline Smoke step completed. step='clear'
QA Activity Baseline Smoke step completed. step='restore-primary'
QA Smoke completed. name='Standard Smoke'
QA Smoke completed. name='Route Callback Smoke'
QA Authoring Validation completed. scope='Loaded Route Content' bindings='1' issues='0'
```

O fechamento de F4 também ajusta o smoke para não emitir request redundante da Primary Activity quando ela já é a Activity ativa.


## F4G hygiene note

F4G adjusts the smoke setup step so it does not issue a redundant Primary Activity request when Primary is already active. This removes the non-fatal happy-path warning observed in the final F4F smoke.
