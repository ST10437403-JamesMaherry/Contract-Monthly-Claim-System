# Contract Monthly Claim System (CMCS)

CMCS is an ASP.NET Core MVC application for managing monthly lecturer claims. Lecturers submit claims, coordinators review them, managers approve them, and HR manages users, reports, invoices, and payment batches.

## Main Features

- Role-based dashboards for Lecturers, Coordinators, Managers, and HR.
- Secure session-based login with hashed passwords.
- HR-created users must change their temporary password on first login.
- Lecturer claim submission with automatic total calculation.
- Coordinator and Manager review workflow with audit trail comments.
- Secure document uploads with file validation, encrypted storage, and authorized downloads.
- HR user management with hourly-rate control.
- PDF and CSV exports for users and payment reports.
- Invoice PDF downloads for payable claims.
- Monthly payment batch CSV exports for payroll processing.
- Automated tests and GitHub Actions build checks.

## Tech Stack

- .NET 8
- ASP.NET Core MVC
- Entity Framework Core
- SQLite
- QuestPDF
- xUnit
- GitHub Actions

## Project Structure

```text
Contract Monthly Claim System/        Main MVC web application
Contract Monthly Claim System.Tests/  Unit and integration tests
.github/workflows/build.yml           CI workflow
docs/                                 Development documentation
```

## Prerequisites

Install the following before running the project:

- .NET 8 SDK
- Git
- Visual Studio 2022, Rider, or VS Code with the C# extension

SQLite is used through Entity Framework Core, so no separate database server is needed.

## How To Run The Project

1. Clone the repository:

   ```powershell
   git clone <repository-url>
   cd Contract-Monthly-Claim-System
   ```

2. Restore NuGet packages:

   ```powershell
   dotnet restore ".\Contract Monthly Claim System.slnx"
   ```

3. Build the solution:

   ```powershell
   dotnet build ".\Contract Monthly Claim System.slnx"
   ```

4. Trust the local HTTPS certificate if needed:

   ```powershell
   dotnet dev-certs https --trust
   ```

5. Run the web application:

   ```powershell
   dotnet run --project ".\Contract Monthly Claim System\Contract Monthly Claim System.csproj" --launch-profile "Contract_Monthly_Claim_System"
   ```

6. Open the app in your browser:

   ```text
   https://localhost:7203
   ```

The application applies pending Entity Framework migrations on startup. A local SQLite database file named `app.db` is created when needed.

## Default Login Accounts

| Role | Email | Password |
| --- | --- | --- |
| Lecturer | `mattjones@university.co.za` | `password123` |
| Lecturer | `crownvic@university.co.za` | `password123` |
| Coordinator | `sarahw@university.co.za` | `password123` |
| Manager | `davidb@university.co.za` | `password123` |
| HR | `hr@university.co.za` | `admin123` |

If local passwords or data are changed during testing, delete the local database files and run the app again to recreate the seeded data.

## Local Database And Files

Local development files should not be committed:

- `app.db`
- `app.db-shm`
- `app.db-wal`
- `App_Data/Uploads`
- `TestResults`

Uploaded documents are stored under `App_Data/Uploads` and encrypted before being written to disk. The development encryption key is stored in `appsettings.Development.json`. Production should use a secret value from environment configuration or a secret store.

## Running Tests

Run the full test suite:

```powershell
dotnet test ".\Contract Monthly Claim System.slnx"
```

Recommended local quality check before pushing:

```powershell
dotnet restore ".\Contract Monthly Claim System.slnx"
dotnet build ".\Contract Monthly Claim System.slnx" --configuration Release --no-restore
dotnet test ".\Contract Monthly Claim System.slnx" --configuration Release --no-build
```

## CI/CD

The repository includes a GitHub Actions workflow at `.github/workflows/build.yml`.

Current CI behavior:

- Runs on every push to any branch.
- Runs on pull requests targeting `main` or `master`.
- Uses the .NET 8 SDK.
- Restores NuGet packages.
- Builds the solution in Release mode.
- Runs the test suite and writes test results to `TestResults`.

Current CD status:

- Automated deployment is not configured yet.
- A future deployment pipeline can be added after the CI build and tests pass.
- Recommended deployment targets include Azure App Service, IIS, or a container-based host.

## Common Development Commands

Add a migration:

```powershell
dotnet ef migrations add <MigrationName> --project ".\Contract Monthly Claim System\Contract Monthly Claim System.csproj" --startup-project ".\Contract Monthly Claim System\Contract Monthly Claim System.csproj"
```

Apply migrations manually:

```powershell
dotnet ef database update --project ".\Contract Monthly Claim System\Contract Monthly Claim System.csproj" --startup-project ".\Contract Monthly Claim System\Contract Monthly Claim System.csproj"
```

Run the app without the launch profile:

```powershell
dotnet run --project ".\Contract Monthly Claim System\Contract Monthly Claim System.csproj"
```

## More Documentation

See `docs/DEVELOPMENT.md` for the development workflow, branch checklist, and local quality checks.
