import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { AppComponent } from './app.component';
import { GameApiService } from './services/game-api.service';
import { makeGameState } from './testing/test-data';

describe('AppComponent', () => {
  let fixture: ComponentFixture<AppComponent>;
  let api: jasmine.SpyObj<GameApiService>;

  const root = (): HTMLElement => fixture.nativeElement as HTMLElement;
  const byTestId = (id: string): HTMLElement => root().querySelector<HTMLElement>(`[data-testid="${id}"]`)!;
  const cells = (): HTMLButtonElement[] => Array.from(root().querySelectorAll<HTMLButtonElement>('button.cell'));

  function create(): void {
    fixture = TestBed.createComponent(AppComponent);
    fixture.detectChanges(); // runs ngOnInit
    fixture.detectChanges();
  }

  beforeEach(() => {
    sessionStorage.clear();
    api = jasmine.createSpyObj<GameApiService>('GameApiService', [
      'createGame',
      'getGame',
      'makeMove',
      'undo',
      'resetGame',
      'getScoreboard',
      'resetScoreboard',
    ]);
    api.createGame.and.returnValue(of(makeGameState()));
    TestBed.configureTestingModule({ imports: [AppComponent], providers: [{ provide: GameApiService, useValue: api }] });
  });

  afterEach(() => sessionStorage.clear());

  it('creates a two-player game on start and renders the board, turn and scoreboard', () => {
    create();

    expect(api.createGame).toHaveBeenCalledOnceWith('TwoPlayer');
    expect(cells().length).toBe(9);
    expect(byTestId('status').textContent).toContain("Player X's turn");
    expect(byTestId('mode-label').textContent).toContain('Two Player Mode');
    expect(byTestId('x-wins').textContent).toBe('0');
  });

  it('sends the current player and cell to the backend when a cell is clicked and renders the response', () => {
    const board = Array<'X' | 'O' | null>(9).fill(null);
    board[4] = 'X';
    api.makeMove.and.returnValue(
      of(
        makeGameState({
          board,
          currentPlayer: 'O',
          canUndo: true,
          moves: [{ moveNumber: 1, player: 'X', row: 2, column: 2, cellIndex: 4, position: 'Row 2, Column 2' }],
        }),
      ),
    );
    create();

    cells()[4].click();
    fixture.detectChanges();

    expect(api.makeMove).toHaveBeenCalledOnceWith('game-1', 'X', 4);
    expect(cells()[4].textContent?.trim()).toBe('X');
    expect(byTestId('status').textContent).toContain("Player O's turn");
    expect(root().querySelector('app-move-history tbody tr')?.textContent).toContain('Row 2, Column 2');
  });

  it('enables Undo only when the backend says undo is possible', () => {
    create();
    expect((byTestId('undo') as HTMLButtonElement).disabled).toBeTrue();

    api.createGame.and.returnValue(of(makeGameState({ canUndo: true, currentPlayer: 'O' })));
    byTestId('mode-computer').click();
    fixture.detectChanges();

    expect((byTestId('undo') as HTMLButtonElement).disabled).toBeFalse();
  });

  it('shows the winner, highlights winning cells and locks the board after a win', () => {
    const board = Array<'X' | 'O' | null>(9).fill(null);
    [0, 1, 2].forEach((i) => (board[i] = 'X'));
    api.createGame.and.returnValue(
      of(makeGameState({ board, status: 'Won', winner: 'X', winningCells: [0, 1, 2], currentPlayer: null })),
    );

    create();

    expect(byTestId('status').textContent).toContain('Player X wins');
    expect(root().querySelectorAll('button.cell.winning').length).toBe(3);
    expect(cells().every((c) => c.disabled)).toBeTrue();
    expect((byTestId('undo') as HTMLButtonElement).disabled).toBeTrue();
  });

  it('shows a draw message', () => {
    api.createGame.and.returnValue(of(makeGameState({ status: 'Draw', currentPlayer: null })));

    create();

    expect(byTestId('status').textContent).toContain('draw');
  });

  it('starts a new game in the other mode and labels the players', () => {
    create();
    api.createGame.and.returnValue(of(makeGameState({ mode: 'VsComputer', gameId: 'game-2' })));

    byTestId('mode-computer').click();
    fixture.detectChanges();

    expect(api.createGame).toHaveBeenCalledWith('VsComputer');
    expect(byTestId('mode-label').textContent).toContain('Play Against Computer');
    expect(byTestId('chip-o').textContent).toContain('computer');
  });

  it('keeps the scoreboard from the backend when the game is reset', () => {
    create();
    const afterReset = makeGameState({ scoreboard: { xWins: 2, oWins: 1, draws: 0 } });
    api.resetGame.and.returnValue(of(afterReset));

    byTestId('reset-game').click();
    fixture.detectChanges();

    expect(api.resetGame).toHaveBeenCalledOnceWith('game-1');
    expect(byTestId('x-wins').textContent).toBe('2');
    expect(byTestId('o-wins').textContent).toBe('1');
  });

  it('resets the scoreboard through the backend', () => {
    api.createGame.and.returnValue(of(makeGameState({ scoreboard: { xWins: 3, oWins: 0, draws: 1 } })));
    create();
    api.resetScoreboard.and.returnValue(of({ xWins: 0, oWins: 0, draws: 0 }));

    byTestId('reset-scoreboard').click();
    fixture.detectChanges();

    expect(api.resetScoreboard).toHaveBeenCalled();
    expect(byTestId('x-wins').textContent).toBe('0');
    expect(byTestId('draws').textContent).toBe('0');
  });

  it('shows the backend error message when a move is rejected', () => {
    api.makeMove.and.returnValue(
      throwError(() => new HttpErrorResponse({ status: 409, error: { detail: 'Cell 4 is already taken.' } })),
    );
    create();

    cells()[4].click();
    fixture.detectChanges();

    expect(byTestId('error').textContent).toContain('Cell 4 is already taken.');
    expect(byTestId('status').textContent).toContain("Player X's turn"); // state unchanged
  });

  it('resumes a saved game after a refresh', () => {
    sessionStorage.setItem('tictactoe.gameId', 'saved-id');
    api.getGame.and.returnValue(of(makeGameState({ gameId: 'saved-id', currentPlayer: 'O' })));

    create();

    expect(api.getGame).toHaveBeenCalledOnceWith('saved-id');
    expect(api.createGame).not.toHaveBeenCalled();
    expect(byTestId('status').textContent).toContain("Player O's turn");
  });

  it('starts a new game when the saved game no longer exists on the backend', () => {
    sessionStorage.setItem('tictactoe.gameId', 'stale-id');
    api.getGame.and.returnValue(throwError(() => new HttpErrorResponse({ status: 404 })));

    create();

    expect(api.createGame).toHaveBeenCalledOnceWith('TwoPlayer');
  });
});
