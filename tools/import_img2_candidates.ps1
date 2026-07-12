param(
    [string] $CandidateDirectory = 'generated_art/candidates/20260711_img2_1k',
    [string] $BackupDirectory = 'generated_art/review/formal_asset_backups/img2_import_20260711'
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$cardNames = @(
    'blood_blade',
    'discontinuous_pulse',
    'blood_veil',
    'blood_splash',
    'lacerate',
    'blood_shield',
    'blood_haste',
    'autophagy',
    'soul_blood_draw',
    'blood_borrow',
    'clotting_barrier',
    'sanguine_rite',
    'blood_feast',
    'farewell_finale',
    'spirit_and_desire_farewell',
    'blood_rebuild',
    'blood_demon_form'
)

$powerNames = @(
    'blood_clan_court_power',
    'blood_debt_strength_power',
    'blood_demon_form_power',
    'blood_feast_power',
    'bloodletting_strength_power',
    'candle_ember_power',
    'clot_instinct_power',
    'clotting_barrier_power',
    'crimson_ward_power',
    'ember_bloodline_power',
    'eternal_replete_power',
    'immortal_bloodline_power',
    'pain_conversion_power',
    'rose_step_power'
)

function Save-ResampledImage {
    param(
        [Parameter(Mandatory = $true)] [System.Drawing.Image] $Source,
        [Parameter(Mandatory = $true)] [string] $Destination,
        [Parameter(Mandatory = $true)] [int] $Width,
        [Parameter(Mandatory = $true)] [int] $Height,
        [Parameter(Mandatory = $true)] [System.Drawing.RectangleF] $SourceRectangle
    )

    $bitmap = [System.Drawing.Bitmap]::new($Width, $Height, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    try {
        $graphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
        $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
        $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $destinationRect = [System.Drawing.RectangleF]::new(0, 0, $Width, $Height)
        $graphics.DrawImage($Source, $destinationRect, $SourceRectangle, [System.Drawing.GraphicsUnit]::Pixel)
        $bitmap.Save($Destination, [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally {
        $graphics.Dispose()
        $bitmap.Dispose()
    }
}

$candidateRoot = (Resolve-Path -LiteralPath $CandidateDirectory).Path
$backupRoot = New-Item -ItemType Directory -Force -Path $BackupDirectory
$cardSmallRoot = New-Item -ItemType Directory -Force -Path 'Entelechia/images/card_portraits'
$cardBigRoot = New-Item -ItemType Directory -Force -Path 'Entelechia/images/card_portraits/big'
$powerSmallRoot = New-Item -ItemType Directory -Force -Path 'Entelechia/images/powers'
$powerBigRoot = New-Item -ItemType Directory -Force -Path 'Entelechia/images/powers/big'

foreach ($name in $cardNames) {
    $sourcePath = Join-Path $candidateRoot "$name.png"
    if (-not (Test-Path -LiteralPath $sourcePath)) {
        throw "Missing card candidate: $sourcePath"
    }

    $smallPath = Join-Path $cardSmallRoot.FullName "$name.png"
    $bigPath = Join-Path $cardBigRoot.FullName "$name.png"
    foreach ($existing in @($smallPath, $bigPath)) {
        if (Test-Path -LiteralPath $existing) {
            $relative = $existing.Substring((Resolve-Path 'Entelechia/images').Path.Length).TrimStart('\', '/')
            $backupPath = Join-Path $backupRoot.FullName $relative
            New-Item -ItemType Directory -Force -Path (Split-Path $backupPath) | Out-Null
            Copy-Item -LiteralPath $existing -Destination $backupPath -Force
        }
    }

    $source = [System.Drawing.Image]::FromFile($sourcePath)
    try {
        $targetRatio = 1000.0 / 760.0
        $cropWidth = [single] $source.Width
        $cropHeight = [single] ($source.Width / $targetRatio)
        $cropY = [single] (($source.Height - $cropHeight) / 2.0)
        $crop = [System.Drawing.RectangleF]::new(0, $cropY, $cropWidth, $cropHeight)
        Save-ResampledImage -Source $source -Destination $bigPath -Width 1000 -Height 760 -SourceRectangle $crop
    }
    finally {
        $source.Dispose()
    }

    $big = [System.Drawing.Image]::FromFile($bigPath)
    try {
        $full = [System.Drawing.RectangleF]::new(0, 0, $big.Width, $big.Height)
        Save-ResampledImage -Source $big -Destination $smallPath -Width 250 -Height 190 -SourceRectangle $full
    }
    finally {
        $big.Dispose()
    }
}

foreach ($name in $powerNames) {
    $sourcePath = Join-Path $candidateRoot "$name.png"
    if (-not (Test-Path -LiteralPath $sourcePath)) {
        throw "Missing Power candidate: $sourcePath"
    }

    $smallPath = Join-Path $powerSmallRoot.FullName "$name.png"
    $bigPath = Join-Path $powerBigRoot.FullName "$name.png"
    foreach ($existing in @($smallPath, $bigPath)) {
        if (Test-Path -LiteralPath $existing) {
            $relative = $existing.Substring((Resolve-Path 'Entelechia/images').Path.Length).TrimStart('\', '/')
            $backupPath = Join-Path $backupRoot.FullName $relative
            New-Item -ItemType Directory -Force -Path (Split-Path $backupPath) | Out-Null
            Copy-Item -LiteralPath $existing -Destination $backupPath -Force
        }
    }

    $source = [System.Drawing.Image]::FromFile($sourcePath)
    try {
        $full = [System.Drawing.RectangleF]::new(0, 0, $source.Width, $source.Height)
        Save-ResampledImage -Source $source -Destination $bigPath -Width 256 -Height 256 -SourceRectangle $full
        Save-ResampledImage -Source $source -Destination $smallPath -Width 64 -Height 64 -SourceRectangle $full
    }
    finally {
        $source.Dispose()
    }
}

Write-Host "Imported $($cardNames.Count) card masters and $($powerNames.Count) Power masters."
Write-Host "Backup: $($backupRoot.FullName)"
