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

  async getJson<T>(path: string, label: string): Promise<T> {
    return this.requestJson<T>('GET', path, label);
  }

  async postJson<T>(path: string, body: unknown, label: string): Promise<T> {
    return this.requestJson<T>('POST', path, label, body);
  }

  private async requestJson<T>(method: 'GET' | 'POST', path: string, label: string, body?: unknown): Promise<T> {
    const accessToken = await this.authContext.getAccessToken();

    try {
      return await firstValueFrom(
        this.http.request<T>(method, `${gatewayBaseUrl()}${path}`, {
          body,
          headers: new HttpHeaders({
            Authorization: `Bearer ${accessToken}`,
          }),
          responseType: 'json',
        })
      );
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
