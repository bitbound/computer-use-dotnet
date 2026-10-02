# Builds the MCP server as a local NuGet tool package and prints the install command.
# Usage: .\publish-local.ps1 [-Version 0.1.0-local]
param(
  [string]$Version = "0.1.0-local"
)

$ErrorActionPreference = "Stop"

$solution = Join-Path $PSScriptRoot "ComputerUseMcp.slnx"
$output = Join-Path $PSScriptRoot "artifacts"

if (Test-Path $output) {
  Remove-Item $output -Recurse -Force
}

dotnet pack $solution --configuration Release -p:PackageVersion=$Version -o $output

if ($LASTEXITCODE -ne 0) {
  throw "dotnet pack failed with exit code $LASTEXITCODE"
}

$package = Get-ChildItem $output -Filter "Bitbound.ComputerUseDotnet.*.nupkg" | Select-Object -First 1

Write-Host ""
Write-Host "Package created: $($package.FullName)"
Write-Host ""
Write-Host "Install locally with:"
Write-Host "  dotnet tool install --global Bitbound.ComputerUseDotnet --version $Version --add-source $output"
Write-Host ""
Write-Host "Then configure your MCP client with:"
Write-Host "  command: computer-use-mcp"
