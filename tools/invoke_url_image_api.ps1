param(
    [Parameter(Mandatory = $true)] [string] $Prompt,
    [Parameter(Mandatory = $true)] [string] $OutputPath,
    [string] $Model = 'gpt-image-2',
    [string] $Size = '1024x1024',
    [string] $Quality = 'high',
    [string] $ImagePath = '',
    [string] $MaskPath = ''
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Net.Http

$key = [Environment]::GetEnvironmentVariable('IMAGE_API_KEY', 'User')
$baseUrl = [Environment]::GetEnvironmentVariable('IMAGE_API_BASE_URL', 'User')
if ([string]::IsNullOrWhiteSpace($key) -or [string]::IsNullOrWhiteSpace($baseUrl)) {
    throw 'IMAGE_API_KEY or IMAGE_API_BASE_URL is not configured.'
}

$handler = [System.Net.Http.HttpClientHandler]::new()
$handler.UseProxy = $false
$client = [System.Net.Http.HttpClient]::new($handler)
$client.Timeout = [TimeSpan]::FromMinutes(10)
$client.DefaultRequestHeaders.Authorization =
    [System.Net.Http.Headers.AuthenticationHeaderValue]::new('Bearer', $key)
$client.DefaultRequestHeaders.UserAgent.ParseAdd('Mozilla/5.0 img-cli/1.0')

function Save-ApiImage([string] $responseJson, [string] $destination) {
    $response = $responseJson | ConvertFrom-Json
    if (-not $response.data -or $response.data.Count -lt 1) {
        throw 'Image API returned no data item.'
    }

    $item = $response.data[0]
    New-Item -ItemType Directory -Force -Path (Split-Path $destination) | Out-Null
    if (-not [string]::IsNullOrWhiteSpace($item.url)) {
        $bytes = $client.GetByteArrayAsync([string] $item.url).GetAwaiter().GetResult()
        [IO.File]::WriteAllBytes($destination, $bytes)
        return
    }
    if (-not [string]::IsNullOrWhiteSpace($item.b64_json)) {
        [IO.File]::WriteAllBytes($destination, [Convert]::FromBase64String($item.b64_json))
        return
    }
    throw 'Image API returned neither url nor b64_json.'
}

try {
    if ([string]::IsNullOrWhiteSpace($ImagePath)) {
        $payload = @{
            model = $Model
            prompt = $Prompt
            size = $Size
            output_format = 'png'
            n = 1
            quality = $Quality
        } | ConvertTo-Json -Depth 5
        $content = [System.Net.Http.StringContent]::new(
            $payload,
            [Text.Encoding]::UTF8,
            'application/json'
        )
        try {
            $httpResponse = $client.PostAsync(
                "$($baseUrl.TrimEnd('/'))/v1/images/generations",
                $content
            ).GetAwaiter().GetResult()
            $responseJson = $httpResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult()
            if (-not $httpResponse.IsSuccessStatusCode) {
                throw "Image generation failed ($([int] $httpResponse.StatusCode)): $responseJson"
            }
            Save-ApiImage $responseJson $OutputPath
        }
        finally {
            $content.Dispose()
        }
    }
    else {
        $form = [System.Net.Http.MultipartFormDataContent]::new()
        try {
            $form.Add([System.Net.Http.StringContent]::new($Model), 'model')
            $form.Add([System.Net.Http.StringContent]::new($Prompt, [Text.Encoding]::UTF8), 'prompt')
            $form.Add([System.Net.Http.StringContent]::new($Size), 'size')
            $form.Add([System.Net.Http.StringContent]::new('png'), 'output_format')
            $form.Add([System.Net.Http.StringContent]::new($Quality), 'quality')
            $imageBytes = [IO.File]::ReadAllBytes((Resolve-Path -LiteralPath $ImagePath))
            $imageContent = [System.Net.Http.ByteArrayContent]::new($imageBytes)
            $imageContent.Headers.ContentType =
                [System.Net.Http.Headers.MediaTypeHeaderValue]::new('image/png')
            $form.Add($imageContent, 'image', [IO.Path]::GetFileName($ImagePath))
            if (-not [string]::IsNullOrWhiteSpace($MaskPath)) {
                $maskBytes = [IO.File]::ReadAllBytes((Resolve-Path -LiteralPath $MaskPath))
                $maskContent = [System.Net.Http.ByteArrayContent]::new($maskBytes)
                $maskContent.Headers.ContentType =
                    [System.Net.Http.Headers.MediaTypeHeaderValue]::new('image/png')
                $form.Add($maskContent, 'mask', [IO.Path]::GetFileName($MaskPath))
            }

            $httpResponse = $client.PostAsync(
                "$($baseUrl.TrimEnd('/'))/v1/images/edits",
                $form
            ).GetAwaiter().GetResult()
            $responseJson = $httpResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult()
            if (-not $httpResponse.IsSuccessStatusCode) {
                throw "Image edit failed ($([int] $httpResponse.StatusCode)): $responseJson"
            }
            Save-ApiImage $responseJson $OutputPath
        }
        finally {
            $form.Dispose()
        }
    }

    Get-Item -LiteralPath $OutputPath | Select-Object FullName, Length, LastWriteTime
}
finally {
    $client.Dispose()
    $handler.Dispose()
}
