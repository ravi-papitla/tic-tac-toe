import { Component, input } from '@angular/core';
import { Scoreboard } from '../../models/game.models';

@Component({
  selector: 'app-scoreboard',
  standalone: true,
  templateUrl: './scoreboard.component.html',
  styleUrl: './scoreboard.component.css',
})
export class ScoreboardComponent {
  readonly scoreboard = input.required<Scoreboard>();
}
