import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
  selector: 'app-load-more-state',
  imports: [CommonModule],
  templateUrl: './load-more-state.html',
})
export class LoadMoreState {
  @Input({ required: true }) visible = false;
  @Input({ required: true }) hasMore = false;
  @Input({ required: true }) loadingMore = false;
  @Input({ required: true }) disabled = false;
  @Input({ required: true }) allLoadedMessage = '';
  @Input({ required: true }) pageNumber = 0;
  @Input({ required: true }) pageSize = 0;

  @Output() loadMore = new EventEmitter<void>();
}
