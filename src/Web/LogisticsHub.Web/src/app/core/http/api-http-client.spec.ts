import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, TestRequest, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ApiAuthContext } from './api-auth-context';
import { ApiHttpClient } from './api-http-client';
import { ApiHttpError } from './api-http-error';

describe('ApiHttpClient', () => {
  let authContext: ApiAuthContext;
  let client: ApiHttpClient;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });

    authContext = TestBed.inject(ApiAuthContext);
    client = TestBed.inject(ApiHttpClient);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
  });

  it('sends GET requests to the configured Gateway with an authorization header', async () => {
    authContext.configure(null, async () => 'access-token');

    const response = client.getJson<{ value: string }>('/resource', 'Load resource');
    const request = await expectRequest('http://localhost:5100/resource');

    expect(request.request.method).toBe('GET');
    expect(request.request.headers.get('Authorization')).toBe('Bearer access-token');

    request.flush({ value: 'loaded' });

    await expect(response).resolves.toEqual({ value: 'loaded' });
  });

  it('sends POST requests with a JSON body and returns typed JSON data', async () => {
    authContext.configure(null, async () => 'access-token');
    const body = { name: 'Resource' };

    const response = client.postJson<{ id: string }>('/resource', body, 'Create resource');
    const request = await expectRequest('http://localhost:5100/resource');

    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual(body);
    expect(request.request.headers.get('Authorization')).toBe('Bearer access-token');

    request.flush({ id: 'RESOURCE-1' });

    await expect(response).resolves.toEqual({ id: 'RESOURCE-1' });
  });

  it('does not send a request when access token acquisition fails', async () => {
    authContext.configure(null, async () => {
      throw new Error('Token unavailable.');
    });

    await expect(client.getJson('/resource', 'Load resource')).rejects.toThrow('Token unavailable.');

    http.expectNone('http://localhost:5100/resource');
  });

  it('converts structured HTTP failures to ApiHttpError with body and raw body preserved', async () => {
    authContext.configure(null, async () => 'access-token');
    const problemBody = {
      title: 'Validation failed',
      errors: {
        name: ['Name is required.'],
      },
    };

    const response = client.postJson('/resource', { name: '' }, 'Create resource');
    const request = await expectRequest('http://localhost:5100/resource');
    request.flush(problemBody, { status: 400, statusText: 'Bad Request' });

    const error = await rejectedWithApiHttpError(response);

    expect(error.label).toBe('Create resource');
    expect(error.status).toBe(400);
    expect(error.statusText).toBe('Bad Request');
    expect(error.body).toEqual(problemBody);
    expect(error.rawBody).toBe(JSON.stringify(problemBody));
    expect(error.message).toContain('Create resource returned 400');
  });

  it('converts string HTTP failures to ApiHttpError and parses JSON problem details when possible', async () => {
    authContext.configure(null, async () => 'access-token');
    const problemJson = JSON.stringify({
      errors: {
        quantity: ['Quantity must be positive.'],
      },
    });

    const response = client.getJson('/resource', 'Load resource');
    const request = await expectRequest('http://localhost:5100/resource');
    request.flush(problemJson, { status: 422, statusText: 'Unprocessable Entity' });

    const error = await rejectedWithApiHttpError(response);

    expect(error.status).toBe(422);
    expect(error.body).toEqual({ errors: { quantity: ['Quantity must be positive.'] } });
    expect(error.rawBody).toBe(problemJson);
  });
});

async function expectRequest(url: string): Promise<TestRequest> {
  for (let attempt = 0; attempt < 10; attempt += 1) {
    await Promise.resolve();

    const requests = TestBed.inject(HttpTestingController).match(url);
    if (requests.length === 1) {
      return requests[0];
    }

    if (requests.length > 1) {
      throw new Error(`Expected one request to ${url}, found ${requests.length}.`);
    }
  }

  throw new Error(`Expected one request to ${url}, found none.`);
}

async function rejectedWithApiHttpError(promise: Promise<unknown>): Promise<ApiHttpError> {
  try {
    await promise;
  } catch (error) {
    if (error instanceof ApiHttpError) {
      return error;
    }

    throw new Error(`Expected ApiHttpError, received ${String(error)}.`);
  }

  throw new Error('Expected request to fail with ApiHttpError.');
}
