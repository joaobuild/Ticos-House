# Tico's House

Terror com comédia em primeira pessoa: Gustavo prometeu desligar o computador às 21h, mas entrou em mais uma partida com os amigos. O quarto, o FPS e a casa continuam funcionando ao mesmo tempo.

**Estado: versão 0.1.0 em desenvolvimento e validação. Ainda não há build publicada.**

## Objetivo

Sair de **Platina 3 — 0 PDL** e alcançar **100 PDL / Diamante** antes das 06h, sem Tico descobrir. Habilidade rende mais PDL; menos partidas significam menos tempo exposto.

## Mecânicas

- Quarto 3D com computador, monitor ao vivo, teclado, cadeira, cama, janela e corredor.
- NULL // SHIFT: FPS fictício original com tiros, headshots, recarga, pulso tático, cobertura, bots e partidas até 13 rounds.
- Gust e quatro amigos sorteados: Joaobuild, Trolezi, Carlos, Loogins, Tavinho e Munhak. Carlos e Munhak têm habilidade inferior, com raras exceções.
- PDL por kills, deaths, assists, headshots, precisão, MVP, clutches, multi-kills, rounds decisivos e mortes precoces. Vitórias rendem 15–40; derrotas removem 12, sem descer abaixo de zero.
- Microfone fictício: aberto favorece comunicação e reduz estresse, mas faz barulho; mutado prejudica o time sem determinar automaticamente a derrota.
- Volume do FPS independente: sons da arena se misturam aos sons espaciais da casa.
- Estresse, barulho e suspeita invisível interagem com o relógio e os estados de Tico.
- Ronco, falsos alarmes, escadas, passos, porta, inspeção e falsa saída.
- Guilherme pode ajudar, errar ou sair. Meliça pode avisar, latir ou receber carinho.
- Corte de Wi-Fi irreversível: nenhum esconderijo ou vitória pode interromper o Final Hunt.
- Cinco finais e registro local dos finais descobertos.

## Como jogar

Use fones. Aproxime-se da cadeira e sente. Pressione Enter para procurar partida. Jogue bem para ganhar mais PDL, mas escute o ronco e a casa. Ao suspeitar de uma visita, desligue o monitor, levante, caminhe até a cama e deite. Espere sinais sonoros de que é seguro levantar: passos que se afastam podem ser falsos.

A partida continua enquanto você se esconde; ficar ausente pode custar rounds. O relógio vai de 21h a 06h em 21 minutos reais, sem contar pausas. Cada round termina por eliminação ou limite de tempo; no limite, vence a equipe com mais sobreviventes (empates são desempates simulados).

## Controles

| Tecla | Ação |
|---|---|
| WASD | Movimento |
| Mouse | Olhar / mirar |
| E | Sentar, levantar, deitar ou fazer carinho quando próximo |
| F | Ligar/desligar monitor próximo à mesa |
| Enter | Procurar nova partida enquanto sentado |
| Botão esquerdo | Atirar |
| R | Recarregar |
| Q | Pulso tático que prejudica a precisão inimiga temporariamente |
| Shift | Correr no quarto; caminhar devagar no FPS |
| Tab (segurar) | Placar do time |
| M | Mutar/desmutar microfone fictício |
| V | Mutar/restaurar áudio do FPS |
| − / + | Diminuir/aumentar volume do FPS |
| Alt + roda do mouse | Ajustar volume do FPS |
| Esc | Pausa, opções de áudio/sensibilidade e reinício |

O jogo não acessa seu microfone, Discord ou roteador reais. Toda comunicação e queda de conexão são ficcionais e locais.

## Download

As builds verificadas serão publicadas em [Releases](https://github.com/joaobuild/Ticos-House/releases). Não há executável validado disponível até a conclusão da compilação e dos testes visuais.

## Desenvolvimento

- Unity **6000.3.14f1 (Unity 6.3 LTS)**.
- C#, pipeline Built-in, Windows x64, backend Mono.
- Geometria, materiais e áudio sintetizado originais criados no projeto. Sem assets de Valorant ou FNAF.
- Gustavo usa terno, camisa branca, gravata turquesa e óculos; Guilherme tem cabelo bagunçado e camisa azul; Tico tem camisa listrada, barba e cinto como adereço; Meliça tem pelo irregular e laço rosa.
- As fotografias de referência não são incluídas no repositório ou no executável.

A cena e os objetos são montados automaticamente pelo código, sem configuração manual. `BuildGame.Windows` cria a cena de entrada e compila para `Builds/TicosHouse/TicosHouse.exe`. Abra a pasta na versão indicada da Unity e use **Tico's House → Build Windows x64**, ou execute `Tools/build-windows.ps1` com o caminho do editor licenciado.

## Testes

`Tools/test-simulation.ps1` executa o núcleo sem depender de licença Unity: faixas de PDL, progressão, morte irreversível, detecção, cama, monitor, pausa, reinício e simulação de 30 noites completas.

O executável também contém `--smoke-test`, que instancia o quarto e os nove bots, inicia uma partida e testa Final Hunt na cama. Isso complementa os testes manuais; não substitui a validação visual e de jogabilidade.

Veja [TESTING.md](Docs/TESTING.md) e [ARCHITECTURE.md](Docs/ARCHITECTURE.md).

## Limitações conhecidas

- Personagens e animações são modelos geométricos provisórios, não esculturas detalhadas das fotos.
- Mapa único, uma arma e uma habilidade; não há multiplayer real.
- IA tática simplificada, sem economia, plantio de objetivo ou navegação NavMesh.
- Áudio original sintetizado; o acabamento sonoro requer validação com fones.
- A distribuição real de vitórias por noite precisa de playtests humanos; testes matemáticos não provam balanceamento ou diversão.
- Interface usa teclado/mouse e escala de referência 1280×720; não há suporte a controle.

## Créditos

Conceito e personagens: grupo de amigos do Joaobuild. Implementação: projeto desenvolvido com assistência do Codex. Todos os nomes e sistemas do FPS fictício são originais deste projeto.
