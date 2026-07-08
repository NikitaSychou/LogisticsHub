import { RuntimeConfig, resolveRuntimeConfig } from './runtime-config';

describe('resolveRuntimeConfig', () => {
  const fallbackConfig: RuntimeConfig = {
    api: {
      gatewayBaseUrl: 'http://localhost:5100',
      scope: 'api://fallback/access_as_user',
    },
    msal: {
      clientId: 'fallback-client-id',
      authority: 'https://login.microsoftonline.com/fallback-tenant',
      redirectUri: 'http://localhost:4200',
    },
  };

  const validConfig: RuntimeConfig = {
    api: {
      gatewayBaseUrl: 'https://gateway.example.test',
      scope: 'api://runtime/access_as_user',
    },
    msal: {
      clientId: 'runtime-client-id',
      authority: 'https://login.microsoftonline.com/runtime-tenant',
      redirectUri: 'https://app.example.test',
    },
  };

  it('accepts a valid runtime config response', async () => {
    const warnings: string[] = [];

    const config = await resolveRuntimeConfig({
      fallback: fallbackConfig,
      loadResponse: async () => jsonResponse(validConfig),
      warn: (reason) => warnings.push(reason),
    });

    expect(config).toEqual(validConfig);
    expect(warnings).toEqual([]);
  });

  it('falls back quietly when runtime config is missing', async () => {
    const warnings: string[] = [];

    const config = await resolveRuntimeConfig({
      fallback: fallbackConfig,
      loadResponse: async () => textResponse('', 404),
      warn: (reason) => warnings.push(reason),
    });

    expect(config).toBe(fallbackConfig);
    expect(warnings).toEqual([]);
  });

  it('falls back and warns when runtime config JSON is invalid', async () => {
    const warnings: string[] = [];

    const config = await resolveRuntimeConfig({
      fallback: fallbackConfig,
      loadResponse: async () => textResponse('{', 200),
      warn: (reason) => warnings.push(reason),
    });

    expect(config).toBe(fallbackConfig);
    expect(warnings).toEqual(['Runtime config JSON could not be parsed.']);
  });

  it('falls back and warns when runtime config shape is invalid', async () => {
    const warnings: string[] = [];

    const config = await resolveRuntimeConfig({
      fallback: fallbackConfig,
      loadResponse: async () => jsonResponse({ api: validConfig.api }),
      warn: (reason) => warnings.push(reason),
    });

    expect(config).toBe(fallbackConfig);
    expect(warnings).toEqual(['Runtime config has an invalid shape.']);
  });

  it('falls back and warns for unexpected non-404 HTTP responses', async () => {
    const warnings: string[] = [];

    const config = await resolveRuntimeConfig({
      fallback: fallbackConfig,
      loadResponse: async () => textResponse('Forbidden', 403),
      warn: (reason) => warnings.push(reason),
    });

    expect(config).toBe(fallbackConfig);
    expect(warnings).toEqual(['Runtime config request failed with HTTP 403.']);
  });

  it('falls back and warns when runtime config cannot be loaded', async () => {
    const warnings: string[] = [];

    const config = await resolveRuntimeConfig({
      fallback: fallbackConfig,
      loadResponse: async () => {
        throw new Error('Network unavailable');
      },
      warn: (reason) => warnings.push(reason),
    });

    expect(config).toBe(fallbackConfig);
    expect(warnings).toEqual(['Runtime config could not be loaded or parsed.']);
  });
});

function jsonResponse(body: unknown, status = 200): Response {
  return textResponse(JSON.stringify(body), status);
}

function textResponse(body: string, status: number): Response {
  return new Response(body, { status });
}
