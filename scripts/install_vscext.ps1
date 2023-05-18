cd ../src/Draco.Extension.VsCode
echo 'y' | vsce package
$vsixFile = Get-ChildItem -Filter "*.vsix" | Select-Object -First 1
code --install-extension $vsixFile --force
cd ../../scripts
