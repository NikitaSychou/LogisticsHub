import { ApiHttpError } from './api-http-error';
import { formatProblemError } from './problem-error.mapper';

describe('formatProblemError', () => {
  it('formats validation errors from structured ApiHttpError body data', () => {
    const error = new ApiHttpError(
      'Create resource',
      400,
      'Bad Request',
      {
        errors: {
          name: ['Name is required.'],
          externalCode: ['External code is required.'],
        },
      },
      ''
    );

    expect(formatProblemError(error, 'Create failed.')).toBe(
      'name: Name is required.\nexternalCode: External code is required.'
    );
  });

  it('uses the ApiHttpError message when the body is not ProblemDetails validation data', () => {
    const error = new ApiHttpError('Load data', 500, 'Server Error', 'Plain failure', 'Plain failure');

    expect(formatProblemError(error, 'Load failed.')).toBe('Load data returned 500: Plain failure');
  });

  it('does not parse ProblemDetails JSON from a plain Error message', () => {
    const error = new Error('{"errors":{"name":["Name is required."]}}');

    expect(formatProblemError(error, 'Create failed.')).toBe('{"errors":{"name":["Name is required."]}}');
  });

  it('uses the fallback for non-Error values', () => {
    expect(formatProblemError(null, 'Something failed.')).toBe('Something failed.');
  });
});
