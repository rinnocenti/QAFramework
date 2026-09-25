---
name: qa-scenario-design
description: Use ao projetar um novo Scenario de certificação runtime no QAFramework (ou o Test Environment que ele precisa), seguindo o ADR-001. Transforma um contrato público do Immersive Framework em uma especificação completa de baseline/action/observation/evidence/cleanup, com categoria e semântica de veredito, ANTES de qualquer código ser escrito. Use também quando o usuário pedir para "criar uma nova QA/smoke/scenario" para o QAFramework.
---

# QA Scenario Design (ADR-001)

Este skill não gera código. Ele produz a especificação de um Scenario — a
unidade conceitual de certificação do QAFramework (ADR-001 §5) — pronta para
ser implementada pelo agente `qa-scenario-writer` ou por um humano.

Fonte de verdade: `ADR-001-QA-Framework-Certification-Architecture.md` (Project
"QA Framework"). Releia as seções citadas abaixo se houver qualquer dúvida —
não invente regra que não esteja lá.

## Antes de começar: onde esse contrato deve ser provado?

O ADR define três níveis (§2). Antes de desenhar um Scenario de QAFramework,
confirme que o nível é o certo:

- Se o contrato pode ser provado sem um runtime materializado (lógica pura,
  value objects, políticas, transições de estado determinísticas,
  serialização, invariantes estruturais) → isso é **Package/NUnit**, não
  QAFramework. Pare aqui e redirecione.
- Se o contrato é sobre o framework real sob condições nominal/boundary/
  adversarial/negative → **QAFramework**. Continue.
- Se é sobre um jogo real combinando features suportadas → **FIRSTGAME**, não
  QAFramework (§2, §17 "Use FIRSTGAME as QA runtime environment" — rejeitado
  como direção inversa também: QAFramework não deve virar um FIRSTGAME, §3).

"Autoria intencionalmente inválida não é automaticamente QA runtime" (§6). Se
dá para provar deterministicamente sem materializar o runtime, é nível mais
baixo.

## Passo 1 — Classifique a categoria do Scenario (§6)

Escolha exatamente uma:

- **Nominal**: configuração válida, operação esperada, usada como prova de
  baseline quando necessário.
- **Boundary**: operação válida na borda (ou atravessando) um contrato.
- **Adversarial**: ambiente válido com timing, ordenação, lifecycle ou
  ownership deliberadamente difíceis.
- **Negative**: requisição suportada que se espera ser rejeitada, sem
  corromper estado válido.

Registre por que essa categoria e não outra — isso vira a justificativa do
Scenario.

## Passo 2 — Baseline conhecido (§4)

Todo Scenario runtime parte de um baseline válido e conhecido:

```
valid environment → known baseline → supported action → condition under test
→ observable framework response → terminal evidence → cleanup → known baseline
```

Pergunte explicitamente:
- Esse baseline já existe como Test Environment canônico, ou precisa de um
  novo? (ver Passo 3)
- Existe qualquer chance de o Scenario depender de ordem de execução ou
  resíduo de outro Scenario? Se sim, o design está errado — "residual state
  is not a fixture" (§4, §9).
- O baseline original pode ser restaurado de forma segura durante o runtime
  atual? Se não, o Scenario **requer fresh boot** — declare isso
  explicitamente, não deixe implícito.

## Passo 3 — Test Environment (§3, §8)

Defina a composição real mínima necessária — nunca a composição inteira do
FIRSTGAME, nunca mecanicamente derivada de pastas/namespaces do código-fonte.
Pergunte: "qual é a menor composição real do framework que esse Scenario
precisa?"

Classifique a composição:
- **Canônica**: deve ser persistida e inspecionável quando fizer sentido.
  Execução normal de QA *valida* o environment, não o conserta
  silenciosamente.
- **Temporária**: só quando a prova exigir mutação destrutiva, autoria
  inválida intencional, topologia especializada, ou isolamento que não pode
  ser restaurado in-place. Estado temporário tem dono explícito e deve ser
  destruído/restaurado por quem o criou.

