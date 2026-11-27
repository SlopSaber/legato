[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$SourceDirectory,
    [Parameter(Mandatory)][string]$WorkspacePath,
    [Parameter(Mandatory)][string]$TargetVersion,
    [Parameter(Mandatory)][string]$BundleName,
    [Parameter(Mandatory)][string]$OutputDirectory,
    [Parameter(Mandatory)][string]$LogPath,
    [string]$PackageManifestPath,
    [string]$UnityEditorRoot = $env:UNITY_EDITOR_ROOT,
    [string]$UnityEditorPath
)
$ErrorActionPreference = 'Stop'
$source = [IO.Path]::GetFullPath($SourceDirectory).TrimEnd('/','\')
$workspace = [IO.Path]::GetFullPath($WorkspacePath).TrimEnd('/','\')
$output = [IO.Path]::GetFullPath($OutputDirectory)
if ($BundleName -notmatch '^[a-z0-9][a-z0-9_.-]*$' -or $BundleName -in '.','..') { throw 'BundleName must be a simple lowercase asset-bundle filename.' }
$versions = [xml](Get-Content -LiteralPath (Join-Path $PSScriptRoot 'Legato.Versions.props') -Raw)
$profile = @($versions.Project.ItemGroup.LegatoPortableShaderProfile | Where-Object { ($_.Include -split ';') -contains $TargetVersion })
if ($profile.Count -ne 1) { throw "Portable shaders have no exact profile for Beat Saber $TargetVersion." }
$unityVersion = $profile[0].UnityVersion
$urp = $profile[0].Pipeline -eq 'Universal'
if (!(Test-Path -LiteralPath $source -PathType Container)) { throw 'SourceDirectory is missing.' }
if ($workspace -eq $source -or $workspace.StartsWith($source + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase) -or
    $source.StartsWith($workspace + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Shader sources and the generated asset workspace must not contain one another.'
}
$marker = Join-Path $workspace '.legato-portable-shaders.json'
if ((Test-Path -LiteralPath $workspace) -and !(Test-Path -LiteralPath $marker)) {
    throw 'WorkspacePath must be new or an existing Legato portable-shader workspace.'
}
$previous = $null
if (Test-Path -LiteralPath $marker) {
    $previous = Get-Content -LiteralPath $marker -Raw | ConvertFrom-Json
    if ($previous.source -ne $source -or $previous.unityVersion -ne $unityVersion) { throw 'Use a separate workspace for each source set and engine version.' }
}
$lockPath = Join-Path $workspace 'Temp/UnityLockfile'
if (Test-Path -LiteralPath $lockPath) {
    try { $lock = [IO.File]::Open($lockPath, 'Open', 'ReadWrite', 'None'); $lock.Dispose() }
    catch { throw 'Generated shader workspace is open.' }
}
$manifest = '{"dependencies":{"com.unity.modules.assetbundle":"1.0.0","com.unity.modules.vr":"1.0.0","com.unity.modules.xr":"1.0.0"}}'
if ($urp) {
    if (!$PackageManifestPath) { throw 'The URP target requires the maintained source project PackageManifestPath.' }
    $manifest = Get-Content -LiteralPath $PackageManifestPath -Raw
    $packages = $manifest | ConvertFrom-Json
    if (!$packages.dependencies.'com.unity.render-pipelines.universal' -and !$profile[0].UrpVersion) { throw 'The package manifest does not declare URP.' }
    if ($profile[0].UrpVersion -and $packages.dependencies.'com.unity.render-pipelines.universal' -ne $profile[0].UrpVersion) {
        $packages.dependencies | Add-Member -NotePropertyName 'com.unity.render-pipelines.universal' -NotePropertyValue $profile[0].UrpVersion -Force
        $manifest = $packages | ConvertTo-Json -Depth 16
    }
}
$files = @(Get-ChildItem -LiteralPath $source -Recurse -File | Where-Object {
    $_.Extension -in '.shader','.hlsl','.cginc' -or $_.Name -match '\.(shader|hlsl|cginc)\.meta$'
} | Sort-Object FullName)
if (@($files | Where-Object Extension -eq '.shader').Count -eq 0) { throw 'No shader sources found.' }
$prepared = @{}
foreach ($file in $files) {
    if (($file.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw 'Source file links are not supported.' }
    $relative = [IO.Path]::GetRelativePath($source, $file.FullName).Replace('\','/')
    $content = [IO.File]::ReadAllText($file.FullName)
    if ($file.Extension -ne '.meta') {
        if ($unityVersion.StartsWith('2018.', [StringComparison]::Ordinal)) {
            $content = [regex]::Replace($content, '(?m)^([ \t]*#pragma[ \t]+(?:multi_compile|shader_feature))_local(?=[ \t])', '$1')
        }
        $content = $content.Replace('Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl', 'Assets/Legato/Core.hlsl')
        $content = $content.Replace('Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl', 'Assets/Legato/Blit.hlsl')
        if (!$urp) {
            $content = [regex]::Replace($content, '(?ms)^[ \t]*// LEGATO_URP_PASS_BEGIN[ \t]*\r?\n.*?^[ \t]*// LEGATO_URP_PASS_END[ \t]*(?:\r?\n|\z)', '')
            if ($content -match 'LEGATO_URP_PASS_(?:BEGIN|END)') { throw "Unpaired URP pass markers in $relative." }
            $content = $content.Replace('"UnityUI.cginc"', '"Assets/Legato/UnityUICompatibility.cginc"')
            if ($content.Contains('TMPro_Properties.cginc')) {
                # Standard TMP SDF shaders moved signed font scale from UV2.y to UV0.w.
                $content = $content.Replace('step(input.texcoord0.w,', 'step(input.texcoord1.y,')
                $content = $content.Replace('abs(input.texcoord0.w)', 'abs(input.texcoord1.y)')
            }
            if ($content -match 'Packages/com\.unity\.render-pipelines\.') {
                throw "Unsupported pipeline include in $relative. Add an explicit shared adapter before backporting this shader."
            }
            $content = [regex]::Replace($content, '"RenderPipeline"\s*=\s*"UniversalPipeline"', '')
            $content = [regex]::Replace($content, '("LightMode"\s*=\s*")UniversalForward(?:Only)?"', '${1}ForwardBase"')
            $content = [regex]::Replace($content, '("LightMode"\s*=\s*")DepthOnly"', '${1}ShadowCaster"')
            $content = [regex]::Replace($content, '("LightMode"\s*=\s*")SRPDefaultUnlit"', '${1}Always"')
            foreach ($match in [regex]::Matches($content, '"LightMode"\s*=\s*"([^"]+)"')) {
                if ($match.Groups[1].Value -notin 'ForwardBase','ShadowCaster','Always') {
                    throw "Unsupported pass '$($match.Groups[1].Value)' in $relative."
                }
            }
        }
    }
    $prepared['Assets/Content/' + $relative] = $content
}
$legato = Split-Path $PSScriptRoot -Parent
foreach ($name in 'Core.hlsl','Blit.hlsl','UnityUICompatibility.cginc') { $prepared['Assets/Legato/' + $name] = [IO.File]::ReadAllText((Join-Path $legato ('ShaderLibrary/' + $name))) }
$prepared['Assets/Legato/Pipeline.hlsl'] = '#define LEGATO_URP ' + [int]$urp + "`n"
if ($profile[0].SideBySideMirrors -eq 'true') { $prepared['Assets/Legato/Pipeline.hlsl'] += "#define LEGATO_SIDE_BY_SIDE_MIRROR 1`n" }
$prepared['Assets/Editor/BuildPortableShaders.cs'] = [IO.File]::ReadAllText((Join-Path $legato 'AssetBuild/Editor/BuildPortableShaders.cs'))
$prepared['ProjectSettings/ProjectVersion.txt'] = "m_EditorVersion: $unityVersion`n"
$prepared['Packages/manifest.json'] = $manifest
$prepared['legato-shaders.json'] = @{
    bundleName=$BundleName; outputDirectory=$output
    singlePassStereo=($profile[0].SinglePassStereo -eq 'true')
    shaders=@($prepared.Keys | Where-Object { $_.EndsWith('.shader') } | Sort-Object)
} | ConvertTo-Json -Depth 4
[IO.Directory]::CreateDirectory($workspace) | Out-Null
foreach ($relative in @($prepared.Keys) + @($previous.files)) {
    if (!$relative) { continue }
    $path = [IO.Path]::GetFullPath((Join-Path $workspace $relative))
    if (!$path.StartsWith($workspace + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'A generated file path escapes WorkspacePath.'
    }
    for ($ancestor = $path; $ancestor; $ancestor = [IO.Path]::GetDirectoryName($ancestor)) {
        if ((Test-Path -LiteralPath $ancestor) -and ((Get-Item -LiteralPath $ancestor -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw 'Generated workspace paths must not traverse filesystem links.'
        }
    }
}
foreach ($relative in @($previous.files)) {
    if ($relative -and !$prepared.ContainsKey($relative)) {
        $path = Join-Path $workspace $relative
        if (Test-Path -LiteralPath $path -PathType Leaf) { Remove-Item -LiteralPath $path }
        if (Test-Path -LiteralPath ($path + '.meta') -PathType Leaf) { Remove-Item -LiteralPath ($path + '.meta') }
    }
}
foreach ($relative in $prepared.Keys) {
    $path = Join-Path $workspace $relative
    [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($path)) | Out-Null
    if (!(Test-Path -LiteralPath $path) -or [IO.File]::ReadAllText($path) -ne $prepared[$relative]) {
        [IO.File]::WriteAllText($path, $prepared[$relative])
    }
}
@{source=$source; unityVersion=$unityVersion; files=@($prepared.Keys)} | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $marker -Encoding utf8
$parameters = @{
    ProjectPath=$workspace; TargetVersion=$TargetVersion; UnityVersion=$unityVersion
    ExecuteMethod='Legato.AssetBuild.BuildPortableShaders.Build'; OutputFile=(Join-Path $output $BundleName)
    LogPath=$LogPath; UnityEditorRoot=$UnityEditorRoot
}
if ($UnityEditorPath) { $parameters.UnityEditorPath=$UnityEditorPath }
& (Join-Path $PSScriptRoot 'Build-AssetBundle.ps1') @parameters
$receiptPath = [IO.Path]::GetFullPath($LogPath) + '.build.json'
$receipt = Get-Content -LiteralPath $receiptPath -Raw | ConvertFrom-Json
$receipt | Add-Member -NotePropertyName shaderSourceDirectory -NotePropertyValue $source
$receipt | Add-Member -NotePropertyName pipeline -NotePropertyValue $(if ($urp) { 'URP' } else { 'BuiltIn' })
$receipt | Add-Member -NotePropertyName generatedInputs -NotePropertyValue @(
    foreach ($relative in ($prepared.Keys | Sort-Object)) {
        @{path=$relative; sha256=(Get-FileHash -LiteralPath (Join-Path $workspace $relative) -Algorithm SHA256).Hash}
    }
)
$receipt | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $receiptPath -Encoding utf8
