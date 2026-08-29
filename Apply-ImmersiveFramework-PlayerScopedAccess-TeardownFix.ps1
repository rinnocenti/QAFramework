[CmdletBinding(PositionalBinding=$false)]
param(
    [Parameter(Mandatory=$false)]
    [string]$Root
)

$ErrorActionPreference = "Stop"

$relative = "Runtime/PlayerParticipation/Authoring/PlayerSessionScopedAccessConsumer.cs"

function Resolve-FrameworkRoot {
    param([string]$ExplicitRoot)

    $candidates = New-Object System.Collections.Generic.List[string]

    if (-not [string]::IsNullOrWhiteSpace($ExplicitRoot)) {
        $candidates.Add($ExplicitRoot)
    }

    $candidates.Add((Get-Location).Path)

    if (-not [string]::IsNullOrWhiteSpace($PSScriptRoot)) {
        $candidates.Add($PSScriptRoot)

        $parent = Split-Path -Parent $PSScriptRoot
        if (-not [string]::IsNullOrWhiteSpace($parent)) {
            $candidates.Add($parent)
        }
    }

    $candidates.Add("C:\Projetos\ImmersivePackages\com.immersive.framework")

    foreach ($candidate in $candidates) {
        if ([string]::IsNullOrWhiteSpace($candidate)) {
            continue
        }

        try {
            $full = [System.IO.Path]::GetFullPath($candidate)
        }
        catch {
            continue
        }

        $probe = Join-Path $full $relative
        if (Test-Path -LiteralPath $probe) {
            return $full
        }
    }

    throw @"
Could not locate Immersive Framework root.

Expected file:
  $relative

Run this script from:
  C:\Projetos\ImmersivePackages\com.immersive.framework

or pass:
  -Root "C:\Projetos\ImmersivePackages\com.immersive.framework"
"@
}

function Read-Source {
    param([string]$Path)

    $bytes = [System.IO.File]::ReadAllBytes($Path)
    $hasBom = $bytes.Length -ge 3 -and
        $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF

    $text = [System.Text.Encoding]::UTF8.GetString($bytes)
    if ($hasBom -and $text.Length -gt 0 -and $text[0] -eq [char]0xFEFF) {
        $text = $text.Substring(1)
    }

    return [PSCustomObject]@{
        Text = $text
        HasBom = $hasBom
        NewLine = $(if ($text.Contains("`r`n")) { "`r`n" } else { "`n" })
    }
}

function Replace-ExactlyOnce {
    param(
        [string]$Text,
        [string]$Old,
        [string]$New,
        [string]$Label
    )

    if ($Text.Contains($New) -and -not $Text.Contains($Old)) {
        return $Text
    }

    $count = ([regex]::Matches($Text, [regex]::Escape($Old))).Count
    if ($count -ne 1) {
        throw "[$Label] Expected exactly one source block, found $count. No file was written."
    }

    return $Text.Replace($Old, $New)
}

$resolvedRoot = Resolve-FrameworkRoot -ExplicitRoot $Root
$path = Join-Path $resolvedRoot $relative
$source = Read-Source $path
$text = $source.Text
$nl = $source.NewLine

$oldReleaseHeader = @'
        internal void ReleaseScopedAccess(string reason, bool isStale = false)
        {
            string resolvedReason = string.IsNullOrWhiteSpace(reason)
'@ -replace "`r?`n", $nl

$newReleaseHeader = @'
        internal void ReleaseScopedAccess(string reason, bool isStale = false)
        {
            // A scene consumer may be destroyed before the persistent Runtime Host.
            // OnDestroy already releases the bound access on the consumer side, so a
            // later owner-side release must treat a Unity-destroyed wrapper as done.
            if (this == null)
            {
                return;
            }

            string resolvedReason = string.IsNullOrWhiteSpace(reason)
'@ -replace "`r?`n", $nl

$text = Replace-ExactlyOnce `
    -Text $text `
    -Old $oldReleaseHeader `
    -New $newReleaseHeader `
    -Label "release-destroyed-consumer-guard"

$oldBuildFields = @'
        private LogField[] BuildFields(string status, LocalPlayerProvisioningConsumerScope runtimeScope, string reason)
        {
            return LogFields.Of(
                LogFields.Field("component", name),
                LogFields.Field("scene", gameObject.scene.name),
                LogFields.Field("status", status),
                LogFields.Field("authoredScope", scope),
                LogFields.Field("runtimeScope", runtimeScope),
                LogFields.Field("bindingState", _bindingState),
                LogFields.Field("message", reason ?? string.Empty));
        }
'@ -replace "`r?`n", $nl

$newBuildFields = @'
        private LogField[] BuildFields(string status, LocalPlayerProvisioningConsumerScope runtimeScope, string reason)
        {
            // Diagnostics must remain side-effect free during Unity teardown. A
            // managed wrapper can outlive its native Unity object, so never read
            // Unity-backed properties after the object has become fake-null.
            string componentName = this != null
                ? name
                : GetType().Name;
            string sceneName = this != null && gameObject != null
                ? gameObject.scene.name
                : string.Empty;

            return LogFields.Of(
                LogFields.Field("component", componentName),
                LogFields.Field("scene", sceneName),
                LogFields.Field("status", status),
                LogFields.Field("authoredScope", scope),
                LogFields.Field("runtimeScope", runtimeScope),
                LogFields.Field("bindingState", _bindingState),
                LogFields.Field("message", reason ?? string.Empty));
        }
'@ -replace "`r?`n", $nl

$text = Replace-ExactlyOnce `
    -Text $text `
    -Old $oldBuildFields `
    -New $newBuildFields `
    -Label "teardown-safe-diagnostics"

# Final invariants before writing.
$required = @(
    "if (this == null)",
    "A scene consumer may be destroyed before the persistent Runtime Host.",
    "string componentName = this != null",
    "string sceneName = this != null && gameObject != null"
)

foreach ($token in $required) {
    if (-not $text.Contains($token)) {
        throw "Required teardown fix token '$token' is missing. No file was written."
    }
}

$forbidden = @(
    'LogFields.Field("component", name)',
    'LogFields.Field("scene", gameObject.scene.name)'
)

foreach ($token in $forbidden) {
    if ($text.Contains($token)) {
        throw "Unsafe teardown diagnostic token '$token' remains. No file was written."
    }
}

$encoding = New-Object System.Text.UTF8Encoding($source.HasBom)
[System.IO.File]::WriteAllText($path, $text, $encoding)

Write-Host ""
Write-Host "Immersive Framework root: $resolvedRoot"
Write-Host "Target: $relative"
Write-Host "Status: applied Player scoped-access teardown fix."
Write-Host ""
Write-Host "No QA, scene, prefab, asset or documentation files were modified."
