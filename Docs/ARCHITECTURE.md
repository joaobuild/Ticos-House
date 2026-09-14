# Arquitetura

`NightSimulation` é um núcleo determinístico por seed, sem dependências Unity. Ele controla a noite e emite eventos de som, diálogo e final. `TicoState` não é exibido ao jogador. `Internet=false` é um latch terminal: interrompe partidas e torna qualquer final subsequente o final Wi-Fi.

`GameRuntime` inicializa a cena, processa interações e renderiza o HUD. O jogador físico permanece no quarto enquanto a câmera do FPS renderiza outro ambiente, a 100 metros, em uma RenderTexture. Apenas o quarto tem AudioListener. Assim, sons da casa usam sua posição verdadeira; sons do FPS são mixados separadamente com panorâmica estéreo relativa à câmera virtual.

`FPSMinigame` controla os nove bots, jogador, rounds, munição, tiros por raycast, stats e resultados. O tick continua fora do computador. O controlador de quarto e o de FPS recebem input de maneira exclusiva. Mic afeta probabilidades de acerto dos aliados; volume não modifica dados de mira ou dano.

`WorldBuilder` cria toda a geometria e materiais. `ProceduralAudio` gera clips uma vez e reutiliza fontes. Não há downloads ou serviços externos durante a partida.

`BuildGame` gera a cena e define a configuração Windows. O bootstrap permite importar o projeto sem referências ausentes. Reinício carrega a cena limpa, com uma nova simulação.

## Invariantes

- Apenas uma concessão de PDL por partida.
- PDL não é negativo; Diamante requer ao menos 100.
- Após perda definitiva da internet, não há recuperação nem progressão.
- Cama e monitor desligado são necessários durante inspeção.
- Levantar durante falsa saída causa derrota.
- Pausa congela ambos os jogos e silencia áudio do FPS.
- Tico não usa coordenadas ou estados em HUD; informações chegam por áudio e personagens.
