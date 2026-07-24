# Angular Azure Static Hosting

This checklist documents the dev-free static hosting workflow for the Angular frontend.

## Hosting Model

- Build `src/Web/LogisticsHub.Web` as static Angular assets.
- Publish the built browser assets to the Azure Storage Static Website `$web` container.
- Host Gateway/backend services separately. The Angular app is not served from the Gateway container.
- The dev-free Storage Static Website uses `index.html` as both the index and error document, so browser refreshes for Angular routes fall back to the SPA shell.
- Azure Front Door, CDN, custom domains, and production cache policy remain future work.

## Prerequisites

- Azure CLI signed in as an operator with blob data permissions on `stlghubdevfree600544`.
- The Storage Static Website endpoint exists: `https://stlghubdevfree600544.z1.web.core.windows.net/`.
- The Gateway endpoint exists: `https://ca-gateway-logisticshub-dev-free.wittyisland-fa7a06fc.swedencentral.azurecontainerapps.io`.
- The Entra SPA app registration allows redirect URI `https://stlghubdevfree600544.z1.web.core.windows.net/`.
- The Gateway Container App allows CORS origin `https://stlghubdevfree600544.z1.web.core.windows.net`.

## Runtime Config

The Angular app loads `/runtime-config.json` before MSAL and Gateway API setup. The checked-in file under `src/Web/LogisticsHub.Web/public/runtime-config.json` is a local-development default and must be replaced in the production browser output before upload.

The deployment script writes `src/Web/LogisticsHub.Web/dist/LogisticsHub.Web/browser/runtime-config.json` with:

- `api.gatewayBaseUrl`: `https://ca-gateway-logisticshub-dev-free.wittyisland-fa7a06fc.swedencentral.azurecontainerapps.io`
- `api.scope`: `api://dcfdc59c-73f1-457d-9dcd-4363640e9bf9/access_as_user`
- `msal.clientId`: `9a11cd54-d5e6-4a09-a236-dfbb02309d3a`
- `msal.authority`: `https://login.microsoftonline.com/942af48b-f19a-49f4-a016-5f3ef85774a9`
- `msal.redirectUri`: `https://stlghubdevfree600544.z1.web.core.windows.net/`

`runtime-config.json` must not contain secrets. Client IDs, authorities, redirect URIs, and scopes are public SPA configuration; client secrets do not belong in the Angular app.

## Build And Deploy

From the repository root:

```powershell
cd .\src\Web\LogisticsHub.Web
npm ci
npm run build
cd ..\..\..
.\deploy-dev-free-angular.ps1 -SkipBuild
```

For a local validation run that builds and generates production `runtime-config.json` without uploading:

```powershell
.\deploy-dev-free-angular.ps1 -SkipUpload
```

The script uses Azure AD authentication for Storage operations through `az storage blob ... --auth-mode login`. It does not use account keys, SAS tokens, or committed credentials.

## Caching And Cleanup

- `index.html` and `runtime-config.json` are uploaded with `Cache-Control: no-store, no-cache, must-revalidate`.
- Conservatively detected hashed Angular `.js` and `.css` build assets, such as `main-*.js`, `chunk-*.js`, and `styles-*.css`, are uploaded with `Cache-Control: public, max-age=31536000, immutable`.
- Non-hashed files such as `favicon.ico` and `runtime-config.README.md` are uploaded with `Cache-Control: no-cache, must-revalidate`.
- After uploading the current build, the script deletes blobs in `$web` that are no longer present in `dist/LogisticsHub.Web/browser`.

## Rollback

Rebuild or restore the previous Angular browser output, generate the matching `runtime-config.json`, and rerun `.\deploy-dev-free-angular.ps1 -SkipBuild`. Because obsolete blobs are removed after upload, a rollback must publish the complete browser output for the selected version.

## Gateway/CORS Checklist

If the Angular frontend and Gateway use different origins, Gateway production CORS must explicitly allow the frontend origin.

- Allow only the real frontend origin: the dev-free Storage Static Website origin now, or the future Front Door/custom domain origin.
- Configure Gateway with the `Cors:AllowedOrigins` string array, for example `Cors__AllowedOrigins__0`, using exact origins without trailing slashes.
- Do not use wildcard CORS with credentials.
- Keep Gateway API URLs in `runtime-config.json` aligned with the production ingress endpoint.

## Verification

- `runtime-config.json` is present at the site root after deployment.
- `runtime-config.json` contains production values and no `localhost` URLs.
- Browser requests for `/runtime-config.json` return no-cache headers.
- Browser requests for hashed Angular assets return long-cache headers.
- Sign-in returns to the dev-free Static Website URL.
- Gateway API calls use the production Gateway URL and include a bearer token for the configured API scope.