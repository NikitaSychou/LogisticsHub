import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-empty-state',
  templateUrl: './empty-state.html',
})
export class EmptyState {
  @Input({ required: true }) title = '';
  @Input({ required: true }) message = '';
}
