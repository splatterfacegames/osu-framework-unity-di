$ErrorActionPreference = "Stop"

Write-Host "Building osu.Framework.SourceGeneration..."
dotnet build "osu-framework~/osu.Framework.SourceGeneration/osu.Framework.SourceGeneration.csproj" -c Release

Write-Host "Copying Source Generator DLL to UnityPackage..."
$source = "osu-framework~/osu.Framework.SourceGeneration/bin/Release/netstandard2.0/osu.Framework.SourceGeneration.dll"
$destDir = "UnityPackage/Editor/Analyzers"
$dest = "$destDir/osu.Framework.SourceGeneration.dll"

if (-not (Test-Path $destDir)) {
    New-Item -ItemType Directory -Force -Path $destDir | Out-Null
}

Copy-Item -Path $source -Destination $dest -Force
Write-Host "Successfully copied to $dest"
