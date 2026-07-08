import { runtimeConfig } from '../config/runtime-config';

export function gatewayBaseUrl(): string {
  return runtimeConfig().api.gatewayBaseUrl;
}
