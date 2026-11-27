[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$ProjectPath,
    [Parameter(Mandatory)][string]$TargetVersion,
    [Parameter(Mandatory)][string]$ExecuteMethod,
    [Parameter(Mandatory)][string]$OutputFile,
    [Parameter(Mandatory)][string]$LogPath,
    [string]$UnityVersion,
    [string]$UnityEditorRoot = $env:UNITY_EDITOR_ROOT,
    [string]$UnityEditorPath,
    [switch]$UseGraphicsDevice
)
$ErrorActionPreference = 'Stop'
if ($TargetVersion -notmatch '^\d+\.\d+\.\d+$') { throw 'TargetVersion must identify a supported game target.' }
if (!$UnityVersion) {
    $UnityVersion = switch ($TargetVersion) {
        '0.11.2' { '2018.1.6f1'; break }
        '1.11.1' { '2019.3.2f1'; break }
        { $_ -in '1.29.0','1.29.1' } { '2019.4.28f1'; break }
        '1.46.0' { '6000.3.19f1'; break }
        default { throw 'Configure UnityVersion explicitly for this asset target.' }
    }
}
if ($UnityVersion -notmatch '^\d+\.\d+\.\d+[abfp]\d+$') { throw 'UnityVersion must be a complete editor version.' }
if ($ExecuteMethod -notmatch '^[A-Za-z_][A-Za-z0-9_.]*$') { throw 'ExecuteMethod must be a static editor method name.' }
$project = [IO.Path]::GetFullPath($ProjectPath)
$output = [IO.Path]::GetFullPath($OutputFile)
$log = [IO.Path]::GetFullPath($LogPath)
$versionFile = Join-Path $project 'ProjectSettings/ProjectVersion.txt'
if (!(Test-Path -LiteralPath (Join-Path $project 'Assets') -PathType Container) -or !(Test-Path -LiteralPath $versionFile -PathType Leaf)) {
    throw 'ProjectPath must be a source Unity asset project.'
}
$sourceVersion = [regex]::Match([IO.File]::ReadAllText($versionFile), '(?m)^m_EditorVersion:\s*(\S+)').Groups[1].Value
if ($sourceVersion -ne $UnityVersion) {
    throw "Asset source project targets Unity $sourceVersion, but $TargetVersion requires the configured Unity $UnityVersion builder. Supply compatible source; opening a newer project in an older editor is not a conversion."
}
$requiredFamily = switch ($TargetVersion) {
    '0.11.2' { '2018.1.6f1'; break }
    '1.11.1' { '2019.3.'; break }
    { $_ -in '1.29.0','1.29.1' } { '2019.4.'; break }
    '1.46.0' { '6000.3.'; break }
    default { $null }
}
if ($requiredFamily -and !$UnityVersion.StartsWith($requiredFamily, [StringComparison]::Ordinal)) { throw 'Configured Unity editor does not match the target engine family.' }
if (!$UnityEditorPath) {
    if (!$UnityEditorRoot) { throw 'Set UnityEditorRoot or UnityEditorPath.' }
    $UnityEditorPath = Join-Path $UnityEditorRoot "$UnityVersion/Editor/Unity.exe"
}
$editor = [IO.Path]::GetFullPath($UnityEditorPath)
if (!(Test-Path -LiteralPath $editor -PathType Leaf)) { throw 'The selected Unity editor is not installed.' }
$editorVersion = (Get-Item -LiteralPath $editor).VersionInfo.ProductVersion
$legacyFileVersion = $UnityVersion -eq '2018.1.6f1' -and $editorVersion -match '^2018\.1\.6\.\d+$'
if (!$legacyFileVersion -and !$editorVersion.StartsWith($UnityVersion, [StringComparison]::Ordinal)) {
    throw "Editor executable reports $editorVersion instead of $UnityVersion."
}
if ($log -eq $output -or $output -eq $editor -or $log -eq $editor) { throw 'Build output, log and editor paths must be distinct.' }
foreach ($path in @($project, $output, $log, $editor)) {
    if ($path.Contains('"')) { throw 'Build paths cannot contain quotes.' }
}
$lockPath = Join-Path $project 'Temp/UnityLockfile'
if (Test-Path -LiteralPath $lockPath) {
    try { $lock = [IO.File]::Open($lockPath, 'Open', 'ReadWrite', 'None'); $lock.Dispose() }
    catch { throw 'Asset project is already open; use an isolated asset checkout.' }
}
[IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($log)) | Out-Null
$before = [DateTime]::UtcNow
$graphics = if ($UseGraphicsDevice) { '-force-d3d11' } else { '-nographics' }
$arguments = '-batchmode ' + $graphics + ' -quit -projectPath "' + $project + '" -executeMethod ' + $ExecuteMethod + ' -logFile "' + $log + '"'
$process = Start-Process -FilePath $editor -ArgumentList $arguments -WindowStyle Hidden -PassThru
$process.WaitForExit()
if ($process.ExitCode -ne 0) { throw "Asset output build failed ($($process.ExitCode)). Log: $log" }
if (!(Test-Path -LiteralPath $output -PathType Leaf) -or (Get-Item -LiteralPath $output).LastWriteTimeUtc -lt $before) {
    throw 'The exporter did not produce a fresh bundle at OutputFile.'
}
$stream = [IO.File]::OpenRead($output)
try {
    $header = New-Object byte[] 512
    $count = $stream.Read($header, 0, $header.Length)
    $text = [Text.Encoding]::ASCII.GetString($header, 0, $count)
} finally { $stream.Dispose() }
if (!$text.StartsWith("UnityFS`0", [StringComparison]::Ordinal) -or !$text.Contains("`0$UnityVersion`0")) {
    throw 'Bundle header does not match the configured compiler; the output will not be accepted.'
}
@{
    targetVersion=$TargetVersion; unityVersion=$UnityVersion; sourceProject=$project; editor=$editor
    executeMethod=$ExecuteMethod; output=$output; sha256=(Get-FileHash -LiteralPath $output -Algorithm SHA256).Hash
    log=$log; completedUtc=[DateTime]::UtcNow.ToString('o'); deployed=$false
} | ConvertTo-Json | Set-Content -LiteralPath ($log+'.build.json') -Encoding utf8
Write-Output "Built $output with Unity $UnityVersion for $TargetVersion."
