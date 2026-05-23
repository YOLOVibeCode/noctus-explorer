<#
.SYNOPSIS
    Publishes Noctus Explorer for all supported architectures.
.DESCRIPTION
    Builds self-contained single-file executables for:
    - win-x64  (Intel/AMD 64-bit)
    - win-x86  (Intel/AMD 32-bit)
    - win-arm64 (ARM64, e.g. Surface Pro X, Snapdragon laptops)
.EXAMPLE
    .\publish.ps1
    .\publish.ps1 -Runtime win-arm64
    .\publish.ps1 -Configuration Release
#>
param(
    [ValidateSet("win-x64", "win-x86", "win-arm64", "all")]
    [string]$Runtime = "all",

    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [string]$Version = ""
)

$ErrorActionPreference = "Stop"
$project = "src/Explorer.App/Explorer.App.csproj"
$outputBase = "publish"

$runtimes = if ($Runtime -eq "all") { @("win-x64", "win-x86", "win-arm64") } else { @($Runtime) }

foreach ($rid in $runtimes) {
    $outDir = "$outputBase/$rid"
    Write-Host "`n=== Publishing for $rid ===" -ForegroundColor Cyan

    $versionArg = if ($Version) { "-p:Version=$Version" } else { "" }

    dotnet publish $project `
        -c $Configuration `
        -r $rid `
        --self-contained true `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        $versionArg `
        -o $outDir

    if ($LASTEXITCODE -ne 0) {
        Write-Error "Publish failed for $rid"
        exit 1
    }

    $exe = Get-ChildItem "$outDir/NoctusExplorer.exe" -ErrorAction SilentlyContinue
    if ($exe) {
        $size = [math]::Round($exe.Length / 1MB, 1)
        Write-Host "  Output: $($exe.FullName) ($size MB)" -ForegroundColor Green
    }
}

Write-Host "`nAll builds complete. Outputs in ./$outputBase/" -ForegroundColor Green
