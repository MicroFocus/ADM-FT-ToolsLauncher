#### START - NEED TO MODIFY ON EVERY RELEASE ####
$Path = 'C:\work\_gitRepos\FTTools\bin\BUNDLE\FTTools-v1.0-beta-rev6'

$tags = @{
    FTToolsLauncher = "v1.0-beta-rev6";
    FTToolsAborter = "v1.0-beta";
    LRAnalysisLauncher = "v1.0-beta";
    ReportConverter = "v1.0-beta-rev5"
}

$newFeatures = @(
    # 'new feature line 1 ...'
    # 'new feature line 2 ...'
)

$bugFixes = @(
    # '#123'
    '#47'
)

$minorChanges = @(
    # 'change1'
)

#### END - NEED TO MODIFY ON EVERY RELEASE ####


Write-Host ('### Release Notes')

# new features?
if ($newFeatures.Length -gt 0) {
    Write-Host ('#### New features')
    foreach ($feature in $newFeatures) {
        Write-Host ("- {0}" -f $feature)
    }
}

# bug fixes
if ($bugFixes.Length -gt 0) {
    Write-Host ('#### Bug fixes')
    foreach ($fix in $bugFixes) {
        Write-Host ("- {0}" -f $fix)
    }
}

# minor changes
if ($minorChanges.Length -gt 0) {
    Write-Host ('#### Minor changes')
    foreach ($change in $minorChanges) {
        Write-Host ("- {0}" -f $change)
    }
}

Write-Host ('')
Write-Host ('### Tools version')
Write-Host ('The following tools are available in this release:')
Write-Host ('| Tool Name & Version | OS (Arch.) |  .NET Runtime | File Spec |')
Write-Host('| ---- | ---- | ---- | ---- |')

# write lines
$toolFiles = Join-Path -Path $Path -ChildPath "*" -Resolve
foreach ($f in $toolFiles) {
    $fileName = [System.IO.Path]::GetFileNameWithoutExtension($f)
    
    $ver = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($f).FileVersion
    if ([string]::IsNullOrWhiteSpace($ver)) { $ver = "???" }

    $nameParts = $fileName.Split('_')
    $toolName = $nameParts[0]

    $tag = $tags[$toolName]
    if ([string]::IsNullOrWhiteSpace($tag)) { $tag = "???" }

    $netfrm = ''
    $osArch = 'win'
    if ($nameParts.Length -ge 2) { $netfrm = $nameParts[1] }
    if ($nameParts.Length -ge 3) { $osArch = $nameParts[2] }

    $osArchParts = $osArch.Split('-')
    $os = "win"
    $arch = "x64, x86"
    if ($osArchParts.Length -ge 1) { $os = $osArchParts[0] }
    if ($osArchParts.Length -ge 2) { $arch = $osArchParts[1] }

    $osDisplay = "Windows"
    if ($os -ieq 'linux') { $osDisplay = 'Linux' }
    if ($os -ieq 'osx') { $osDisplay = 'Mac OS' }

    $netfrmDisplay = ".NET Framework 4.0+"
    if ($netfrm -ieq 'net4') { $netfrmDisplay = '.NET Framework 4.0' }
    if ($netfrm -ieq 'net45') { $netfrmDisplay = '.NET Framework 4.5' }
    if ($netfrm -ieq 'net452') { $netfrmDisplay = '.NET Framework 4.5.2' }
    if ($netfrm -ieq 'net46') { $netfrmDisplay = '.NET Framework 4.6' }
    if ($netfrm -ieq 'net461') { $netfrmDisplay = '.NET Framework 4.6.1' }
    if ($netfrm -ieq 'net472') { $netfrmDisplay = '.NET Framework 4.7.2' }
    if ($netfrm -ieq 'net48') { $netfrmDisplay = '.NET Framework 4.8' }
    if ($netfrm -ieq 'net5') { $netfrmDisplay = '.NET 5.0' }

    $size = (Get-Item $f).Length

    $md5 = (Get-FileHash $f -Algorithm MD5).Hash
    $sha1 = (Get-FileHash $f -Algorithm SHA1).Hash
    $sha256 = (Get-FileHash $f -Algorithm SHA256).Hash

    Write-Host ("| **{0}**<br/>__v{1}__<br/>`{2}` | {3}<br/>{4} | {5} | __Size__: `{6}` bytes<br/>__MD5__: `{7}`<br/>__SHA1__: `{8}`<br/>__SHA256__: `{9}` |" -f $fileName, $ver, $tag, $osDisplay, $arch, $netfrmDisplay, $size, $md5, $sha1, $sha256)
}
