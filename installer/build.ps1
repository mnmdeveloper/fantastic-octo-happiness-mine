$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

dotnet restore .\CutVPN.Setup.csproj
dotnet publish .\CutVPN.Setup.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true

Write-Host ""
Write-Host "CutVPN Setup built:" -ForegroundColor Green
Write-Host (Join-Path $PSScriptRoot 'bin\Release\net8.0-windows\win-x64\publish\CutVPN.Setup.exe')
