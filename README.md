# HRConnect HRMS

HRConnect is a React and ASP.NET Core HR management application for employee records, self-service profiles, leave, documents, and role-based administration.

## Live deployment

- Frontend: https://yellow-ground-0709fd410.5.azurestaticapps.net
- API: https://hrconnectapi-d2bbatcedeg0a7ht.indiasouthcentral-01.azurewebsites.net

## Architecture

`React + Vite (Azure Static Web Apps) -> HTTPS/JWT -> ASP.NET Core API (Azure App Service) -> EF Core + SQL Server`

- `src/Frontend/hrconnect-ui`: React UI and Playwright tests.
- `src/Backend/HRConnect.API`: API, JWT authentication, authorization, Google sign-in, and documents.
- `src/Backend/HRConnect.Application`: application services and interfaces.
- `src/Backend/HRConnect.Domain`: entities and business enums.
- `src/Backend/HRConnect.Infrastructure`: EF Core DbContext, migrations, and repositories.

## Features

- JWT authentication with role and user/employee ID claims.
- Google sign-in only for existing HRConnect accounts.
- Employees access only their own profile, documents, balances, and leave records; admins manage all employees.
- Employee records, departments, leave balances/requests/review, and protected document uploads.
- Password-reset tokens are hashed, expire after 30 minutes, revoke active sessions, and are rate limited.

## Local setup

Requirements: .NET 10 SDK, SQL Server LocalDB, and Node.js 22+.

```powershell
cd C:\HRConnect-HRMS\src\Backend
dotnet ef database update --project HRConnect.Infrastructure --startup-project HRConnect.API
dotnet run --project HRConnect.API
```

```powershell
cd C:\HRConnect-HRMS\src\Frontend\hrconnect-ui
npm install
npm run dev
```

Set the JWT signing key and Google client ID through .NET user secrets or Azure app settings; never commit them.

## Testing

```powershell
cd C:\HRConnect-HRMS\src\Backend
dotnet test HRConnect.slnx
cd C:\HRConnect-HRMS\src\Frontend\hrconnect-ui
npm ci
npx playwright install chromium
npm run test:e2e
npm run build
```

The xUnit suite covers employee service behavior, leave rules, and authorization contracts. Playwright covers admin directory access, employee self-service access, blocked directory access, and leave navigation.

## Azure deployment

- The frontend deploys through Azure Static Web Apps using `.github/workflows/azure-static-web-apps-yellow-ground-0709fd410.yml`.
- The API deploys to Azure App Service at the live API URL above.
- Pull requests and pushes to `main` run tests. Deployment depends on the test job, so it cannot run after a failing suite.

## Scope

v1.0.0 is intentionally focused on HRMS. Future work is limited to critical bug and security fixes; no chatbot, Docker, microservices, or additional product modules are planned for this release line.
