# F2-03 — ADR-SETTINGS-001 — Settings Source Policy

Status: Accepted  
Fase: F2  
Cut de decisão: F2A  
Ordem no Plano: F2-03  
Tipo: Bootstrap / Settings  
Escopo: Project Settings / runtime bootstrap

---

## Contexto

O roadmap F2 inclui `IF-FW-ROAD-2E — Settings source decision`. O package atual carrega `ImmersiveFrameworkSettingsAsset` por `Resources.Load` durante bootstrap e possui tooling de Project Settings para criar/selecionar esse asset.

Essa decisão precisa ficar explícita antes de mexer em Session runtime state, porque a origem da `GameApplicationAsset` ativa participa do boot e da Session inicial.

---

## Decisão

`Resources` é aceito como fonte temporária e explícita de settings para o bootstrap atual.

A fonte canônica de F2 é:

```text
ImmersiveFrameworkSettingsAsset.ResourcesPath
```

O runtime pode carregar o asset com:

```text
Resources.Load<ImmersiveFrameworkSettingsAsset>(ImmersiveFrameworkSettingsAsset.ResourcesPath)
```

O tooling de editor pode criar o asset no caminho relativo:

```text
Assets/ImmersiveFramework/Resources/ImmersiveFrameworkSettings.asset
```

Essa decisão é aceita para F2 como política de bootstrap mínimo, não como API estável final de distribuição.

---

## Restrições

O runtime não deve:

```text
criar settings automaticamente
buscar todos os assets do projeto
usar fallback silencioso
usar path absoluto local
escolher GameApplication por nome
usar singleton global de configuração mutável
```

Se settings ou active game application estiverem ausentes, o boot deve falhar de forma visível via validator/result/log/fact, não por fallback.

---

## Consequências

### Positivas

- Mantém o bootstrap simples e determinístico.
- Não exige pacote externo nem ProjectSettings customizados no F2.
- Evita scan global e fallback silencioso.
- Usa paths relativos ao projeto/Assets.

### Trade-offs

- `Resources` permanece uma solução temporária.
- A configuração final pode mudar antes de API estável.
- O asset precisa estar no local esperado para runtime build funcionar.

---

## Fora do escopo

- Migrar para Addressables.
- Criar provider customizado de settings final.
- Criar package settings externo.
- Criar bootstrap multi-application.
- Criar fallback por scene object.

---

## Critérios de validação da implementação posterior

- A origem do settings está documentada e validada.
- Missing settings falha visivelmente.
- Missing active `GameApplicationAsset` falha visivelmente.
- Não existe fallback silencioso.
- O smoke de boot continua usando a mesma origem explícita.

---

## Relação com roadmap

Cobre:

```text
IF-FW-ROAD-2E — Settings source decision
```

Relaciona-se com:

```text
F2-01 — Session Scope and Owner
F2B — SessionRuntimeState explicit boundary
```

---

## Notas de implementação

Não é necessário trocar a origem de settings no F2A. O corte só aceita e documenta a política.

Uma migração futura pode substituir `Resources`, mas deve vir por ADR/corte próprio e manter fail-fast sem fallback silencioso.
