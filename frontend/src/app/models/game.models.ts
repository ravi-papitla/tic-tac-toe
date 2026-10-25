export type Player = 'X' | 'O';
export type GameMode = 'TwoPlayer' | 'VsComputer';
export type GameStatus = 'InProgress' | 'Won' | 'Draw';

export interface MoveRecord {
  moveNumber: number;
  player: Player;
  /** 1-based */
  row: number;
  /** 1-based */
  column: number;
  /** 0-8, row-major */
  cellIndex: number;
  position: string;
}

export interface Scoreboard {
  xWins: number;
  oWins: number;
  draws: number;
}

/** Mirrors the backend GameStateResponse. The backend is the single source of truth. */
export interface GameState {
  gameId: string;
  board: (Player | null)[];
  /** null once the game is completed */
  currentPlayer: Player | null;
  mode: GameMode;
  status: GameStatus;
  winner: Player | null;
  winningCells: number[];
  moves: MoveRecord[];
  canUndo: boolean;
  scoreboard: Scoreboard;
}
