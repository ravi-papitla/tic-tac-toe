import { GameState } from '../models/game.models';

export function makeGameState(overrides: Partial<GameState> = {}): GameState {
  return {
    gameId: 'game-1',
    board: Array<null>(9).fill(null),
    currentPlayer: 'X',
    mode: 'TwoPlayer',
    status: 'InProgress',
    winner: null,
    winningCells: [],
    moves: [],
    canUndo: false,
    scoreboard: { xWins: 0, oWins: 0, draws: 0 },
    ...overrides,
  };
}
