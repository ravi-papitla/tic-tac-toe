import { HttpErrorResponse, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../environments/environment';
import { makeGameState } from '../testing/test-data';
import { GameApiService, describeError } from './game-api.service';

describe('GameApiService', () => {
  let service: GameApiService;
  let http: HttpTestingController;
  const base = environment.apiBaseUrl;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(GameApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('creates a game with the requested mode', () => {
    const state = makeGameState({ mode: 'VsComputer' });
    let result: unknown;
    service.createGame('VsComputer').subscribe((s) => (result = s));

    const req = http.expectOne(`${base}/games`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ mode: 'VsComputer' });
    req.flush(state);

    expect(result).toEqual(state);
  });

  it('gets a game by id', () => {
    service.getGame('abc').subscribe();

    const req = http.expectOne(`${base}/games/abc`);
    expect(req.request.method).toBe('GET');
    req.flush(makeGameState());
  });

  it('submits a move with player and cell index', () => {
    service.makeMove('abc', 'O', 4).subscribe();

    const req = http.expectOne(`${base}/games/abc/moves`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ player: 'O', cellIndex: 4 });
    req.flush(makeGameState());
  });

  it('calls undo and reset endpoints', () => {
    service.undo('abc').subscribe();
    const undo = http.expectOne(`${base}/games/abc/undo`);
    expect(undo.request.method).toBe('POST');
    undo.flush(makeGameState());

    service.resetGame('abc').subscribe();
    const reset = http.expectOne(`${base}/games/abc/reset`);
    expect(reset.request.method).toBe('POST');
    reset.flush(makeGameState());
  });

  it('reads and resets the scoreboard', () => {
    service.getScoreboard().subscribe();
    const get = http.expectOne(`${base}/scoreboard`);
    expect(get.request.method).toBe('GET');
    get.flush({ xWins: 1, oWins: 2, draws: 3 });

    service.resetScoreboard().subscribe();
    const reset = http.expectOne(`${base}/scoreboard/reset`);
    expect(reset.request.method).toBe('POST');
    reset.flush({ xWins: 0, oWins: 0, draws: 0 });
  });
});

describe('describeError', () => {
  it('uses the problem-details detail sent by the backend', () => {
    const error = new HttpErrorResponse({ status: 409, error: { detail: 'Cell 0 is already taken.' } });
    expect(describeError(error)).toBe('Cell 0 is already taken.');
  });

  it('explains when the backend cannot be reached', () => {
    const error = new HttpErrorResponse({ status: 0 });
    expect(describeError(error)).toContain('Cannot reach the game server');
  });

  it('falls back to a generic message for unknown errors', () => {
    expect(describeError(new Error('boom'))).toBe('Something went wrong. Please try again.');
  });
});
