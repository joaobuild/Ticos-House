# Validação da versão 0.1.0

## Executado

- Compilação Windows x64 em Unity 6000.3.14f1: concluída.
- Execução do player em Direct3D 11 / NVIDIA GTX 1650: concluída.
- Núcleo: 200 seeds de Final Hunt e 30 noites completas, 1.512.916 verificações de transições, limites e invariantes. Os números altos são principalmente verificações de limites por tick; não representam milhões de cenários diferentes.
- Player final: 26 verificações integradas passaram, incluindo shader, render texture, voz, colisão, cadeira, 4 aliados distintos, 5 agentes, cabeça, corpo, erro de mira, munição, recarga, dano, partida até 13, placar, entrega única de resultado, cama, monitor, carinho, Final Hunt e reinício sem duplicar runtime ou AudioListener.
- Capturas da build visível conferidas: Ascent pixelada com Jett, interface, cama e Tico na porta.
- Build normal aberta para playtest do usuário, sem argumentos de teste.

## Problemas encontrados e corrigidos

1. Shader Standard removido da build procedural: referência explícita e inclusão corrigidas.
2. Tiros do protótipo 3D não identificavam o collider do jogador: corrigido antes da substituição por câmera fixa.
3. Reinício descarregava o runtime sem criar um novo: runtime salvo na cena de entrada.
4. Mixagem estridente reportada pelo usuário: ruído filtrado, picos menores, menos tiros simultâneos e áudio dos fones removido ao levantar.
5. Porta reportada com visual estranho: batentes, painéis, maçaneta e iluminação refeitos.
6. Mira e imagem poderiam usar proporções diferentes: ambas usam as mesmas coordenadas 960×540, com apresentação sem corte.
7. Uma janela de validação automática foi mostrada como se fosse interativa, confundindo o playtest: a janela normal foi aberta separadamente. O jogo normal nunca roda o roteiro automaticamente.
8. Usuário não conseguia clicar para buscar partida: cursor agora fica livre no lobby e Buscar partida virou um botão real. O atalho Enter permanece. A validação verifica o modo de cursor do lobby e a transição para a mira.
9. Alternar para outra janela deixava a noite avançar durante a conversa: adicionada pausa automática na perda de foco.

## Escopo e limites

O runner usa o player real, renderização, física do quarto e métodos de gameplay. Ele prepara estados específicos e acelera um match de bots; isso é teste de integração, não uma partida humana completa. O balanceamento da noite, a diversão e a percepção espacial exigem mais partidas com fones. O primeiro retorno humano motivou a troca completa do FPS 3D por Ascent fixa e a revisão do áudio.

## Reproduzir

- `Tools/test-simulation.ps1`: núcleo puro, sem editor.
- `TicosHouse.exe --smoke-test -batchmode -logFile smoke.log`: inicialização e Final Hunt.
- `TicosHouse.exe --qa-test --qa-dir <pasta> -logFile integration.log`: runner integrado. A janela fica parada inicialmente e depois executa ações sozinha. Não é um modo para jogar.
- `TicosHouse.exe` sem argumentos: jogo normal, controlado pelo usuário.

## Regressão manual

| Cenário | Esperado |
|---|---|
| Começar, WASD, E na cadeira, Enter | Entra na Ascent sem mover a câmera do mapa |
| Mouse e clique | Mira livre; cabeça mata em um tiro; corpo exige três |
| Segurar Tab | Placar do time |
| F, E, andar até cama, E | Monitor apagado, Gust escondido |
| Levantar na falsa saída | Derrota |
| Wi-Fi cortado mesmo na cama | Final inevitável |
| M, V, −/+, Esc e volume geral | Controles de comunicação e mixagem |
| Recomeçar | 21h, 0 PDL, internet online e uma única instância dos sistemas |

## Regressão 0.1.1

Build Windows validada com 34 verificações automatizadas (TICOS_QA_PASS): 150 HP, dano/efeito de tiro, morte, preservação de vida e estatísticas na troca de cenário, intervalo de reação, novos assets, munição, recarga, partida até 13, Final Hunt e restart. Fundos também renderizados diretamente da câmera para PNG e inspecionados. Execução oculta; não substitui playtest humano de dificuldade.

## Regressão 0.1.2

37 verificações do player passaram, incluindo cinco fundos carregados, movimento com amplitude maior que 70 pixels e mais de oito inversões numa amostra determinística de 9,6 s, limites das hitboxes e proteção durante troca de cenário. Novos fundos renderizados pela câmera e inspecionados. Não houve teste humano de dificuldade.

## Regressão 0.1.3

Cobertura nova: amostra determinística de 100 disparos para verificar headshots, tiros no corpo e cadência; 300 ações aliadas por modo, com a mesma seed, comparando contribuição mutada e comunicação aberta. Verifica cobertura maior com comunicação e ausência de cobertura quando o aliado morre. Esses testes verificam o efeito mecânico, não equivalem a playtest humano de dificuldade.

Resultado: 42 verificações do player aprovadas (TICOS_QA_PASS).
