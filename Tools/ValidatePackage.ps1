[CmdletBinding()]
param(
    [Parameter()]
    [string]$UnityPath = "C:\Program Files\Unity\Hub\Editor\6000.3.11f1\Editor\Unity.exe",

    [Parameter()]
    [string]$ProjectPath,

    [Parameter()]
    [string]$PackageReference,

    [Parameter()]
    [string]$DocumentationPath,

    [Parameter()]
    [switch]$StaticOnly
)

$ErrorActionPreference = "Stop"

$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
if ([string]::IsNullOrWhiteSpace($ProjectPath)) {
    $ProjectPath = Join-Path $repositoryRoot "work~\UnityValidation"
}
$ProjectPath = [System.IO.Path]::GetFullPath($ProjectPath)
$validationRoot = [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot "work~"))
$validationPrefix = $validationRoot.TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar
if (-not $ProjectPath.StartsWith($validationPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "The disposable Unity project must remain under $validationRoot"
}

$package = Get-Content -LiteralPath (Join-Path $repositoryRoot "package.json") -Raw | ConvertFrom-Json
if ($package.name -ne "com.geurts.gameforge.documentation") {
    throw "package.json has the wrong package identity."
}
if ($null -ne $package.dependencies -and @($package.dependencies.PSObject.Properties).Count -ne 0) {
    throw "The package must not declare Unity package dependencies."
}

$productionAssembly = Get-Content -LiteralPath (Join-Path $repositoryRoot "Editor\Geurts.GameForge.Documentation.Editor.asmdef") -Raw | ConvertFrom-Json
if (@($productionAssembly.includePlatforms).Count -ne 1 -or $productionAssembly.includePlatforms[0] -ne "Editor") {
    throw "The production assembly must be Editor-only."
}
if (@($productionAssembly.references).Count -ne 0) {
    throw "The production assembly must not reference another package assembly."
}
if (@($productionAssembly.defineConstraints).Count -ne 1 -or $productionAssembly.defineConstraints[0] -ne "UNITY_EDITOR_WIN") {
    throw "The production assembly must be limited to Windows Editor hosts."
}

$batchFiles = @(Get-ChildItem -LiteralPath $repositoryRoot -Recurse -File -Filter "*.bat" | Where-Object {
    $_.FullName -notmatch '[\\/](?:work~|old-validation~|outputs|\.git)[\\/]'
})
if ($batchFiles.Count -ne 0) {
    throw "External batch launchers are outside this package's scope."
}
if (Test-Path -LiteralPath (Join-Path $repositoryRoot "GeurtsTechniques")) {
    throw "The Unity package must not embed the authoritative documentation payload."
}
if (Test-Path -LiteralPath (Join-Path $repositoryRoot "GeurtsGameForgeDocumentation")) {
    throw "The Unity package must not embed an installed documentation copy."
}

$forbiddenDependencyPattern = '(?i)com\.gameforge\.intelligence|odin|quantum|brick.?manager|gameforge.?god'
$dependencyFiles = @(
    (Join-Path $repositoryRoot "package.json"),
    (Join-Path $repositoryRoot "Editor\Geurts.GameForge.Documentation.Editor.asmdef")
)
foreach ($dependencyFile in $dependencyFiles) {
    if ((Get-Content -LiteralPath $dependencyFile -Raw) -match $forbiddenDependencyPattern) {
        throw "A forbidden package dependency appears in $dependencyFile."
    }
}

if ($StaticOnly) {
    [pscustomobject]@{
        Package = $package.name
        PackageVersion = $package.version
        StaticValidation = "Passed"
    }
    return
}

if (-not (Test-Path -LiteralPath $UnityPath -PathType Leaf)) {
    throw "Unity Editor was not found at: $UnityPath"
}

if (Test-Path -LiteralPath $ProjectPath) {
    Remove-Item -LiteralPath $ProjectPath -Recurse -Force
}
New-Item -ItemType Directory -Path (Join-Path $ProjectPath "Assets") -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $ProjectPath "Packages") -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $ProjectPath "ProjectSettings") -Force | Out-Null

