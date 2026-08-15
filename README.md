# HRConnect HRMS

HRConnect is a React and ASP.NET Core HR management application with Google/password authentication, role-based access, employee records, departments, leave workflows, and protected employee document uploads.

## Features

- Employee directory plus private employee profiles
- Departments and reporting-manager relationships
- Roles: `Employee`, `Manager`, `HR`, and `Admin`
- Configurable leave types, balances, requests, manager/HR approval, cancellation, half-days, overlap checks, and balance enforcement
- Employee-only PAN card, Aadhaar card, profile photo, passport, education, employment, and other document uploads
- Google sign-in and password sign-in; both issue the same application JWT and map to a `UserAccount`
- One-time password-reset links with hashed token storage, 30-minute expiry, per-IP throttling, and session revocation
- User-to-employee linking for self-service access

The seeded leave categories follow the categories publicly described on MRI Software's India benefits page. Entitlement numbers are application defaults, not a claim about MRI's internal policy. HR should update them from the current internal handbook before production use.

## Local setup

Requirements: .NET SDK, SQL Server LocalDB, and Node.js/npm.

1. Store development secrets outside source control:

   ```powershell
   cd C:\HRConnect-HRMS\src\Backend\HRConnect.API
   dotnet user-secrets set "Jwt:SigningKey" "replace-with-a-random-secret-at-least-32-characters"
   dotnet user-secrets set "GoogleAuth:ClientId" "YOUR_CLIENT_ID.apps.googleusercontent.com"
   ```

   To deliver password-reset emails outside local development, also configure SMTP secrets/settings:

   ```powershell
   dotnet user-secrets set "PasswordReset:SmtpHost" "smtp.example.com"
   dotnet user-secrets set "PasswordReset:SmtpPort" "587"
   dotnet user-secrets set "PasswordReset:SmtpUsername" "smtp-user"
   dotnet user-secrets set "PasswordReset:SmtpPassword" "smtp-password"
   dotnet user-secrets set "PasswordReset:FromEmail" "no-reply@example.com"
   dotnet user-secrets set "PasswordReset:FrontendBaseUrl" "https://your-frontend.example.com"
   ```

   With SMTP unset in Development, an existing account receives a development-only reset link directly in the UI. Production never returns reset tokens in API responses.

2. Apply database migrations:

   ```powershell
   cd C:\HRConnect-HRMS\src\Backend
   dotnet ef database update --project HRConnect.Infrastructure --startup-project HRConnect.API
   ```

3. Start the API:

   ```powershell
   cd C:\HRConnect-HRMS\src\Backend\HRConnect.API
   dotnet run --launch-profile https
   ```

4. Start the frontend in another terminal:

   ```powershell
   cd C:\HRConnect-HRMS\src\Frontend\hrconnect-ui
   npm install
   npm run dev
   ```

Open `http://localhost:5173`. The frontend uses `https://localhost:7030/api` by default; override it with `VITE_API_BASE_URL` if needed.

For the Google OAuth web client, add `http://localhost:5173` and `http://127.0.0.1:5173` as authorized JavaScript origins. Google Identity Services returns the credential to the frontend popup, so this implementation does not require an authorized redirect URI.

## Access model

- Employee: own private profile, documents, leave balances, and leave requests
- Manager: employee permissions plus direct-report work-profile access and leave review
- HR: manage employee work records, departments, leave policy/balances, and user-to-employee links
- Admin: all HR capabilities plus role assignment and employee deletion

Role or employee-link changes take effect after the affected user signs in again. Seed/bootstrap migration logic promotes the earliest existing account to `Admin`; review role assignments immediately after migration.

## Private documents

Files are restricted to PDF/JPG/JPEG/PNG, limited to 10 MB, renamed with random server-side names, and stored outside the web root under `src/Backend/HRConnect.API/App_Data/employee-documents`. Only the employee linked to the profile can list, download, upload, or delete them; HR/Admin access is denied at the API boundary. Personal contact, identity, address, and emergency information follows the same employee-only rule. For production, use encrypted object storage, malware scanning, retention rules, audit logging, and a managed secrets store.

## Verification

```powershell
cd C:\HRConnect-HRMS\src\Backend
dotnet build HRConnect.sln

cd C:\HRConnect-HRMS\src\Frontend\hrconnect-ui
npm run lint
npm run build
```
