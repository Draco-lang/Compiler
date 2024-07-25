<#
    .Description
    Installs the Draco Debug Adapter tool globally from source.
    This helps testing debugger or debug adapter changes while development.
#>

$ErrorActionPreference = "Stop"
Push-Location $PSScriptRoot
dotnet pack ../src/Draco.DebugAdapter --output .
if ((dotnet tool list --global) -match "Draco.DebugAdapter") {
    dotnet tool uninstall --global Draco.DebugAdapter
}
dotnet tool install --global --add-source . Draco.DebugAdapter
Pop-Location
