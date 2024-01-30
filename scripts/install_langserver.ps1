$ErrorActionPreference = "Stop"
Push-Location $PSScriptRoot
dotnet pack ../src/Draco.LanguageServer --output .
if ((dotnet tool list --global) -match "Draco.LanguageServer") {
    dotnet tool uninstall --global Draco.LanguageServer
}
dotnet tool install --global --add-source . Draco.LanguageServer
Pop-Location
