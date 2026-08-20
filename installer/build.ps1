<#
  CutVPN Setup — Build Script
  ===========================
  Собирает CutVPN.Setup.exe как один self-contained .exe для Windows x64.

  Запуск:
      powershell -ExecutionPolicy Bypass -File build.ps1

  Требования:
      .NET 8 SDK (dotnet --version)
#>
$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot

Write-Host "==> CutVPN Setup build" -ForegroundColor Cyan

# ── Проверяем dotnet ──────────────────────────────────────────────────────────
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Error "dotnet SDK не найден. Установите .NET 8 SDK: https://dotnet.microsoft.com/download"
}

$version = dotnet --version
Write-Host "    dotnet: $version" -ForegroundColor Gray

# ── Restore ───────────────────────────────────────────────────────────────────
Write-Host "==> dotnet restore" -ForegroundColor Cyan
dotnet restore .\CutVPN.Setup.csproj --verbosity quiet

# ── Publish ───────────────────────────────────────────────────────────────────
Write-Host "==> dotnet publish (Release, win-x64, single-file)" -ForegroundColor Cyan
dotnet publish .\CutVPN.Setup.csproj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    /p:PublishSingleFile=true `
    /p:IncludeNativeLibrariesForSelfExtract=true `
    /p:PublishReadyToRun=true `
    /p:DebugType=None `
    /p:DebugSymbols=false `
    --verbosity minimal

# ── Output ────────────────────────────────────────────────────────────────────
$out = Join-Path $PSScriptRoot "bin\Release\net8.0-windows\win-x64\publish\CutVPN.Setup.exe"
if (Test-Path $out) {
    $size = [math]::Round((Get-Item $out).Length / 1MB, 1)
    Write-Host ""
    Write-Host "✓ Готово! ($($size) MB)" -ForegroundColor Green
    Write-Host "  $out" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Следующий шаг: скопируйте установщики компонентов в папку payload\" -ForegroundColor Gray
    Write-Host "  - DesktopGoose.Setup.exe" -ForegroundColor Gray
    Write-Host "  - CockroachOnDesktop.exe" -ForegroundColor Gray
    Write-Host "  - workrave-setup.exe"     -ForegroundColor Gray
} else {
    Write-Error "Сборка не создала EXE по пути: $out"
}
