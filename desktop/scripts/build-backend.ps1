$ErrorActionPreference = "Stop"

$projectPath = Join-Path $PSScriptRoot "..\..\backend\src\Mailgo.AppHost\Mailgo.AppHost.csproj"
$outputPath = Join-Path $PSScriptRoot "..\resources\backend"

# Publishing win-x64 only for now (current test machine).
Write-Host "Publishing backend to $outputPath"
dotnet publish `
  $projectPath `
  -c Release `
  -o $outputPath `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:PublishTrimmed=false

Write-Host "Backend publish complete."
