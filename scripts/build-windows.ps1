param(
    [string]$RhinoCommonPath = (Join-Path $env:ProgramFiles 'Rhino 8/System/RhinoCommon.dll'),
    [string]$YakPath = (Join-Path $env:ProgramFiles 'Rhino 8/System/yak.exe'),
    [string]$OutputDirectory = (Join-Path $PSScriptRoot '../dist/Windows')
)

$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot '../ononSmartView.csproj'
$manifestSource = Join-Path $PSScriptRoot '../packaging/windows/manifest.yml'
$RhinoCommonPath = [System.IO.Path]::GetFullPath($RhinoCommonPath)
$YakPath = [System.IO.Path]::GetFullPath($YakPath)
$OutputDirectory = [System.IO.Path]::GetFullPath($OutputDirectory)
$packageDirectory = Join-Path $OutputDirectory 'yak-package'
$legacyRhiPath = Join-Path $OutputDirectory 'ononSmartView.rhi'

if (-not (Test-Path -LiteralPath $RhinoCommonPath)) {
    throw "RhinoCommon.dll was not found at '$RhinoCommonPath'. Install Rhino 8 or pass -RhinoCommonPath."
}
if (-not (Test-Path -LiteralPath $YakPath)) {
    throw "Yak was not found at '$YakPath'. Install Rhino 8 or pass -YakPath."
}
if (-not (Test-Path -LiteralPath $manifestSource)) {
    throw "Yak manifest was not found at '$manifestSource'."
}

dotnet build $project -c Release -p:UseRhinoRuntimeReferences=false "-p:RhinoCommonPath=$RhinoCommonPath"
if ($LASTEXITCODE -ne 0) {
    throw 'The ononSmartView build failed.'
}

$buildDirectory = Join-Path (Split-Path $project) 'bin/Release/net8.0'
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
Remove-Item $legacyRhiPath -Force -ErrorAction SilentlyContinue
Remove-Item $packageDirectory -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $packageDirectory | Out-Null
Copy-Item (Join-Path $buildDirectory 'ononSmartView.dll') (Join-Path $packageDirectory 'ononSmartView.rhp') -Force
Copy-Item (Join-Path $buildDirectory 'ononSmartView.deps.json') (Join-Path $packageDirectory 'ononSmartView.deps.json') -Force
Copy-Item $manifestSource (Join-Path $packageDirectory 'manifest.yml') -Force

Push-Location $packageDirectory
try {
    & $YakPath build --platform win
    if ($LASTEXITCODE -ne 0) {
        throw 'Yak failed to build the Windows package.'
    }
}
finally {
    Pop-Location
}

$yakPackage = Get-ChildItem -LiteralPath $packageDirectory -Filter '*.yak' | Select-Object -First 1
if (-not $yakPackage) {
    throw "Yak completed without producing a package in '$packageDirectory'."
}
Move-Item -LiteralPath $yakPackage.FullName -Destination (Join-Path $OutputDirectory $yakPackage.Name) -Force

$manualDirectory = Join-Path $OutputDirectory 'manual-load'
New-Item -ItemType Directory -Force -Path $manualDirectory | Out-Null
Copy-Item (Join-Path $buildDirectory 'ononSmartView.dll') (Join-Path $manualDirectory 'ononSmartView.rhp') -Force
Copy-Item (Join-Path $buildDirectory 'ononSmartView.deps.json') (Join-Path $manualDirectory 'ononSmartView.deps.json') -Force
$manualArchive = Join-Path $OutputDirectory 'ononSmartView-windows-manual.zip'
Remove-Item $manualArchive -Force -ErrorAction SilentlyContinue
Compress-Archive -Path (Join-Path $manualDirectory '*') -DestinationPath $manualArchive -Force

Write-Host "Windows Yak package created: $(Join-Path $OutputDirectory $yakPackage.Name)"
Write-Host "Manual-load archive created: $manualArchive"
