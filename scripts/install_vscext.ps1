$ErrorActionPreference = "Stop"
# get scripts directory and push location
Push-Location $PSScriptRoot
cd ../src/Draco.Extension.VsCode
npm i
vsce package --skip-license
$vsixFile = Get-ChildItem -Filter "*.vsix" | Select-Object -First 1
code --install-extension $vsixFile --force
Pop-Location
