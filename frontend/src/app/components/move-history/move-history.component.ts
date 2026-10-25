import { Component, input } from '@angular/core';
import { MoveRecord } from '../../models/game.models';

@Component({
  selector: 'app-move-history',
  standalone: true,
  templateUrl: './move-history.component.html',
  styleUrl: './move-history.component.css',
})
export class MoveHistoryComponent {
  readonly moves = input<MoveRecord[]>([]);
}
