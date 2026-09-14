# Registro de validação

## Executado

- Compilação do núcleo com o compilador C# do Windows: passou.
- Compilação estática de todos os scripts de runtime contra assemblies reais da Unity 6000.3.14f1: passou.
- 200 seeds de Final Hunt: cama, monitor desligado, chamadas repetidas e passagem das 06h não permitem escapar.
- 30 noites completas com jogador silencioso na cama: IA visita o quarto e a noite termina em Survivor.
- Faixas de PDL, progressão, derrota sem PDL negativo, comunicação, carinho, pausa e detecção: passaram.
- Total: 1.512.916 verificações, a maioria limites de variáveis durante simulações de tempo.

## Bloqueio atual

A primeira tentativa de build foi interrompida pela Unity com `No valid Unity Editor license found`. Unity Hub foi instalado para ativação pelo titular da conta.

## Ainda obrigatório antes de publicar v0.1.0

- Build Windows x64 no editor licenciado.
- Smoke test no player compilado e análise de logs.
- Teste visual de quarto, monitor, FPS, cama e jumpscare.
- Disparos reais, hitboxes da cabeça, dano ao jogador, recarga, rounds e fim de partida.
- Testes de volume, panorâmica, passos e ronco com fones.
- Fluxo de reinício depois de derrota e opções.
- Playtest humano de ritmo, legibilidade e balanceamento.

## Matriz manual

| Cenário | Resultado esperado |
|---|---|
| Entrar no quarto e andar até mesa | Colisão preservada, E funciona perto da cadeira |
| Sentar e Enter | FPS renderiza no monitor, sorteia 4 aliados |
| Sair durante round | Bots continuam e o round pode ser perdido |
| Tico inspeciona com monitor ligado | Derrota, mesmo na cama |
| Monitor desligado + cama | Sobrevive à visita normal |
| Falsa saída + levantar | Jumpscare |
| Wi-Fi cortado + cama | Derrota inevitável |
| V / − / + | Só volume do FPS muda |
| Partida vencida | PDL concedido uma única vez |
| Reiniciar | Relógio 21h, PDL 0, internet online, IA dormindo |