if ([string]::IsNullOrWhiteSpace($PackageReference)) {
    $packagePath = $repositoryRoot.Replace('\', '/')
    $PackageReference = "file:$packagePath"
}

$manifest = [ordered]@{
    dependencies = [ordered]@{
        "com.geurts.gameforge.documentation" = $PackageReference
        "com.unity.test-framework" = "1.6.0"
    }
    testables = @("com.geurts.gameforge.documentation")
}
$manifest | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $ProjectPath "Packages\manifest.json") -Encoding UTF8

# Use real documentation as an external integration fixture, never as a bundled template.
if (-not [string]::IsNullOrWhiteSpace($DocumentationPath)) {
    $fixtureRoot = Join-Path $ProjectPath "GeurtsGameForgeDocumentation"
    foreach ($relativePath in @("GeurtsTechniqueManifest.md", "GeurtsTechniques/GeurtsGitIgnoreTechnique.md")) {
        $sourcePath = Join-Path $DocumentationPath $relativePath
        $fixturePath = Join-Path $fixtureRoot $relativePath
        New-Item -ItemType Directory -Path (Split-Path -Parent $fixturePath) -Force | Out-Null
        Copy-Item -LiteralPath $sourcePath -Destination $fixturePath
    }
}
@"
m_EditorVersion: 6000.3.11f1
m_EditorVersionWithRevision: 6000.3.11f1 (3000ef702840)
"@ | Set-Content -LiteralPath (Join-Path $ProjectPath "ProjectSettings\ProjectVersion.txt") -Encoding UTF8

$resultPath = Join-Path $ProjectPath "TestResults.xml"
$logPath = Join-Path $ProjectPath "UnityValidation.log"
$arguments = @(
    "-runTests",
    "-batchmode",
    "-nographics",
    "-projectPath", $ProjectPath,
    "-testPlatform", "EditMode",
    "-testResults", $resultPath,
    "-logFile", $logPath
)

$process = Start-Process -FilePath $UnityPath -ArgumentList $arguments -Wait -PassThru -WindowStyle Hidden
if ($process.ExitCode -ne 0) {
    if (Test-Path -LiteralPath $logPath) {
        Get-Content -LiteralPath $logPath -Tail 160 | Write-Host
    }
    throw "Unity validation failed with exit code $($process.ExitCode)."
}
if (-not (Test-Path -LiteralPath $resultPath -PathType Leaf)) {
    if (Test-Path -LiteralPath $logPath) {
        Get-Content -LiteralPath $logPath -Tail 160 | Write-Host
    }
    throw "Unity did not produce EditMode test results."
}

[xml]$results = Get-Content -LiteralPath $resultPath -Raw
$testRun = $results.'test-run'
if ($null -eq $testRun -or [int]$testRun.failed -ne 0 -or [int]$testRun.passed -lt 1 -or
    (-not [string]::IsNullOrWhiteSpace($DocumentationPath) -and [int]$testRun.skipped -ne 0)) {
    throw "Unity EditMode tests did not pass."
}

$log = Get-Content -LiteralPath $logPath -Raw
if ($log -match '(?m)\berror CS\d+' -or $log -match '(?m)\bwarning CS\d+' -or $log -match 'Tundra build failed') {
    throw "Unity reported a C# compiler diagnostic or build failure. See $logPath"
}

[pscustomobject]@{
    Package = $package.name
    PackageVersion = $package.version
    PackageReference = $PackageReference
    Unity = "6000.3.11f1"
    Passed = [int]$testRun.passed
    Failed = [int]$testRun.failed
    Skipped = [int]$testRun.skipped
    ResultPath = $resultPath
    LogPath = $logPath
}
