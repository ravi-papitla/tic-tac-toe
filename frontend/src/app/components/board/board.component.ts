import { Component, input, output } from '@angular/core';
import { Player } from '../../models/game.models';

@Component({
  selector: 'app-board',
  standalone: true,
  templateUrl: './board.component.html',
  styleUrl: './board.component.css',
})
export class BoardComponent {
  readonly board = input.required<(Player | null)[]>();
  readonly winningCells = input<number[]>([]);
  /** True while a request is pending or the game is over: no cell can be clicked. */
  readonly disabled = input(false);

  readonly cellSelected = output<number>();

  isWinning(index: number): boolean {
    return this.winningCells().includes(index);
  }

  label(index: number, cell: Player | null): string {
    const row = Math.floor(index / 3) + 1;
    const column = (index % 3) + 1;
    return `Row ${row}, Column ${column}, ${cell ?? 'empty'}`;
  }
}
