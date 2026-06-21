# ADR-DIAG-001 — FrameworkFact vs Human Log

Status: Accepted  
Fase: F1  
Tipo: Diagnostics  
Escopo: Diagnostics

---

## Contexto

O baseline atual já possui `FrameworkLogger` para logs humanos. Isso é suficiente para leitura no Console e smoke manual, mas não é suficiente como contrato de validação.

O framework precisa distinguir dois usos diferentes:

```text
Log humano = mensagem textual para leitura e investigação.
FrameworkFact = dado estruturado sobre algo que aconteceu ou foi validado.
```

Sem essa separação, smokes, validators e diagnósticos futuros tendem a depender de parsing de texto, o que transforma logs em API informal e frágil.

## Decisão

Adicionar o conceito de `FrameworkFact` como diagnóstico estruturado mínimo, separado de logs humanos.

`FrameworkFact` deve representar fatos do framework, não mensagens formatadas. Um fact mínimo deve conter, quando aplicável:

| Campo | Papel |
|---|---|
| `code` | Código estável do fato/resultado. |
| `scope` | Escopo: Application, Session, Route, Activity, Content, Local, Surface, Runtime, Validation, QA. |
| `severity` | Info, Warning, Error, Fatal ou equivalente. |
| `source` | Origem técnica ou autoral do fato. |
| `subject` | Identidade ou referência do sujeito observado. |
| `reason` | Motivo estruturado ou textual curto. |
| `details` | Dados opcionais para debug/relatório. |

Logs continuam existindo, mas sua função é comunicação humana. Facts são usados por:

- smoke;
- validators;
- QA tooling;
- relatórios;
- decisões explícitas de fail-fast/degraded em fases futuras.

## Regras

1. **Não parsear log como contrato**  
   Nenhum fluxo novo deve depender de substring de log para validar sucesso/falha.

2. **Fact não substitui log imediatamente**  
   Durante a transição, um mesmo evento pode gerar log humano e fact estruturado.

3. **Fact não é telemetry externa**  
   F1 cria diagnóstico interno mínimo. Export, dashboard e telemetry ficam fora.

4. **Sem recorder monolítico público**  
   F1 não deve introduzir service locator, event bus global ou fact recorder público obrigatório.

5. **Escopo explícito**  
   Facts devem indicar escopo para evitar mistura entre Application, Route, Activity e Content.

## Consequências

### Positivas

- Evita transformar logs em API.
- Melhora smoke e validators.
- Prepara requiredness, release diagnostics e stale/foreign reference checks.
- Permite relatórios mais confiáveis sem aumentar ruído humano.

### Negativas / trade-offs

- Pode haver duplicidade temporária entre log e fact.
- Exige definir códigos/severidade mínimos.
- Exige disciplina para não transformar facts em sistema global pesado.

## Fora do escopo

- Telemetry externa.
- Dashboard visual.
- Persistência de fatos.
- Event bus global.
- Fact schema avançado.
- Substituir todos os logs atuais.

## Critérios de validação

- Existe uma modelagem mínima de `FrameworkFact` antes de validators avançados dependerem dela.
- Logs humanos continuam úteis, mas não são fonte funcional de verdade.
- Smokes futuros podem validar outcomes por facts ou resultados tipados, não por parsing de texto.
- Nenhum fact recorder público vira service locator.

## Impacto esperado

Pré-requisito para:

- `ValidationMode` com semântica real;
- required/optional policy;
- Content/Local diagnostics;
- release diagnostics;
- QA tooling mais confiável.

## Relação com roadmap

F1. Condiciona F5 e fases posteriores que dependem de validators e smoke estruturado.

## Notas de implementação

O primeiro corte técnico deve ser pequeno: tipo(s) mínimos, severidade/scope/código e integração limitada. Não criar pipeline de diagnostics amplo ainda.
