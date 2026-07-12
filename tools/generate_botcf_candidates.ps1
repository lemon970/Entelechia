param(
    [Parameter(Mandatory = $true)]
    [string[]] $AssetNames,

    [string] $OutputDirectory = "generated_art/candidates/20260711_img2_1k",
    [string] $PromptDocument = "generated_art/review/IMAGE_RESOURCE_PROMPTS_20260711.md",
    [string] $CharacterReference = "",

    [switch] $DryRun
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($CharacterReference)) {
    $referenceCandidates = @(
        Get-ChildItem -LiteralPath 'D:/desktop/mod/pic' -File -Filter '*_1.png' |
            Where-Object { $_.Length -gt 500000 }
    )
    if ($referenceCandidates.Count -ne 1) {
        throw "Expected exactly one large *_1.png character reference; found $($referenceCandidates.Count)."
    }
    $CharacterReference = $referenceCandidates[0].FullName
}

if ([string]::IsNullOrWhiteSpace($env:BOTCF_API_KEY)) {
    throw "BOTCF_API_KEY is not set in this process."
}

$promptText = Get-Content -LiteralPath $PromptDocument -Raw -Encoding UTF8
$sharedMatch = [regex]::Match(
    $promptText,
    '(?s)## Shared visual specification\s+(?<body>.*?)(?=\r?\n## A\.)'
)
if (-not $sharedMatch.Success) {
    throw "Could not extract shared visual specification from $PromptDocument"
}

$sharedRules = $sharedMatch.Groups['body'].Value.Trim()
$characterAssets = @(
    'blood_blade',
    'blood_demon_form_power',
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
$outputRoot = New-Item -ItemType Directory -Force -Path $OutputDirectory

Add-Type -AssemblyName System.Net.Http
$client = [System.Net.Http.HttpClient]::new()
$client.Timeout = [TimeSpan]::FromMinutes(10)
$client.DefaultRequestHeaders.Authorization =
    [System.Net.Http.Headers.AuthenticationHeaderValue]::new('Bearer', $env:BOTCF_API_KEY)

function Get-AssetPrompt([string] $assetName) {
    $escaped = [regex]::Escape($assetName)
    $match = [regex]::Match(
        $promptText,
        "(?s)^### [ABC]\d{2} ``$escaped``\s+\*\*Prompt:\*\*\s*(?<prompt>.*?)(?=\r?\n\r?\n(?:\*\*Acceptance:\*\*|### |## ))",
        [Text.RegularExpressions.RegexOptions]::Multiline
    )
    if (-not $match.Success) {
        throw "Prompt not found for asset: $assetName"
    }

    $assetSharedRules = $sharedRules
    if ($assetName.EndsWith('_power')) {
        $assetSharedRules = [regex]::Replace(
            $assetSharedRules,
            '(?m)^- \*\*Card composition:\*\*.*(?:\r?\n|$)',
            ''
        )
    }
    else {
        $assetSharedRules = [regex]::Replace(
            $assetSharedRules,
            '(?m)^- \*\*Power icon composition:\*\*.*(?:\r?\n|$)',
            ''
        )
    }

    $referenceInstruction = ''
    if ($characterAssets -contains $assetName) {
        $referenceInstruction = @"

The uploaded standing portrait is an identity and design reference only. Preserve its face, hair, pointed ears, costume construction, body proportions, and exact scythe design, but create the new pose, framing, action, lighting, and background described above. Do not simply reproduce the reference pose or its empty background.
"@
    }

    return @"
$($match.Groups['prompt'].Value.Trim())
$referenceInstruction

Mandatory shared constraints:
$assetSharedRules

Output exactly one finished illustration. No labels, captions, borders, mockups, or explanation.
"@
}

function Save-ImageResponse([string] $json, [string] $destination) {
    $response = $json | ConvertFrom-Json
    if (-not $response.data -or $response.data.Count -lt 1) {
        throw "Image API returned no data item."
    }

    $item = $response.data[0]
    if (-not [string]::IsNullOrWhiteSpace($item.b64_json)) {
        [IO.File]::WriteAllBytes($destination, [Convert]::FromBase64String($item.b64_json))
        return
    }

    if (-not [string]::IsNullOrWhiteSpace($item.url)) {
        $bytes = $client.GetByteArrayAsync([string] $item.url).GetAwaiter().GetResult()
        [IO.File]::WriteAllBytes($destination, $bytes)
        return
    }

    throw "Image API returned neither b64_json nor url."
}

try {
    foreach ($assetName in $AssetNames) {
        $destination = Join-Path $outputRoot.FullName "$assetName.png"
        if (Test-Path -LiteralPath $destination) {
            Write-Host "SKIP existing: $destination"
            continue
        }

        $prompt = Get-AssetPrompt $assetName
        if ($DryRun) {
            Write-Host "DRY RUN $assetName"
            Write-Output $prompt
            continue
        }
        Write-Host "GENERATE $assetName"

        if ($characterAssets -contains $assetName) {
            if (-not (Test-Path -LiteralPath $CharacterReference)) {
                throw "Character reference not found: $CharacterReference"
            }

            $form = [System.Net.Http.MultipartFormDataContent]::new()
            try {
                $form.Add([System.Net.Http.StringContent]::new('gpt-image-2-1k'), 'model')
                $form.Add([System.Net.Http.StringContent]::new($prompt, [Text.Encoding]::UTF8), 'prompt')
                $form.Add([System.Net.Http.StringContent]::new('1024x1024'), 'size')
                $imageBytes = [IO.File]::ReadAllBytes((Resolve-Path -LiteralPath $CharacterReference))
                $imageContent = [System.Net.Http.ByteArrayContent]::new($imageBytes)
                $imageContent.Headers.ContentType =
                    [System.Net.Http.Headers.MediaTypeHeaderValue]::new('image/png')
                $form.Add($imageContent, 'image', [IO.Path]::GetFileName($CharacterReference))

                $httpResponse = $client.PostAsync('https://botcf.com/v1/images/edits', $form).GetAwaiter().GetResult()
                $responseJson = $httpResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult()
                if (-not $httpResponse.IsSuccessStatusCode) {
                    throw "Image edit failed ($([int]$httpResponse.StatusCode)): $responseJson"
                }
                Save-ImageResponse $responseJson $destination
            }
            finally {
                $form.Dispose()
            }
        }
        else {
            $body = @{
                model = 'gpt-image-2-1k'
                prompt = $prompt
                size = '1024x1024'
            } | ConvertTo-Json -Depth 5
            $content = [System.Net.Http.StringContent]::new($body, [Text.Encoding]::UTF8, 'application/json')
            try {
                $httpResponse = $client.PostAsync('https://botcf.com/v1/images/generations', $content).GetAwaiter().GetResult()
                $responseJson = $httpResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult()
                if (-not $httpResponse.IsSuccessStatusCode) {
                    throw "Image generation failed ($([int]$httpResponse.StatusCode)): $responseJson"
                }
                Save-ImageResponse $responseJson $destination
            }
            finally {
                $content.Dispose()
            }
        }

        $written = Get-Item -LiteralPath $destination
        Write-Host "WROTE $($written.FullName) ($($written.Length) bytes)"
    }
}
finally {
    $client.Dispose()
}
