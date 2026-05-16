# Generates src/Web/SharedApiClient/schema.d.ts from the OpenAPI spec built by dotnet build.
# The .NET build tool names the output file after the assembly (Api.json).
# Run: npm run codegen:api (from repo root)

$schemaPath = Join-Path $PSScriptRoot "Api.json"
$outputPath = Join-Path $PSScriptRoot "../../src/Web/SharedApiClient/schema.d.ts"

if (-not (Test-Path $schemaPath)) {
    Write-Error "Schema file not found: $schemaPath`nRun first: dotnet build FIBRADIS.slnx"
    exit 1
}

npx openapi-typescript $schemaPath --output $outputPath
if ($LASTEXITCODE -ne 0) { Write-Error "openapi-typescript failed"; exit 1 }
Write-Host "✅ Generated: $outputPath"
