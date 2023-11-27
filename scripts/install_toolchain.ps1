# Check if a path argument was passed in
if ($args.Length -eq 0) {
    Write-Error "Please specify a path argument"
    exit 1
}

# Get the path argument and create the directory if it doesn't exist
$path = $args[0]
if (!(Test-Path $path)) {
    New-Item -ItemType Directory -Path $path
}

$toolchainPath = Join-Path -Path $path -ChildPath "Toolchain"

# Remove previous toolchain
Remove-Item -Path $toolchainPath -Recurse -ErrorAction SilentlyContinue

# Install the new toolchain in its place
dotnet pack ../src/Draco.sln --output $toolchainPath

# Install the project templates
$templateProjectPath = Get-ChildItem -Path $toolchainPath -Filter "*Draco.ProjectTemplates.*.nupkg*" | ForEach-Object { $_.FullName }
try { dotnet new uninstall Draco.ProjectTemplates --verbosity quiet } catch { }
dotnet new install --force $templateProjectPath

# We save the current location and go to the specified path
Push-Location
Set-Location $path

# Create a test project in Draco if one doesn't exist yet
if (!(Get-ChildItem -Filter *.dracoproj)) {
    dotnet new console --language draco
}

# Add the toolchain as the primary nuget source and change the restore direcotry
$nugetConfigPath = "nuget.config"
if (!(Test-Path $nugetConfigPath)) {
    $nugetConfig = '<?xml version="1.0" encoding="utf-8"?>
    <configuration>
      <packageSources>
        <clear />
        <add key="draco" value=".\Toolchain" />
        <add key="nuget" value="https://api.nuget.org/v3/index.json" />
      </packageSources>
      <config>
        <add key="globalPackagesFolder" value="Toolchain\GlobalPackages" />
      </config>
    </configuration>'

    Out-File -FilePath $nugetConfigPath -InputObject $nugetConfig -Encoding utf8
    Write-Host "Successfully created NuGet.config."
}

# Restore old location
Pop-Location
