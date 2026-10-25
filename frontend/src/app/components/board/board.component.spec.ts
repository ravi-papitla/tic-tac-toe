import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Player } from '../../models/game.models';
import { BoardComponent } from './board.component';

describe('BoardComponent', () => {
  let fixture: ComponentFixture<BoardComponent>;

  const emptyBoard = (): (Player | null)[] => Array<null>(9).fill(null);
  const cells = (): HTMLButtonElement[] =>
    Array.from((fixture.nativeElement as HTMLElement).querySelectorAll<HTMLButtonElement>('button.cell'));

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [BoardComponent] });
    fixture = TestBed.createComponent(BoardComponent);
  });

  it('renders a 3x3 grid of enabled empty cells', () => {
    fixture.componentRef.setInput('board', emptyBoard());
    fixture.detectChanges();

    expect(cells().length).toBe(9);
    expect(cells().every((c) => !c.disabled && c.textContent?.trim() === '')).toBeTrue();
  });

  it('shows X and O and locks occupied cells', () => {
    const board = emptyBoard();
    board[0] = 'X';
    board[4] = 'O';
    fixture.componentRef.setInput('board', board);
    fixture.detectChanges();

    expect(cells()[0].textContent?.trim()).toBe('X');
    expect(cells()[4].textContent?.trim()).toBe('O');
    expect(cells()[0].disabled).toBeTrue();
    expect(cells()[4].disabled).toBeTrue();
    expect(cells()[1].disabled).toBeFalse();
  });

  it('emits the cell index when an empty cell is clicked', () => {
    fixture.componentRef.setInput('board', emptyBoard());
    fixture.detectChanges();
    const selected: number[] = [];
    fixture.componentInstance.cellSelected.subscribe((i) => selected.push(i));

    cells()[7].click();

    expect(selected).toEqual([7]);
  });

  it('disables every cell when the board is disabled', () => {
    fixture.componentRef.setInput('board', emptyBoard());
    fixture.componentRef.setInput('disabled', true);
    fixture.detectChanges();

    expect(cells().every((c) => c.disabled)).toBeTrue();
  });

  it('highlights the winning cells', () => {
    const board = emptyBoard();
    [0, 1, 2].forEach((i) => (board[i] = 'X'));
    fixture.componentRef.setInput('board', board);
    fixture.componentRef.setInput('winningCells', [0, 1, 2]);
    fixture.detectChanges();

    const highlighted = cells()
      .map((c, i) => (c.classList.contains('winning') ? i : -1))
      .filter((i) => i >= 0);
    expect(highlighted).toEqual([0, 1, 2]);
  });

  it('describes cells for screen readers', () => {
    fixture.componentRef.setInput('board', emptyBoard());
    fixture.detectChanges();

    expect(cells()[0].getAttribute('aria-label')).toBe('Row 1, Column 1, empty');
    expect(cells()[5].getAttribute('aria-label')).toBe('Row 2, Column 3, empty');
  });
});
