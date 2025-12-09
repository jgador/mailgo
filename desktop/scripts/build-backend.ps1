$ErrorActionPreference = "Stop"

$projectPath = Join-Path $PSScriptRoot "..\..\backend\src\Mailgo.AppHost\Mailgo.AppHost.csproj"
$outputPath = Join-Path $PSScriptRoot "..\resources\backend"

Write-Host "Publishing backend to $outputPath"
dotnet publish $projectPath -c Release -o $outputPath

Write-Host "Backend publish complete."
