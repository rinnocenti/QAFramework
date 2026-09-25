---
name: qa-greenfield-slice
description: Use ao iniciar ou expandir a cobertura de certificação runtime do QAFramework. Garante a sequência greenfield do ADR-001 (uma vertical slice por vez → validar manualmente → segunda slice deliberadamente difícil → reavaliar arquitetura comum → só então expandir sistematicamente) e o checklist de infraestrutura de execução (Runner). Use antes de aceitar "vamos criar várias QAs" ou "vamos montar a arquitetura genérica" de uma vez.
---

# QA Greenfield Slice Sequencing (ADR-001 §14–§16)

O QAFramework é greenfield: a implementação legada foi removida, e decisões
como nomes de classes concretas, se Scenario é interface/base
class/component, representação de Environment, estrutura de diretórios,
topologia de asmdef, descoberta/registro de Scenario, formato de resultado
persistente, valores exatos de timeout e número de environments canônicos
são **intencionalmente não decididas** (§16) até que vertical slices
demonstrem os requisitos mínimos comuns.

Esse skill existe para impedir o erro mais provável nesse momento do
projeto: pular direto para "vamos construir o Runner genérico e a suíte
inteira" antes de ter evidência de duas slices reais.

## Passo 1 — Onde estamos na sequência? (§14)

Determine em qual passo da sequência o pedido atual se encaixa:

1. Preservar decisões arquiteturais e auditoria histórica como documentação
   (já feito — ADR-001).
2. Implementar a infraestrutura mínima de execução exigida por **uma**
   vertical slice.
3. Implementar **um** Scenario runtime representativo.
4. Validar manualmente no Unity.
5. Implementar um **segundo** Scenario deliberadamente difícil.
6. Reavaliar a arquitetura comum.
7. Só então expandir a cobertura de certificação sistematicamente.

Se o pedido do usuário é "criar várias QAs/scenarios de uma vez" e os passos
2–6 ainda não aconteceram, **isso é um desvio do ADR** — sinalize
explicitamente antes de prosseguir e proponha reduzir para uma única slice
primeiro.

## Passo 2 — Se isto é a primeira slice (§15, primeira demonstração)

A primeira slice deve demonstrar o ciclo completo:

```
known environment → baseline → supported action → evidence → cleanup
→ baseline restored → verdict
```

Nada além disso. Não construa Runner genérico, Suite, discovery automático,
ou múltiplos environments canônicos ainda — isso é decisão adiada (§16).

## Passo 3 — Se isto é a segunda slice (§15, segunda demonstração)

A segunda slice deve exercitar pelo menos **uma** propriedade difícil de
lifecycle:

- async controlado
- execução multi-fase
- requisito de fresh boot
- timing adversarial
- conflito de ownership
- interrupção/recuperação de runtime

Se essa segunda slice **exigir uma arquitetura de execução separada** da
primeira, a conclusão correta é: "o modelo comum ainda não está provado"
(§15) — não force um Runner unificado prematuramente; documente a
divergência e volte ao Passo 1.

## Passo 4 — Checklist de infraestrutura de execução (Runner, §10)

Mecânica de execução é infraestrutura compartilhada e não deve ser
reinventada por domínio. Quando (e só quando) o Runner comum estiver sendo
extraído de slices já provadas, ele deve cobrir:

- preparação em Edit Mode
- entrada em runtime
- transições de Play Mode
- sobrevivência a domain reload
- persistência de fase de execução
- timeouts
- recuperação de execução interrompida
- saída do runtime
- restauração do environment
- publicação do resultado terminal

CAMERA-032 histórico é evidência desses requisitos, não uma implementação
para copiar (§10). Classes concretas do Runner permanecem indecididas até
que as slices demonstrem o mínimo comum.

## Passo 5 — Antes de expandir (§13, §14 item 7)

Só expanda cobertura sistematicamente depois do Passo 6 (reavaliação). Ao
expandir, lembre:

- Suite seleciona Scenarios independentemente válidos; não reimplementa
  setup de environment, actions, assertions, cleanup ou lifecycle.
- Full QA é orquestração, não uma mega-smoke.
- Scenarios só podem compartilhar um boot de Play Mode se cada baseline for
  verificável independentemente, cada um restaura seu próprio estado,
  nenhum depende de resíduo de outro, e a falha de um não invalida a
  evidência do outro.
- Performance não justifica acoplamento oculto de lifecycle.

## Passo 6 — Sinalize desvios em vez de "corrigir" silenciosamente

Se o pedido do usuário conflita com essa sequência (ex.: "recria a suíte
antiga", "monta já toda a estrutura de pastas genérica", "faz um harness
específico para esse domínio"), não implemente silenciosamente do jeito
pedido nem do jeito que você acha certo — explique o trade-off citando a
seção do ADR-001 e pergunte como o usuário quer prosseguir. Alternativas
descartadas pelo ADR (§17) que merecem esse alerta:

- usar FIRSTGAME como ambiente de QA
- criar um jogo de referência dentro do QAFramework
- mockar o runtime do framework dentro do QAFramework
- um harness especializado por domínio
- recriar a arquitetura legada
