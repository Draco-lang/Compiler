<#
    .Description
    Installs the Draco Visual Studio Code extension from source.
    This helps testing extension changes while development.
#>

$ErrorActionPreference = "Stop"
# get scripts directory and push location
Push-Location $PSScriptRoot
cd ../src/Draco.Extension.VsCode
npm i
echo 'y' | vsce package
$vsixFile = Get-ChildItem -Filter "*.vsix" | Select-Object -First 1
code --install-extension $vsixFile --force
Pop-Location
