# Shut down dotnet build server
dotnet build-server shutdown

# Clean and build ScriptPath/../Draco.Coverage project
dotnet clean "$PSScriptRoot\..\Draco.Coverage\Draco.Coverage.csproj"
dotnet build "$PSScriptRoot\..\Draco.Coverage\Draco.Coverage.csproj"

# Delete ScriptPath/GlobalPackages/draco.coverage.msbuild folder with all its contents
Remove-Item -Path "$PSScriptRoot\GlobalPackages\draco.coverage.msbuild" -Recurse -Force

# Copy the Draco.Coverage.MSBuild.1.0.0.nupkg package from ScriptPath/../artifacts/package/debug to ScriptPath/LocalPackages
Copy-Item -Path "$PSScriptRoot\..\artifacts\package\debug\Draco.Coverage.MSBuild.1.0.0.nupkg" -Destination "$PSScriptRoot\LocalPackages"

# Run project build
dotnet build
