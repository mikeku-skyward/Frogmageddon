# Requirements Document

## Introduction

The Start Screen feature introduces a canvas-rendered title screen for Frogmageddon that displays before gameplay begins. Instead of auto-starting the game on first render, the application shows a "Frogmageddon" title and a Start button drawn directly on the 800×600 game canvas. The player initiates gameplay by clicking the Start button or pressing Enter. The default Blazor loading spinner is removed entirely so the app loads to a black screen before showing the canvas-rendered start screen.

## Glossary

- **Start_Screen**: The canvas-rendered screen displayed after Blazor initialization that shows the game title and a start button, drawn using the 2D canvas context
- **Game_Canvas**: The 800×600 HTML5 canvas element used for all game rendering
- **Title_Text**: The "Frogmageddon" text drawn on the canvas using ctx.fillText()
- **Start_Button**: A rectangle drawn on the canvas that acts as a clickable region to begin gameplay
- **Game_Loop**: The requestAnimationFrame-based loop in GameLoop.cs that drives game updates and rendering
- **Input_Manager**: The InputManager.cs component responsible for capturing and processing user input
- **Loading_Screen**: The default Blazor SVG circle progress indicator shown in index.html during WebAssembly download
- **Playing_State**: The active gameplay state where the player controls the character and the game loop processes movement

## Requirements

### Requirement 1: Remove Default Loading Screen

**User Story:** As a player, I want the app to load to a black screen instead of showing the default Blazor spinner, so that the visual experience feels cohesive with the arcade-style game.

#### Acceptance Criteria

1. THE Loading_Screen SHALL be removed from index.html so that no SVG progress indicator or loading text is displayed during Blazor WebAssembly initialization.
2. WHILE Blazor WebAssembly resources are downloading, THE Game_Canvas SHALL not be visible to the player.
3. WHEN Blazor initialization completes, THE Game_Canvas SHALL become visible with a black background.

### Requirement 2: Display Start Screen on Canvas

**User Story:** As a player, I want to see a title screen with the game name and a Start button when the game loads, so that I know the game is ready and can begin when I choose.

#### Acceptance Criteria

1. WHEN Blazor initialization completes, THE Start_Screen SHALL render the Title_Text "Frogmageddon" centered horizontally on the Game_Canvas.
2. THE Title_Text SHALL be drawn above the vertical center of the Game_Canvas using ctx.fillText().
3. THE Start_Button SHALL be drawn as a filled rectangle centered horizontally on the Game_Canvas below the Title_Text.
4. THE Start_Button SHALL contain the text "Start" drawn centered within the rectangle.
5. THE Start_Screen SHALL render using the 2D canvas context without any HTML overlay elements.

### Requirement 3: Start Game via Mouse Click

**User Story:** As a player, I want to click the Start button to begin gameplay, so that I can start the game using familiar mouse interaction.

#### Acceptance Criteria

1. WHEN the player clicks within the Start_Button rectangle boundaries, THE Start_Screen SHALL transition to the Playing_State.
2. WHEN the Start_Screen transitions to the Playing_State, THE Game_Loop SHALL begin processing game updates and rendering the player.
3. THE Input_Manager SHALL detect mouse click events on the Game_Canvas and determine whether the click coordinates fall within the Start_Button boundaries.

### Requirement 4: Start Game via Enter Key

**User Story:** As a player, I want to press Enter to begin gameplay from the start screen, so that I can start the game using the keyboard without needing a mouse.

#### Acceptance Criteria

1. WHILE the Start_Screen is displayed, WHEN the player presses the Enter key, THE Start_Screen SHALL transition to the Playing_State.
2. WHEN the Start_Screen transitions to the Playing_State via Enter key, THE Game_Loop SHALL begin processing game updates and rendering the player.
3. WHILE the Playing_State is active, THE Input_Manager SHALL not treat Enter key presses as a start-game trigger.

### Requirement 5: Prevent Auto-Start of Gameplay

**User Story:** As a player, I want the game to wait for my explicit input before starting, so that I am not thrown into gameplay before I am ready.

#### Acceptance Criteria

1. WHEN Blazor initialization completes, THE Game_Loop SHALL not begin processing game updates until the player triggers the start action.
2. WHILE the Start_Screen is displayed, THE Game_Loop SHALL only render the Start_Screen and shall not process player movement or game state updates.

### Requirement 6: Game State Transition

**User Story:** As a developer, I want a clear state transition from start screen to gameplay, so that the game loop knows which rendering and update logic to execute.

#### Acceptance Criteria

1. THE Game_Loop SHALL support a Start_Screen state and a Playing_State as distinct phases of execution.
2. WHEN the game transitions from Start_Screen to Playing_State, THE Game_Loop SHALL initialize the player at the center of the Game_Canvas.
3. WHEN the game transitions from Start_Screen to Playing_State, THE Start_Screen SHALL no longer be rendered on the Game_Canvas.
