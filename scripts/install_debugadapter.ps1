dotnet pack ../src/Draco.DebugAdapter --output .
try { dotnet tool uninstall --global Draco.DebugAdapter --verbosity quiet } catch {}
dotnet tool install --global --add-source . Draco.DebugAdapter
