# POST-RESET-B5A — QA Legacy Documentation Setup Reference Cleanup

## Objetivo

Remover referencias ativas que poderiam recriar ou validar `Assets/_Documentation` dentro do Framework QA Project.

## Arquivos auditados

| Arquivo | Classificacao | Resultado |
|---|---|---|
| `Assets/_Project/Scripts/Editor/ImmersiveInitialProjectSetup.cs` | `legacy-doc-reference` / `EditorOnly` | Continha a constante `DocumentationRootFolder` e entradas em `InitialFolders`. Corrigido. |
| `Assets/_Project/Scripts/Editor/Project.Editor.asmdef` | `false-positive` | Assembly Editor-only; sem referencia a `_Documentation`. |
| `Assets/_Project/Scripts/Runtime/Project.Runtime.asmdef` | `false-positive` | Sem referencia a `_Documentation`. |
| `Assets/ImmersiveFrameworkQA/README.md` | `qa-operational-doc-reference` | Mantem policy curta dizendo que `_Documentation` nao deve voltar. |
| `Assets/ImmersiveFrameworkQA/Documentation/POST-RESET-B5-QA-Legacy-Documentation-Removal.md` | `qa-operational-doc-reference` | Relatorio historico de remocao. |
| `Assets/ImmersiveFrameworkQA/Documentation/POST-RESET-B4-QA-Project-Cleanup.md` | `qa-operational-doc-reference` | Relatorio historico anterior ao B5. |

## Referências legadas encontradas

Referencia ativa encontrada:

```text
Assets/_Project/Scripts/Editor/ImmersiveInitialProjectSetup.cs
  DocumentationRootFolder = "Assets/_Documentation"
  InitialFolders includes:
    Assets/_Documentation
    Assets/_Documentation/ADRs
    Assets/_Documentation/Notes
    Assets/_Documentation/Setup
```

Referencias documentais encontradas:

- `Assets/ImmersiveFrameworkQA/README.md`;
- relatorios `POST-RESET-B4`, `POST-RESET-B5` e `POST-RESET-B5A`.

## Mudanças feitas

- Removida a constante `DocumentationRootFolder`.
- Removidas as entradas de `InitialFolders` que criavam `Assets/_Documentation`.
- Mantido o restante do setup sem alteracao.
- Atualizado o relatorio B5 com a secao `Follow-up B5A — Setup Reference Cleanup`.

## Mudanças não feitas

- Nao foi criada documentacao canonica no QA Project.
- Nao foi recriada `Assets/_Documentation`.
- Nao foram alterados runtime do framework, packages, cenas, prefabs, `.asset`, `.mat`, `.controller` ou `.inputactions`.
- Nao foi redirecionado conteudo canonico do framework para `Assets/ImmersiveFrameworkQA/Documentation`.

## Validação textual

Buscas executadas em `C:\Projetos\My project\Assets`:

```text
Assets/_Documentation
_Documentation
Documentation~
ImmersiveInitialProjectSetup
CreateDirectory
Directory.CreateDirectory
AssetDatabase.CreateFolder
```

Resultado:

- nenhuma referencia ativa restante a `Assets/_Documentation` em scripts locais;
- `_Documentation` aparece apenas em relatorios/README historicos explicando a remocao;
- `Documentation~` aparece apenas como referencia documental ao package;
- `AssetDatabase.CreateFolder` permanece somente no helper generico `ProjectFolders.EnsureFolder`, sem alvo `_Documentation`;
- `Directory.CreateDirectory` e `CreateDirectory` nao foram encontrados como API ativa neste projeto.

Unity import, compile, build, playmode, smoke e batchmode nao foram executados.

## Risco restante

Baixo. O setup ainda cria outras pastas legadas do projeto QA (`Assets/_Project`, `Assets/_External`, `Assets/_Sandbox`), mas nao cria nem valida `Assets/_Documentation`.

## Próximo passo

Executar validacao manual de import/compile no Unity e seguir para `POST-RESET-B6 - QA Asset Migration To ImmersiveFrameworkQA` quando for migrar assets serializados de `Assets/_Project`.
