dotnet pack ../src/Draco.LanguageServer --output . 
try { dotnet tool uninstall --global Draco.LanguageServer } catch {}
dotnet tool install --global --add-source . Draco.LanguageServer
