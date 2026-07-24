# Angular Azure Static Hosting

This checklist documents the production hosting model for the Angular frontend.

## Hosting Model

- Build `src/Web/LogisticsHub.Web` as static Angular assets.
- Publish the built assets to Azure Storage Static Website.
- Put Azure Front Door or CDN in front of the storage static website for HTTPS, custom domain, routing, and cache policy control.
- Host Gateway/backend services separately. The Angular app should not be served from the Gateway container in this deployment model.

## Runtime Config

The Angular app loads `/runtime-config.json` before MSAL and Gateway API setup. The checked-in file under `src/Web/LogisticsHub.Web/public/runtime-config.json` is a local-development default and must be replaced or generated during production deployment.

Production `runtime-config.json` must provide:

- production Gateway base URL;
- MSAL client ID;
- MSAL authority;
- SPA redirect URI;
- API scope used for Gateway access tokens.

`runtime-config.json` must not contain secrets. Client IDs, authorities, redirect URIs, and scopes are public app configuration; client secrets do not belong in the SPA.

Deployment should fail or clearly alert if production `runtime-config.json` is missing, malformed, or still points to `localhost`.

## Caching

- Serve `runtime-config.json` with `Cache-Control: no-store`, `no-cache`, or a very short TTL.
- Hashed Angular build assets can use a long cache TTL.
- Ensure Front Door/CDN rules do not accidentally cache stale `runtime-config.json` for long periods.

## Microsoft Entra Checklist

- Register the final Front Door/custom domain URL as a SPA redirect URI.
- Review the post-logout redirect URI for the same production origin.
- Ensure the API scope in `runtime-config.json` matches the backend expected scope configuration.
- Do not invent or hardcode tenant, client, or scope values in deployment scripts. Use the configured Entra app registrations.

## Gateway/CORS Checklist

If the Angular frontend and Gateway use different origins, Gateway production CORS must explicitly allow the frontend origin.

- Allow only the real frontend origin: the dev-free Storage Static Website origin now, or the future Front Door/custom domain origin.
- Configure Gateway with the `Cors:AllowedOrigins` string array, for example `Cors__AllowedOrigins__0`, using exact origins without trailing slashes.
- Do not use wildcard CORS with credentials.
- Keep Gateway API URLs in `runtime-config.json` aligned with the production ingress endpoint.

## Deployment Checks

- `runtime-config.json` is present at the site root after deployment.
- `runtime-config.json` contains production values and no `localhost` URLs.
- Browser requests for `/runtime-config.json` return short/no-cache headers.
- Browser requests for hashed Angular assets return long-cache headers.
- Sign-in returns to the production SPA URL.
- Gateway API calls use the production Gateway URL and include a bearer token for the configured API scope.
