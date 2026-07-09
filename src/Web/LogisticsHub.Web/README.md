# LogisticsHub.Web

Minimal Angular shell for validating Microsoft Entra ID sign-in with MSAL and calling the protected Gateway.

## Run

Start the backend Gateway and services first, then run:

```powershell
npm install
npm start
```

Open `http://localhost:4200`, sign in, and click **Load companies**. The app calls:

```text
GET http://localhost:5100/company/companies
```

The Entra SPA redirect URI must include:

```text
http://localhost:4200
```

MSAL settings are centralized in `src/environments/environment.ts`. The checked-in values are public app registration identifiers and API scope values, not client secrets.

## Build

```powershell
npm run build
```

## Azure Static Hosting

For the production static hosting checklist, including Azure Storage Static Website, Front Door/CDN, and `runtime-config.json` handling, see [Angular Azure Static Hosting](../../../docs/frontend-azure-static-hosting.md).
