# Minolynth — Procedural Maze Game

> Projeto desenvolvido no âmbito da unidade curricular de **Tecnologias Multimédia**

---

## Equipa

| Nome | Número de Aluno |
|------|----------------|
| Gabriel Banks | 29730 |
| Jõao Fernandes | 29964 |

---

## Versão do Unity

**Unity 6000.3.9f1** (Unity 6) — PC, Linux x86\_64

---

## Descrição do Projeto

**Minolynth** é um jogo de labirinto 2D com geração procedural. O jogador navega por um labirinto único gerado a cada sessão, recolhe chaves para abrir portas, evita inimigos com IA própria e procura a saída para vencer.

### Funcionalidades Implementadas

- **Geração procedural do labirinto** — cada sessão produz um layout único baseado em grelha dinâmica
- **Movimento passo a passo** — o jogador desloca-se célula a célula com animação de transição suave e contador de turnos
- **Sistema de chaves e portas** — chaves espalhadas pelo labirinto que desbloqueiam portas específicas
- **IA de inimigos** — um inimigo com deteção de colisão que provoca game over
- **Câmara dinâmica** — câmara que segue o jogador ao longo do labirinto
- **Áudio** — efeitos sonoros de movimento e feedback de jogo, com gestão de AudioSource e clips configuráveis
- **UI completa** — menu principal, menu de pausa, ecrã de vitória, ecrã de derrota e página de definições (jogabilidade)

---

## Jogabilidade

### Objetivo

Recolher as chaves necessárias, abrir as portas correspondentes e alcançar a **saída** do labirinto sem ser apanhado pelos inimigos.

### Controlos

| Tecla | Ação |
|-------|------|
| `W` / `↑` | Mover para cima |
| `S` / `↓` | Mover para baixo |
| `A` / `←` | Mover para a esquerda |
| `D` / `→` | Mover para a direita |
| `ESC` / `P` | Pausar / retomar o jogo |
| `Enter` | Confirmar opção nos menus |

### Regras

1. O jogador começa na entrada do labirinto.
2. Cada tecla de direção move o jogador uma célula — o movimento é bloqueado se existir parede nessa direção.
3. Certas portas exigem a posse da chave correspondente para poderem ser atravessadas.
4. Contacto com um inimigo resulta em **game over**.
5. Alcançar a célula de saída com as condições cumpridas resulta em **vitória**.

---

## Como Abrir o Projeto

### Pré-requisitos

- [Unity Hub](https://unity.com/download) instalado
- Unity **6000.3.9f1** instalado via Unity Hub

### Passos

1. Clonar o repositório:
   ```
   git clone https://github.com/GJJMB/tecmulProj1.git
   ```
2. Abrir o **Unity Hub** → **Add** → **Add project from disk**.
3. Selecionar a pasta raiz `tecmulProj1/`.
4. Confirmar que a versão do editor é **6000.3.9f1**; instalar via Unity Hub se necessário.
5. Abrir o projeto e, no painel *Project*, navegar até `Assets/Scenes/` e fazer duplo clique na cena principal.
6. Clicar em **Play (▶)** para correr o jogo no editor.

### Compilação Pré-existente (Linux)

Uma build compilada para Linux x86\_64 está disponível em `gamecompiles/`:

```bash
./gamecompiles/build1final.x86_64
```

A pasta `build1final_Data/` deve estar no mesmo diretório que o executável.

---

## Assets Multimédia

### Áudio

|   Asset     | Formato |                         Justificação                         |
|-------------|---------|--------------------------------------------------------------|
| Step 1 - 4  |   WAV   | DAW em que foi editado o audio original so funciona com wav  |
| Doorcreek   |   WAV   | DAW em que foi editado o audio original so funciona com wav  |
| keyjingle   |   WAV   | DAW em que foi editado o audio original so funciona com wav  |
| Failsfx     |   WAV   | Audio original era de pobre qualidade                        |
| Win         |   WAV   | Audio Retirado do sistema operativo Win 95 formato original  |

> Os clips de áudio estão em `Assets/audio/` e são referenciados e configuráveis via `SettingsPageController`.

### Visuais / Texturas

| Asset | Ficheiro | Formato | Origem / Justificação |
|-------|---------|---------|----------------------|
| Fundo do menu principal | `testmenu.png` | PNG |  Retirado do Freepik; PNG mantido no formato original para preservar qualidade sem recompressão |
| Fundo de jogo | `rm218-bb-07jpg.jpg` | JPG | Retirado do Freepik; JPG mantido no formato original da fonte |
| Ecrã de vitória | `yourewinner.jpg` | JPG | Meme extraído do videojogo *Big Rigs: Over the Road Racing*; JPG por ser o formato em que o asset circula online |
| Ecrã de derrota | `you loser.png` | PNG | Versão editada do asset de vitória; PNG resultante do editor de imagem utilizado na edição |

> Assets em `Assets/Scene Backgrounds/`. Os ficheiros `.meta` são gerados automaticamente pelo Unity e não fazem parte dos assets originais.
> Fundos em `Assets/Scene Backgrounds/`; prefabs e objetos de jogo em `Assets/Objects/`.

*(Preencher esta secção com os formatos e resoluções reais dos assets utilizados)*

---

## Estrutura do Repositório

```
Assets/
├── Scripts/
│   ├── PlayerController.cs        # Movimento, input e gestão de turnos
│   ├── MazeGridController.cs      # Grelha do labirinto e pathfinding
│   ├── Mazegen2.cs                # Geração procedural do labirinto
│   ├── EnemyController.cs         # IA dos inimigos
│   ├── GameSetup.cs               # Inicialização do estado de jogo
│   ├── Key.cs / Door.cs           # Lógica de chaves e portas
│   ├── CameraFollow.cs            # Câmara dinâmica
│   ├── MainMenu.cs                # Menu principal
│   ├── PauseMenu.cs               # Menu de pausa
│   ├── WinScreen.cs               # Ecrã de vitória
│   ├── GameOverScreen.cs          # Ecrã de derrota
│   ├── BackgroundController.cs    # Renderização de fundo
│   └── SettingsPageController.cs  # Página de definições
├── Scenes/                        # Cenas Unity
├── Objects/                       # Prefabs e objetos de jogo
├── audio/                         # Efeitos sonoros e música
├── Scene Backgrounds/             # Texturas de fundo
└── Settings/                      # Configurações do jogo
gamecompiles/                      # Build executável (Linux x86_64)
```

---

## Dependências

| Package | Origem |
|---------|--------|
| **TextMesh Pro** | Incluído no projeto (UI de texto) |
| **Input System** | Incluído no projeto (input de teclado/controlador) |

---

## Observações e Limitações Conhecidas

- Baixo FrameRate devido a maneira que esta a ser gerado o Labirinto
- Inimigo nao aumenta de dificuldade ao longo do percurso do jogo