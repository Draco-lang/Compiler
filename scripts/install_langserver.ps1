<#
    .Description
    Install the Draco Language Server tool globally from source.
    This helps testing language server changes while development.
#>

$ErrorActionPreference = "Stop"
Push-Location $PSScriptRoot
dotnet pack ../src/Draco.LanguageServer --configuration Debug --output .
if ((dotnet tool list --global) -match "Draco.LanguageServer") {
    dotnet tool uninstall --global Draco.LanguageServer
}
dotnet tool install --global --add-source . Draco.LanguageServer
Pop-Location
