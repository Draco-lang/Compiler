dotnet pack ../src/Draco.DebugAdapter --output .
if ((dotnet tool list --global) -match "Draco.DebugAdapter") {
    dotnet tool uninstall --global Draco.DebugAdapter
}
dotnet tool install --global --add-source . Draco.DebugAdapter
