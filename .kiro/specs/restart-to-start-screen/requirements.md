# Requirements Document

## Introduction

This document specifies the requirements for fixing two bugs in the Frogmageddon game loop: (1) RestartGame should navigate to the start screen instead of directly to gameplay, and (2) the start screen should render continuously so frog sprites appear after async loading completes.

## Glossary

- **GameLoop**: The central game engine class that manages phase transitions, input handling, and rendering each tick.
- **StartScreen**: The initial game phase showing the title, frog sprite, and start/instructions buttons.
- **RestartGame**: The method that resets all game state and transitions the player to a new session.
- **GamePhase**: An enum representing the current phase of the game (StartScreen, Instructions, Playing, Paused, GameOver).
- **RenderStartScreenAsync**: The renderer method that draws the start screen to the canvas.

## Requirements

### Requirement 1: Restart navigates to start screen

**User Story:** As a player, I want restarting the game to return me to the start screen, so that I see the title and can choose when to begin a new session.

#### Acceptance Criteria

1. WHEN RestartGame is invoked, THE GameLoop SHALL set the current phase to GamePhase.StartScreen
2. WHEN RestartGame is invoked, THE GameLoop SHALL set the last rendered phase to null to allow immediate re-rendering
3. WHEN RestartGame is invoked, THE GameLoop SHALL reset player health to 100 and score to 0
4. WHEN RestartGame is invoked, THE GameLoop SHALL clear all frogs and bullets and return them to their respective pools

### Requirement 2: Start screen renders continuously

**User Story:** As a player, I want the start screen to display frog sprites as soon as they finish loading, so that the screen looks complete regardless of async load timing.

#### Acceptance Criteria

1. WHILE the current phase is StartScreen and the game is running, THE GameLoop SHALL invoke RenderStartScreenAsync every tick
2. WHILE the current phase is StartScreen, THE GameLoop SHALL NOT gate rendering behind a last-rendered-phase check
