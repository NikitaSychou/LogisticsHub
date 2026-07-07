import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
  selector: 'app-error-alert',
  imports: [CommonModule],
  templateUrl: './error-alert.html',
  styleUrl: './error-alert.css',
})
export class ErrorAlert {
  @Input({ required: true }) message = '';
  @Input() retryLabel = 'Retry';
  @Input() retryingLabel = 'Retrying...';
  @Input() retrying = false;
  @Input() retryDisabled = false;

  @Output() retry = new EventEmitter<void>();

  protected get hasRetryAction(): boolean {
    return this.retry.observed;
  }
}
