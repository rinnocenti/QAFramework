# F4G — F4 Closure Hygiene

Status: APPLIED / PENDING COMPILE-SMOKE  
Fase: F4  
Corte: F4G  
Roadmap: IF-FW-ROAD-4G — F4 closure hygiene

---

## Contexto

O smoke final do F4F validou o baseline de Activity, mas a auditoria pré-F5 encontrou três ajustes pequenos antes de entrar em Local Contribution:

```text
1. Mensagens/editor/validator ainda usavam Activity Content Binding.
2. Activity Baseline Smoke emitia warning não fatal quando Primary já estava ativa.
3. F4 precisava registrar formalmente a fronteira ActivityContentSet F4 vs LocalContributionSet F5.
```

## Mudanças

- Alinha o Inspector e o validator para `Activity Local Visibility Adapter`.
- Atualiza comentários de API local para o nome novo.
- Ajusta `Run Activity Baseline Smoke` para não requisitar Primary Activity quando ela já é a Activity ativa.
- Registra `F4F_CLOSURE.md` e `F4_CLOSURE.md`.
- Registra que F5 não deve reutilizar nome/path de GameObject ou cena como identidade funcional local.

## Fora do escopo

```text
LocalContributionSet
LocalContentIdentity implementation
ActivityContentProfile loading
canonical Activity materialization
release/unload policy
Surface
RuntimeMaterialization
actors/input/camera/reset/snapshot
```

## Validação requerida

Rodar:

```text
Run Activity Baseline Smoke
Run Standard Smoke
Run Route Callback Smoke
Validate Loaded Route Content
```

Critérios:

```text
QA Smoke completed. name='Activity Baseline Smoke'
QA Smoke completed. name='Standard Smoke'
QA Smoke completed. name='Route Callback Smoke'
QA Authoring Validation completed. scope='Loaded Route Content' bindings='1' issues='0'
Activity Request ignored. source='FrameworkQaCanvas' reason='qa.activity' = ausente no Activity Baseline Smoke
Exception / FATAL / error CS = ausentes
```
