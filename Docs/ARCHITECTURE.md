# Arquitetura

`NightSimulation` é um núcleo determinístico por seed sem dependências Unity. Controla tempo, variáveis, visitas e finais e emite sons, diálogos e efeitos. O estado e a suspeita do Tico nunca são mostrados no HUD. Depois de `Internet=false`, todo final é Wi-Fi e não há progresso.

`GameRuntime` coordena input, quarto, interfaces e cenas. A cena contém o runtime para que o reinício funcione. Há um CharacterController e um AudioListener na casa. Computador e cama usam interações de proximidade. A câmera aproxima-se do monitor ao sentar.

`FPSMinigame` é o minijogo de mira fixo da Ascent solicitado na revisão. Uma câmera ortográfica isolada na layer 8 renderiza o cenário e cinco sprites em RenderTexture 640×360 com filtro Point. Mira e hitboxes compartilham coordenadas lógicas 960×540. Os agentes fazem AD–AD; o jogador não navega pelo mapa. Aliados são simulados fora de tela e continuam lutando quando Gust abandona a cadeira.

`WorldBuilder` cria a casa e personagens geométricos. `ProceduralAudio` sintetiza efeitos e reutiliza fontes. Sons da casa são espaciais; os do FPS têm panorâmica própria e desligam ao tirar os fones. Master e FPS têm volumes separados.

`BuildGame` garante materiais e shaders, configura importação das imagens e compila Windows x64. `RuntimeValidation` só é criado quando o executável recebe `--qa-test` explicitamente. Sem argumentos, o jogo sempre inicia no menu normal.

## Invariantes

- Uma entrega de resultado e PDL por partida.
- PDL nunca negativo; Diamante a partir de 100.
- Nenhuma recuperação ou vitória após Wi-Fi desligado.
- Inspeção exige cama e monitor apagado.
- Sair da cama enquanto Tico ainda verifica/finge sair causa derrota.
- Pausa congela os dois jogos; não aparece localização da ameaça no HUD.
- Reinício recria o runtime e mantém apenas um AudioListener.
