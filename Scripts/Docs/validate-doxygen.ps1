$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptRoot "../..")
$doxyfile = Join-Path $repoRoot "Doxyfile"
$htmlIndex = Join-Path $repoRoot "docs/generated/html/index.html"

function Fail-DoxygenValidation {
    param(
        [string]$Message,
        [int]$Code = 1
    )

    Write-Host "::error::$Message"
    Write-Host "::endgroup::"
    exit $Code
}

Write-Host "::group::Doxygen validation"
Write-Host "Repository root: $repoRoot"
Write-Host "Doxyfile: $doxyfile"

if (-not (Test-Path $doxyfile)) {
    Fail-DoxygenValidation "Doxyfile was not found at $doxyfile"
}

$doxygen = Get-Command doxygen -ErrorAction SilentlyContinue
if (-not $doxygen) {
    Fail-DoxygenValidation "Doxygen is not installed or is not available on PATH."
}

Write-Host "Using Doxygen: $($doxygen.Source)"
Write-Host "Generating documentation..."

Push-Location $repoRoot
try {
    & doxygen $doxyfile
    $exitCode = $LASTEXITCODE
}
finally {
    Pop-Location
}

if ($exitCode -ne 0) {
    Fail-DoxygenValidation "Doxygen failed with exit code $exitCode." $exitCode
}

if (-not (Test-Path $htmlIndex)) {
    Fail-DoxygenValidation "Doxygen completed but expected output was not found: $htmlIndex"
}

Write-Host "Doxygen generated HTML successfully."
Write-Host "Validated output: $htmlIndex"
Write-Host "::endgroup::"
