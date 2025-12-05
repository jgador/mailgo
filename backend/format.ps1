$currentBranch = git rev-parse --abbrev-ref HEAD
Write-Output "Running dotnet format for branch '$currentBranch'..."

dotnet format whitespace --no-restore --verbosity normal
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

dotnet format style --no-restore --severity warn --verbosity normal
exit $LASTEXITCODE
