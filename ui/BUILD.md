# Build and test CutVPN

Requirements:
- Windows 10/11
- .NET 8 SDK
- Visual Studio 2022 or `dotnet` CLI

Build:

```powershell
dotnet build ui/CutVPN.csproj -c Release
```

Run:

```powershell
dotnet run --project ui/CutVPN.csproj
```

Publish a self-contained Windows executable:

```powershell
dotnet publish ui/CutVPN.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

The current UI is deliberately a transparent tray application. It stores only the prank-mode preference under the current user's LocalAppData and exposes a visible CutVPN tray menu.
