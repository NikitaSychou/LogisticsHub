import { ApiHttpError } from './api-http-error';
import { ProblemDetails } from './problem-details.models';

export function formatProblemError(error: unknown, fallback: string): string {
  if (error instanceof ApiHttpError) {
    return tryFormatProblemDetails(error.body) ?? error.message;
  }

  if (!(error instanceof Error)) {
    return fallback;
  }

  return error.message;
}

function tryFormatProblemDetails(body: unknown): string | null {
  const record = asRecord(body) as ProblemDetails;
  const errors = asRecord(record.errors);
  const details = Object.entries(errors)
    .flatMap(([field, value]) =>
      Array.isArray(value)
        ? value.map((item) => `${field}: ${String(item)}`)
        : []
    );

  return details.length > 0 ? details.join('\n') : null;
}

function asRecord(value: unknown): Record<string, unknown> {
  return value !== null && typeof value === 'object' ? (value as Record<string, unknown>) : {};
}
