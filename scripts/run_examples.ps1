<#
    .Description
    Runs all examples in the examples directory.
#>

# Save location and go to the examples directory
Push-Location
Set-Location "../examples"

# Find each subdirectory with a .dracoproj file
$directories = Get-ChildItem -Directory | Where-Object { $_.GetFiles("*.dracoproj").Count -gt 0 }

# Run each example
foreach ($directory in $directories) {
    # Save the current location and go to the example directory
    Push-Location
    Set-Location $directory.FullName

    # Log a message about the ran example
    Write-Host "Running example in $directory"

    # Run the example
    dotnet run

    # Restore the old location
    Pop-Location
}

# Restore directory
Pop-Location
