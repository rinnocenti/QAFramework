# IF-ADR-046 — Pacote de evidências apresentado pelo usuário

Este pacote contém evidências que o usuário forneceu à conversa, para permitir análise direta pelo Codex. **Não é certificação automática** nem afirma que os testes executaram nos SHAs listados.

## Conteúdo

- `FirstGame-LocalMultiplayer-console.txt` — log bruto integral fornecido pelo usuário, 886 linhas / 339106 bytes; contém bootstrap, Join P1/P2, Leave e Rejoin.
- `Framework-EditMode-292of292.png` — screenshot do Test Runner enviado pelo usuário, indicando 292 testes EditMode aprovados, 0 falhas, sendo PlayerParticipation 101 testes.
- `QA-terminals.txt` — transcrição literal das linhas terminais publicadas na conversa para IF-ADR-042 (7/7), 043 (7/7), 044 (8/8), Slice 1 (8/8) e Leave/Rejoin Occurrence (PASS). Não contém os logs integrais de cada cenário.
- `SHA256SUMS.txt` — hashes calculados dos arquivos entregues (integridade do pacote, não timestamp de execução).

## Contexto Git reportado separadamente pelo Codex

- Framework no relatório mais recente: `c66be5709e669dfc71b5e9ea9320156496058490`, `master`, limpo. A execução não contém SHA embutido e **não deve ser vinculada retroativamente** a esse commit.
- QAFramework: `693e0d1048303f5fde26831b117ad4c82fd2cf7d`, com configuração local mencionada no relatório mais recente.
- FirstGame: `c0d14a057197624ff2f070a6e601d7d225f972db`, `main`, limpo no último relatório.
- Framework consumido pelo FirstGame: dependência local `file:C:/Projetos/ImmersivePackages/com.immersive.framework` (conforme inspeção anterior de Codex). Esse caminho não fixa SHA.

## Limites

A data/hora exata de cada execução, os hashes dos bytes Unity/importados no momento da execução e o SHA automaticamente capturado naquele momento não constam dos terminais. Não devem ser inventados. O arquivo bruto do FirstGame e a captura do Test Runner são evidências preservadas agora; capturar seus hashes **não atribui** proveniência Git histórica.

## Propósito

Permitir que o responsável pela governança avalie aceitação proporcional de evidência funcional com proveniência parcial, documentando risco residual, sem criar obrigações de guardar Console vivo para cada QA ou repetir testes exclusivamente para capturar SHA.
