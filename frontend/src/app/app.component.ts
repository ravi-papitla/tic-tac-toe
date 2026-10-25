import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { Observable } from 'rxjs';
import { BoardComponent } from './components/board/board.component';
import { MoveHistoryComponent } from './components/move-history/move-history.component';
import { ScoreboardComponent } from './components/scoreboard/scoreboard.component';
import { GameMode, GameState, MoveRecord, Scoreboard } from './models/game.models';
import { GameApiService, describeError } from './services/game-api.service';

const STORAGE_KEY = 'tictactoe.gameId';
const NO_MOVES: MoveRecord[] = [];

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [BoardComponent, MoveHistoryComponent, ScoreboardComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css',
})
export class AppComponent implements OnInit {
  private readonly api = inject(GameApiService);

  /** Latest state returned by the backend. The UI only renders this; it never computes game rules. */
  readonly game = signal<GameState | null>(null);
  readonly scoreboard = signal<Scoreboard>({ xWins: 0, oWins: 0, draws: 0 });
  readonly mode = signal<GameMode>('TwoPlayer');
  readonly busy = signal(false);
  readonly error = signal<string | null>(null);

  readonly modeLabel = computed(() => (this.mode() === 'VsComputer' ? 'Play Against Computer' : 'Two Player Mode'));
  readonly moves = computed(() => this.game()?.moves ?? NO_MOVES);
  readonly isCompleted = computed(() => {
    const status = this.game()?.status;
    return status === 'Won' || status === 'Draw';
  });
  readonly boardDisabled = computed(() => this.busy() || this.game() === null || this.isCompleted());
  readonly canUndo = computed(() => !this.busy() && this.game()?.canUndo === true);

  readonly headline = computed(() => {
    const game = this.game();
    if (!game) return 'Loading…';
    if (game.status === 'Draw') return "It's a draw!";
    if (game.status === 'Won') {
      const who = game.mode === 'VsComputer' ? (game.winner === 'X' ? ' (you)' : ' (computer)') : '';
      return `Player ${game.winner}${who} wins! 🎉`;
    }
    return `Player ${game.currentPlayer}'s turn`;
  });

  ngOnInit(): void {
    const savedId = this.readSavedGameId();
    if (savedId) {
      // Resume the session after a page refresh; fall back to a new game if the backend no longer knows it.
      this.run(this.api.getGame(savedId), (state) => this.applyState(state), () => this.startNewGame('TwoPlayer'));
    } else {
      this.startNewGame('TwoPlayer');
    }
  }

  startNewGame(mode: GameMode): void {
    this.run(this.api.createGame(mode), (state) => this.applyState(state));
  }

  onModeSelected(mode: GameMode): void {
    if (this.busy() || mode === this.game()?.mode) return;
    this.startNewGame(mode); // a game is bound to one mode, so switching mode starts a new game
  }

  onCellSelected(cellIndex: number): void {
    const game = this.game();
    if (!game || this.boardDisabled() || game.board[cellIndex] !== null || game.currentPlayer === null) return;
    this.run(this.api.makeMove(game.gameId, game.currentPlayer, cellIndex), (state) => this.applyState(state));
  }

  undo(): void {
    const game = this.game();
    if (!game || !this.canUndo()) return;
    this.run(this.api.undo(game.gameId), (state) => this.applyState(state));
  }

  resetGame(): void {
    const game = this.game();
    if (!game || this.busy()) return;
    this.run(this.api.resetGame(game.gameId), (state) => this.applyState(state));
  }

  resetScoreboard(): void {
    if (this.busy()) return;
    this.run(this.api.resetScoreboard(), (scoreboard) => this.scoreboard.set(scoreboard));
  }

  dismissError(): void {
    this.error.set(null);
  }

  private applyState(state: GameState): void {
    this.game.set(state);
    this.mode.set(state.mode);
    this.scoreboard.set(state.scoreboard);
    this.saveGameId(state.gameId);
  }

  /** Runs one backend call, tracking the busy flag and surfacing errors. */
  private run<T>(request$: Observable<T>, onSuccess: (value: T) => void, onFailure?: (error: unknown) => void): void {
    this.busy.set(true);
    this.error.set(null);
    request$.subscribe({
      next: (value) => {
        this.busy.set(false);
        onSuccess(value);
      },
      error: (err: unknown) => {
        this.busy.set(false);
        if (onFailure) {
          onFailure(err);
        } else {
          this.error.set(describeError(err));
        }
      },
    });
  }

  private readSavedGameId(): string | null {
    try {
      return sessionStorage.getItem(STORAGE_KEY);
    } catch {
      return null; // storage can be unavailable (privacy mode); the app still works
    }
  }

  private saveGameId(gameId: string): void {
    try {
      sessionStorage.setItem(STORAGE_KEY, gameId);
    } catch {
      // ignore: resuming after refresh is a convenience only
    }
  }
}
