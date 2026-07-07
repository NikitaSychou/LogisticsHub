export class ApiHttpError extends Error {
  constructor(
    readonly label: string,
    readonly status: number,
    readonly statusText: string,
    readonly body: unknown,
    readonly rawBody: string
  ) {
    super(`${label} returned ${status}: ${rawBody || statusText}`);
    this.name = 'ApiHttpError';
  }
}
