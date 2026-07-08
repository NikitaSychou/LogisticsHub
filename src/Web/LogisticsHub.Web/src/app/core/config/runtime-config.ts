import { Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';

export interface RuntimeConfig {
  api: {
    gatewayBaseUrl: string;
    scope: string;
  };
  msal: {
    clientId: string;
    authority: string;
    redirectUri: string;
  };
}

const RUNTIME_CONFIG_PATH = '/runtime-config.json';
const defaultRuntimeConfig: RuntimeConfig = environment;

let activeRuntimeConfig = defaultRuntimeConfig;

@Injectable({ providedIn: 'root' })
export class RuntimeConfigService {
  get config(): RuntimeConfig {
    return activeRuntimeConfig;
  }
}

export async function loadRuntimeConfig(): Promise<void> {
  try {
    const response = await fetch(RUNTIME_CONFIG_PATH, { cache: 'no-store' });
    if (!response.ok) {
      if (response.status !== 404) {
        warnFallback(`Runtime config request failed with HTTP ${response.status}.`);
      }

      return;
    }

    const responseBody = await responseJson(response);
    if (!responseBody.ok) {
      warnFallback('Runtime config JSON could not be parsed.');
      return;
    }

    const runtimeConfig = toRuntimeConfig(responseBody.value);
    if (runtimeConfig) {
      activeRuntimeConfig = runtimeConfig;
      return;
    }

    warnFallback('Runtime config has an invalid shape.');
  } catch {
    warnFallback('Runtime config could not be loaded or parsed.');
  }
}

export function runtimeConfig(): RuntimeConfig {
  return activeRuntimeConfig;
}

function toRuntimeConfig(value: unknown): RuntimeConfig | null {
  if (!isRecord(value)) {
    return null;
  }

  const api = isRecord(value['api']) ? value['api'] : null;
  const msal = isRecord(value['msal']) ? value['msal'] : null;
  if (!api || !msal) {
    return null;
  }

  const config: RuntimeConfig = {
    api: {
      gatewayBaseUrl: stringValue(api['gatewayBaseUrl']),
      scope: stringValue(api['scope']),
    },
    msal: {
      clientId: stringValue(msal['clientId']),
      authority: stringValue(msal['authority']),
      redirectUri: stringValue(msal['redirectUri']),
    },
  };

  return isCompleteRuntimeConfig(config) ? config : null;
}

function isCompleteRuntimeConfig(config: RuntimeConfig): boolean {
  return Boolean(
    config.api.gatewayBaseUrl &&
      config.api.scope &&
      config.msal.clientId &&
      config.msal.authority &&
      config.msal.redirectUri
  );
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null;
}

async function responseJson(response: Response): Promise<{ ok: true; value: unknown } | { ok: false }> {
  try {
    return { ok: true, value: await response.json() };
  } catch {
    return { ok: false };
  }
}

function stringValue(value: unknown): string {
  return typeof value === 'string' ? value.trim() : '';
}

function warnFallback(reason: string): void {
  console.warn(`${reason} Falling back to local defaults.`);
}
