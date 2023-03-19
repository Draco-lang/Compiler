# We collect all our projects
# NOTE: We exclude bin folders, as BDN generates projects there we don't care about
$projects = Get-ChildItem '../src' -Recurse -Include *.csproj | ? { $_.FullName -inotmatch '\\bin\\|\\obj\\' }

foreach ($project in $projects) {
    # Get the root folder of the project
    $projectRoot = [IO.Path]::GetFullPath([IO.Path]::GetDirectoryName($project))
    # From the project name we can get the root namespace
    $projectRootNamespace = [IO.Path]::GetFileNameWithoutExtension($project)
    # Get the source files for the project
    $csFiles = Get-ChildItem $projectRoot -Recurse -Include *.cs | ? { $_.FullName -inotmatch '\\bin\\|\\obj\\' }
    foreach ($csFile in $csFiles) {
        # For any file, the namespace is the root namespace
        # + the relative path to the file separated by '.'
        $filePath = [IO.Path]::GetFullPath([IO.Path]::GetDirectoryName($csFile))
        $relativeFilePath = $filePath.Substring($projectRoot.Length).TrimStart('\\')
        $relativeNamespace = $relativeFilePath.Replace('\', '.')
        $expectedNamespace = $projectRootNamespace
        if ($relativeNamespace) {
            $expectedNamespace = "$projectRootNamespace.$relativeNamespace"
        }
        # Retrieve the namespace declaration within the file
        $fileContent = Get-Content $csFile.FullName
        $namespace = $fileContent | Select-String -CaseSensitive "namespace\s+(.+?)[\s{;]" | ForEach-Object { $_.Matches.Groups[1].Value }
        # See what's the status with the file
        if ($namespace -eq $null) {
            Write-Warning "File $($csFile.FullName) does not contain a namespace declaration."
        }
        elseif ($namespace -ne $expectedNamespace) {
            Write-Warning "File $($csFile.FullName) has namespace $($namespace), but it should be $expectedNamespace."
        }
    }
}
