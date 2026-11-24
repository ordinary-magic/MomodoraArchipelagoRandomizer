# Build the various pieces
dotnet build Randomizer\Momo4ApRandomizer.csproj -c Release # Built in place
dotnet build Livesplit\Momo4Livesplit.csproj -c Release # Copy is done manually, no -o needed.
dotnet build Standalone\Momo4Standalone.csproj -c Release -o build/Standalone

# Todo: invoke the python build here

Write-Host "Done!"
