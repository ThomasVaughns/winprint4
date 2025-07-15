# Bootstraps the WinPrint development environment on Windows.
# Install Visual Studio Build Tools with required workloads, initialize submodules,
# and restore NuGet packages.
#
# The optional VSVersion parameter specifies which version of Visual Studio Build
# Tools to install (for example '2025').

param(
    [string]$VSVersion = "2022",
    [switch]$SkipInstall
)

Write-Host "Initializing git submodules..."
git submodule update --init --recursive

if (-not $SkipInstall) {
    Write-Host "Installing Visual Studio Build Tools $VSVersion via winget..."
    $buildToolsId = "Microsoft.VisualStudio.$VSVersion.BuildTools"
    winget install -e --id $buildToolsId --source winget `
        --override "--quiet --add Microsoft.VisualStudio.Workload.VCTools --add Microsoft.VisualStudio.Workload.ManagedDesktop --add Microsoft.VisualStudio.Component.Windows11SDK.22621 --norestart" || Write-Warning "winget failed or is not available. Ensure Visual Studio with the .NET Desktop and C++ workloads is installed"

    Write-Host "Installing the .NET Windows Desktop workload..."
    dotnet workload install windowsdesktop || Write-Warning "Failed to install the Windows Desktop workload"
}

Write-Host "Restoring NuGet packages..."
dotnet restore src/WinPrint.sln --no-cache /p:RestoreWarnAsError=false
