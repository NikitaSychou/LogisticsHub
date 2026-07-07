import { ProblemDetails } from './problem-details.models';

export function formatProblemError(error: unknown, fallback: string): string {
  if (!(error instanceof Error)) {
    return fallback;
  }

  const problem = tryExtractProblemDetails(error.message);
  return problem ?? error.message;
}

function tryExtractProblemDetails(message: string): string | null {
  const jsonStart = message.indexOf('{');
  if (jsonStart < 0) {
    return null;
  }

  const parsed = parseJson(message.substring(jsonStart));
  const record = asRecord(parsed) as ProblemDetails;
  const errors = asRecord(record.errors);
  const details = Object.entries(errors)
    .flatMap(([field, value]) =>
      Array.isArray(value)
        ? value.map((item) => `${field}: ${String(item)}`)
        : []
    );

  return details.length > 0 ? details.join('\n') : null;
}

function parseJson(value: string): unknown {
  try {
    return JSON.parse(value);
  } catch {
    return value;
  }
}

function asRecord(value: unknown): Record<string, unknown> {
  return value !== null && typeof value === 'object' ? (value as Record<string, unknown>) : {};
}
