# Development Guide

This guide covers the local setup and day-to-day workflow for the Contract Monthly Claim System.

## Prerequisites

- .NET 8 SDK
- Git
- Visual Studio 2022, Rider, or VS Code with the C# extension

SQLite is used through Entity Framework Core, so no separate database server is required.

## Local Setup

1. Clone the repository.
2. Restore NuGet packages:

   ```powershell
   dotnet restore ".\Contract Monthly Claim System\Contract Monthly Claim System.csproj"
   ```

3. Build the application:

   ```powershell
   dotnet build ".\Contract Monthly Claim System\Contract Monthly Claim System.csproj"
   ```

4. Run the web application:

   ```powershell
   dotnet run --project ".\Contract Monthly Claim System\Contract Monthly Claim System.csproj"
   ```

The application applies pending Entity Framework migrations on startup. A local `app.db` file will be created in the application folder if one does not already exist.

## Local Data

The following files are local development artifacts and should not be committed:

- `app.db`
- `app.db-shm`
- `app.db-wal`
- `App_Data/Uploads`

The repository keeps migrations in source control so the database can be recreated from code.

## Document Storage

Uploaded documents are stored under `App_Data/Uploads` and encrypted with AES-GCM before they are written to disk.

The encryption key is read from `DocumentStorage:EncryptionKey`. Development has a local key in `appsettings.Development.json`; production environments should override it with a secret value from environment configuration or a secret store.

The key may be a base64 value or plain text, but it must decode to 16, 24, or 32 bytes for AES.

## Quality Checks

Before opening a pull request or merging a branch, run:

```powershell
dotnet restore ".\Contract Monthly Claim System\Contract Monthly Claim System.csproj"
dotnet build ".\Contract Monthly Claim System\Contract Monthly Claim System.csproj" --configuration Release --no-restore
```

Automated builds run the same restore and build steps through GitHub Actions.

## Branch Workflow

Use focused branches with small, reviewable commits. A clean branch should usually follow this shape:

1. Infrastructure or setup change.
2. Application change.
3. Verification or documentation update.

This keeps the project history easy to read and shows the reason behind each change.

## Commit Checklist

- The app builds locally.
- Generated database files and uploads are not included.
- The README or development guide is updated when setup steps change.
- Comments explain intent where the code is not obvious.
