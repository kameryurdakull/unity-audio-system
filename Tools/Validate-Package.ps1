param(
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = 'Stop'
$packageName = 'com.kameryurdakull.audiosystem'
$packageRoot = Join-Path $ProjectRoot "Packages/$packageName"
$package = Get-Content -LiteralPath (Join-Path $packageRoot 'package.json') -Raw | ConvertFrom-Json

if ($package.name -ne $packageName) { throw 'Beklenmeyen paket adı.' }
if ($package.version -notmatch '^\d+\.\d+\.\d+(-[0-9A-Za-z.-]+)?$') { throw 'Paket sürümü geçerli bir semver olmalı.' }
foreach ($field in @('displayName', 'description', 'unity', 'documentationUrl', 'changelogUrl')) {
    if ([string]::IsNullOrWhiteSpace($package.$field)) { throw "Eksik manifest alanı: $field" }
}

$requiredDependencies = @('com.cysharp.unitask', 'jp.hadashikick.vcontainer', 'com.kameryurdakull.eventbus')
foreach ($name in $requiredDependencies) {
    if ($null -eq $package.dependencies.$name) { throw "Eksik zorunlu bağımlılık: $name" }
}
foreach ($dependency in $package.dependencies.PSObject.Properties) {
    if ($dependency.Value -notmatch '^\d+\.\d+\.\d+(-[0-9A-Za-z.-]+)?$') {
        throw "Paket bağımlılığı semver olmalı; Git URL proje manifest'ine aittir: $($dependency.Name)"
    }
}

foreach ($path in @('Runtime/AudioSystem.Runtime.asmdef', 'Editor/AudioSystem.Editor.asmdef',
        'Tests/Editor/AudioSystem.Tests.asmdef', 'README.md', 'CHANGELOG.md')) {
    if (-not (Test-Path -LiteralPath (Join-Path $packageRoot $path))) { throw "Eksik paket dosyası: $path" }
}

$assemblies = @{}
$guids = @{}
$checkedFiles = 0
foreach ($file in Get-ChildItem -LiteralPath $packageRoot -Recurse -File) {
    if ($file.Extension -eq '.asmdef') {
        $assembly = Get-Content -LiteralPath $file.FullName -Raw | ConvertFrom-Json
        if ($assemblies.ContainsKey($assembly.name)) { throw "Tekrarlanan assembly: $($assembly.name)" }
        $assemblies.Add($assembly.name, $file.FullName)
        if (-not ($assembly.references -contains 'AudioSystem.Runtime') -and $assembly.name -ne 'AudioSystem.Runtime') {
            throw "Runtime referansı eksik: $($assembly.name)"
        }
    }
    if ($file.Extension -in @('.cs', '.asmdef', '.asset', '.unity', '.wav')) {
        if (-not (Test-Path -LiteralPath ($file.FullName + '.meta'))) { throw "Eksik meta: $($file.FullName)" }
        $checkedFiles++
    }
    if ($file.Extension -eq '.meta') {
        $content = Get-Content -LiteralPath $file.FullName -Raw
        $match = [regex]::Match($content, '(?m)^guid: ([0-9a-f]{32})\r?$')
        if (-not $match.Success) { throw "Geçersiz GUID: $($file.FullName)" }
        $guid = $match.Groups[1].Value
        if ($guids.ContainsKey($guid)) { throw "Tekrarlanan GUID: $($file.FullName)" }
        $guids.Add($guid, $file.FullName)
    }
    if ($file.Extension -eq '.dll') { throw "Üçüncü parti DLL paketlenmemeli: $($file.FullName)" }
}

foreach ($sample in $package.samples) {
    if (-not $sample.path.StartsWith('Samples~/')) { throw 'Örnekler Samples~/ altında olmalı.' }
    $samplePath = [IO.Path]::GetFullPath((Join-Path $packageRoot $sample.path))
    $packagePrefix = [IO.Path]::GetFullPath($packageRoot) + [IO.Path]::DirectorySeparatorChar
    if (-not $samplePath.StartsWith($packagePrefix, [StringComparison]::OrdinalIgnoreCase)) { throw 'Örnek yolu paket dışına çıkıyor.' }
    if (-not (Test-Path -LiteralPath $samplePath)) { throw "Örnek bulunamadı: $($sample.path)" }
}

$runtimeAssembly = Get-Content -LiteralPath (Join-Path $packageRoot 'Runtime/AudioSystem.Runtime.asmdef') -Raw | ConvertFrom-Json
foreach ($reference in @('UniTask', 'VContainer', 'EventSystem')) {
    if (-not ($runtimeAssembly.references -contains $reference)) { throw "Runtime assembly referansı eksik: $reference" }
}
$tests = Get-Content -LiteralPath (Join-Path $packageRoot 'Tests/Editor/AudioSystem.Tests.asmdef') -Raw | ConvertFrom-Json
if (-not ($tests.includePlatforms -contains 'Editor')) { throw 'Paket testleri Editor ile sınırlandırılmalı.' }
if (-not ($tests.optionalUnityReferences -contains 'TestAssemblies')) { throw 'Test assembly işareti eksik.' }

Write-Output "Paket doğrulandı: $($package.name)@$($package.version)"
Write-Output "$checkedFiles asset/kod dosyası, $($assemblies.Count) assembly ve $($guids.Count) benzersiz GUID kontrol edildi."
