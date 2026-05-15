# FIBRADIS

Financial Bonds & Fixed-Income Discovery System

## Stack

| Layer | Technology |
|---|---|
| Backend API | ASP.NET Core (.NET 10) |
| ORM | EF Core 10 + SQL Server |
| SPA Main | Vite + React 19 + TypeScript |
| SPA Ops | Vite + React 19 + TypeScript |
| UI | shadcn/ui + Tailwind CSS v4 |

## Quick Start

```bash
# Backend
dotnet build FIBRADIS.slnx
dotnet ef database update --project src/Server/Infrastructure --startup-project src/Server/Api

# Frontend Main (port 5173)
cd src/Web/Main && npm install && npm run dev

# Frontend Ops (port 5174)
cd src/Web/Ops && npm install && npm run dev
```

## Project Structure

```
src/
  Server/          # .NET projects
    Api/           # ASP.NET Core Web API
    Application/   # Use cases & CQRS
    Domain/        # Entities & domain logic
    Infrastructure/ # EF Core, jobs, integrations
    SharedApiContracts/ # Versioned DTOs
  Web/
    Main/          # Investor-facing SPA
    Ops/           # Operations dashboard SPA
tests/
  Unit/
  Integration/
  Contract/
  E2E/
```