Setup/rebuild do environment é uma operação de autoria, distinta da execução
do Scenario — não misture as duas coisas no design.

## Passo 4 — Action e Observation (§7)

A action deve usar **apenas superfícies suportadas**: APIs públicas do
framework, autoria suportada, integrações reais quando fizerem parte do
contrato (ex.: Input System, camera runtime).

Proibido por design (§7): reflection privada, service locator para burlar
ownership, descoberta global oportunista, hosts de runtime internos,
fallback silencioso, mutação direta de estado privado do framework.

Sistemas externos podem ser controlados quando legitimamente fora do
contrato do framework (ex.: dispositivos sintéticos de Input System).

**Se o contrato importante não puder ser exercitado/observado por superfície
suportada**, não mude o framework ainda — classifique o gap primeiro (§7):

1. É uma preocupação de teste de package/interno?
2. Falta observabilidade pública?
3. Falta um ponto de extensão suportado?
4. É um contrato que não deveria ser certificado externamente?

O framework não deve ganhar backdoors só para conveniência de QA. Registre a
classificação do gap explicitamente na especificação do Scenario — não pule
essa etapa criando um workaround.

## Passo 5 — Async causal proof, se aplicável (§11)

Se um estado assíncrono intermediário faz parte do contrato sendo provado:

```
start operation → observe required pending/intermediate state
→ release controlled condition → await terminal state → verify terminal evidence
```

QA controla a condição que permite a conclusão. Nunca deixe o Scenario
"correr" contra uma janela assíncrona naturalmente curta quando esse estado
intermediário é exatamente o que está sendo provado.

## Passo 6 — Evidence e veredito (§12)

Antes de qualquer execução, declare o "expectation set": que evidência prova
o contrato.

Evidência conceitual a especificar:
- scenario identity
- expected evidence/cases
- observed evidence/cases
- terminal result
- first causal failure (quando aplicável — falhas secundárias não substituem
  a primeira divergência causal)
- cleanup result
- restore result (quando aplicável)

Semântica de veredito (use exatamente estes três, nada além):
- **PASS**: contrato provado.
- **FAIL**: contrato foi exercitado corretamente e foi violado.
- **BLOCKED**: environment, precondição ou observabilidade impediu que o
  contrato fosse exercitado corretamente.

## Passo 7 — Cleanup e ownership (§9)

"O componente que cria estado temporário é dono da sua liberação." Especifique:
- ordem de release (reverse ownership order para recursos runtime).
- quem restaura mutações a nível de environment (o owner do
  environment/runner).
- que cleanup roda tanto em sucesso quanto em falha, e aparece na evidência
  terminal.
- o resultado explícito ao final: `baseline restored` ou `fresh runtime boot
  required`.

Um veredito de certificação nunca pode ser emitido antes do cleanup e da
restauração obrigatórios terminarem.

## Passo 8 — Escopo do Scenario (§5, §13)

Um Scenario pode provar múltiplas observações **só se pertencerem ao mesmo
ciclo de vida causal**. Não deixe virar uma suíte genérica.

Se esse Scenario só funciona quando executado dentro de uma Suite/Full QA
maior (compartilhando boot, dependendo de outro Scenario), o isolamento está
errado (§13) — volte ao Passo 2.

## Saída deste skill

Produza um documento curto (pode virar o cabeçalho/comentário do Scenario)
com:

1. Contrato sob teste (1 frase, referência à API pública).
2. Categoria (§6) + justificativa.
3. Baseline / Test Environment (canônico existente, ou novo — canônico ou
   temporário — com dono do cleanup).
4. Action (superfícies suportadas usadas).
5. Observation + Evidence esperada.
6. Semântica PASS/FAIL/BLOCKED específica deste Scenario.
7. Cleanup: ordem, dono, resultado terminal esperado
   (`baseline restored` / `fresh boot required`).
8. Gap classification, se algo não for exercitável por superfície suportada.

Isso é o input para o agente `qa-scenario-writer`. Não avance para
implementação sem esses 8 pontos resolvidos — implementação apressada é
exatamente o que o ADR-001 tenta evitar (§14, §16).
