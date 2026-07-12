$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$driverPath = Join-Path $root 'EntelechiaCode/Animation/EntelechiaCombatAnimationDriver.cs'
$patchesPath = Join-Path $root 'EntelechiaCode/CombatPatches.cs'
$characterPath = Join-Path $root 'EntelechiaCode/Character/Entelechia.cs'

$driver = Get-Content -LiteralPath $driverPath -Raw
$patches = Get-Content -LiteralPath $patchesPath -Raw
$character = Get-Content -LiteralPath $characterPath -Raw
$failures = [System.Collections.Generic.List[string]]::new()

function Assert-Pattern {
    param(
        [string]$Text,
        [string]$Pattern,
        [string]$Message
    )

    if ($Text -cnotmatch $Pattern) {
        $failures.Add($Message)
    }
}

Assert-Pattern $driver 'public const float DeathDuration = 1\.10f;' 'Death duration must remain 1.10 seconds and be shared with the hook wait.'
Assert-Pattern $driver 'public void PlayDeath\(\)' 'Animation driver must expose PlayDeath().'
Assert-Pattern $driver '\[AnimationDriver\] PlayDeath start' 'PlayDeath must emit a runtime marker.'
Assert-Pattern $driver 'new Vector2\(0f, 28f\)' 'Death must move the body down by 28 pixels.'
Assert-Pattern $driver 'Mathf\.DegToRad\(3f\)' 'Death must rotate the body by 3 degrees.'
Assert-Pattern $driver 'Vector2\.One \* 0\.26f' 'Death blood drops must reach scale 0.26.'
Assert-Pattern $driver '"modulate:a", 0\.50f' 'Death blood drops must peak at alpha 0.50.'
Assert-Pattern $driver 'public void CleanupForCombatEnd\(\)' 'Driver must expose combat-end cleanup.'
Assert-Pattern $driver '_body\.Visible = false;' 'Combat-end cleanup must hide the body.'
Assert-Pattern $driver 'ResetVfx\(_slashArc\);[\s\S]*ResetVfx\(_impactBurst\);[\s\S]*ResetVfx\(_bloodDrops\);' 'Cleanup must hide every VFX sprite.'

Assert-Pattern $patches 'HarmonyPatch\(typeof\(Hook\), nameof\(Hook\.AfterDeath\)\)' 'Combat patches must use Hook.AfterDeath.'
Assert-Pattern $patches 'if \(!wasRemovalPrevented &&' 'Prevented removal must not play the death animation.'
Assert-Pattern $patches 'driver => driver\.PlayDeath\(\)' 'AfterDeath must call PlayDeath().'
Assert-Pattern $patches 'Cmd\.Wait\(\s*MathF\.Max\(deathAnimLength, EntelechiaCombatAnimationDriver\.DeathDuration\),\s*ignoreCombatEnd: true\)' 'AfterDeath must wait for the longer official/custom death duration.'
Assert-Pattern $patches 'if \(TryPlayCreatureAnimation\(creature, "PlayDeath", driver => driver\.PlayDeath\(\)\)\)' 'Death wait must only apply when a Entelechia animation driver was found.'
Assert-Pattern $patches 'private static bool TryPlayCreatureAnimation\(' 'Animation lookup must report whether it actually started an effect.'
Assert-Pattern $patches 'Task\.WhenAll\(__result, visualWait\)' 'Official death hooks and the visual wait must run concurrently.'
Assert-Pattern $patches 'HarmonyPatch\(typeof\(Hook\), nameof\(Hook\.AfterCombatEnd\)\)' 'Combat patches must use Hook.AfterCombatEnd.'
Assert-Pattern $patches 'driver => driver\.CleanupForCombatEnd\(\)' 'AfterCombatEnd must clean the animation driver.'

Assert-Pattern $character '20260710-statefx-death-cleanup-phase6' 'Visual diagnostic marker must identify Phase 6.'

if ($failures.Count -gt 0) {
    Write-Host "Phase 6 contract failed ($($failures.Count) issue(s)):" -ForegroundColor Red
    foreach ($failure in $failures) {
        Write-Host "- $failure" -ForegroundColor Red
    }
    exit 1
}

Write-Host 'Phase 6 death/cleanup contract passed.' -ForegroundColor Green
