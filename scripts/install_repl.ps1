<#
    .Description
    Install the Draco Repl tool globally from source.
    This helps testing repl changes while development.
#>

$ErrorActionPreference = "Stop"
Push-Location $PSScriptRoot
dotnet pack ../src/Draco.Repl --configuration Debug --output .
if ((dotnet tool list --global) -match "Draco.Repl") {
    dotnet tool uninstall --global Draco.Repl
}
dotnet tool install --global --add-source . Draco.Repl
Pop-Location
