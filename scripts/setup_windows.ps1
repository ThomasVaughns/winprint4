# Bootstraps the WinPrint development environment on Windows.
# Install Visual Studio Build Tools with required workloads, initialize submodules,
# and restore NuGet packages.

param(
    [switch]$SkipInstall
)

Write-Host "Initializing git submodules..."
git submodule update --init --recursive

if (-not $SkipInstall) {
    Write-Host "Installing Visual Studio Build Tools and workloads via winget..."
    winget install -e --id Microsoft.VisualStudio.2022.BuildTools --source winget `
        --override "--quiet --add Microsoft.VisualStudio.Workload.VCTools --add Microsoft.VisualStudio.Workload.ManagedDesktop --add Microsoft.VisualStudio.Component.Windows11SDK.22621 --norestart" || Write-Warning "winget failed or is not available. Ensure Visual Studio with the .NET Desktop and C++ workloads is installed"
}

Write-Host "Restoring NuGet packages..."
dotnet restore src/WinPrint.sln --no-cache /p:RestoreWarnAsError=false
