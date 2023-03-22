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

# Remove everything that used to be there and could be relevant
Remove-Item -Path $path -Include *.dracoproj -Recurse
Remove-Item -Path $path -Include *.draco -Recurse
Remove-Item -Path $path -Include *.nupkg -Recurse
Remove-Item -Path $path -Include *.config -Recurse

# We package the toolchain
$toolchainPath = Join-Path -Path $path -ChildPath "toolchain"
dotnet pack ../src --output $toolchainPath

# Install the project templates
$templateProjectPath = Get-ChildItem -Path $toolchainPath -Filter "*Draco.ProjectTemplates.*.nupkg*" | % { $_.FullName }
try { dotnet new uninstall Draco.ProjectTemplates } catch {}
dotnet new install --force $templateProjectPath

# We save the current location and go to the specified path
Push-Location
cd $path

# Create a test project in Draco
dotnet new console --language draco

# Add the toolchain as the primary nuget source
dotnet new nugetconfig
dotnet nuget remove source nuget --configfile nuget.config
dotnet nuget add source ./toolchain --name DracoToolchain --configfile nuget.config

# Restore old location
Pop-Location
