import { HttpClient, HttpErrorResponse, HttpHeaders } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiAuthContext } from './api-auth-context';
import { ApiHttpError } from './api-http-error';
import { gatewayBaseUrl } from './api-config';

@Injectable({ providedIn: 'root' })
export class ApiHttpClient {
  private readonly http = inject(HttpClient);
  private readonly authContext = inject(ApiAuthContext);

  async get(path: string, label: string): Promise<string> {
    return this.request('GET', path, label);
  }

  async post(path: string, body: unknown, label: string): Promise<string> {
    return this.request('POST', path, label, body);
  }

  private async request(method: 'GET' | 'POST', path: string, label: string, body?: unknown): Promise<string> {
    const accessToken = await this.authContext.getAccessToken();

    try {
      const response = await firstValueFrom(
        this.http.request(method, `${gatewayBaseUrl}${path}`, {
          body,
          headers: new HttpHeaders({
            Authorization: `Bearer ${accessToken}`,
          }),
          responseType: 'text',
        })
      );

      return response ?? '';
    } catch (error) {
      throw this.toApiError(error, label);
    }
  }

  private toApiError(error: unknown, label: string): Error {
    if (error instanceof HttpErrorResponse) {
      const rawBody = this.toRawBody(error.error);
      return new ApiHttpError(label, error.status, error.statusText, this.toBody(error.error), rawBody);
    }

    return error instanceof Error ? error : new Error(`${label} failed.`);
  }

  private toBody(value: unknown): unknown {
    if (typeof value !== 'string') {
      return value;
    }

    try {
      return JSON.parse(value);
    } catch {
      return value;
    }
  }

  private toRawBody(value: unknown): string {
    if (typeof value === 'string') {
      return value;
    }

    if (value === null || value === undefined) {
      return '';
    }

    return JSON.stringify(value);
  }
}
