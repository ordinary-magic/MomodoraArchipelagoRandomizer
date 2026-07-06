# Search and run msbuild.exe
Write-Host "Building Game Plugin..."
$vs = & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" -version "[16.0,18.0)" -products * -requires Microsoft.Component.MSBuild -prerelease -latest -utf8 -format json | ConvertFrom-Json
$msbuild = Join-Path $vs[0].installationPath -ChildPath "MSBuild" | Join-Path -ChildPath "Current" | Join-Path -ChildPath "Bin" | Join-Path -ChildPath "MSBuild.exe"
& $msbuild /t:build MomodoraArchipelagoRandomizer.sln

#I wish this worked but compress-archive doesnt store folders in the resultant zip
#Compress-Archive "Archipelago\momo4" "momo4.zip"
# Nvm I just do it in python instead

# Move everything to the build folder
Write-Host "Creating Build artifacts..."
if(!(Test-Path "build")) {
    New-Item -Path "build" -ItemType Directory -Force | Out-Null
}

# Consolidate the build into the output folder
Copy-Item "bin\Debug\MomodoraArchipelagoRandomizer.dll" "build\MomodoraArchipelagoRandomizer.dll" -Force
Copy-Item "bin\Debug\Archipelago.MultiClient.Net.dll" "build\Archipelago.MultiClient.Net.dll" -Force
Copy-Item "bin\Debug\Newtonsoft.Json.dll" "build\Newtonsoft.Json.dll" -Force
Copy-Item "README.md" "build\README.md" -Force


# Update the "Components" which are used in the update script
Copy-Item "bin\Debug\MomodoraArchipelagoRandomizer.dll" "Components\MomodoraArchipelagoRandomizer.dll" -Force
Copy-Item "bin\Debug\Archipelago.MultiClient.Net.dll" "Components\Archipelago.MultiClient.Net.dll" -Force

Write-Host "Done!"

