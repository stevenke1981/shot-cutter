param(
    [Parameter(Mandatory = $true)]
    [string]$TargetDir,
    [string]$FFmpegPath,
    [string]$FFprobePath
)

$ErrorActionPreference = "Stop"

function Resolve-ToolPath {
    param(
        [string]$ConfiguredPath,
        [string]$CommandName
    )

    if ($ConfiguredPath -and (Test-Path -LiteralPath $ConfiguredPath)) {
        return (Resolve-Path -LiteralPath $ConfiguredPath).Path
    }

    $cmd = Get-Command $CommandName -ErrorAction SilentlyContinue
    if ($cmd) {
        $item = Get-Item -LiteralPath $cmd.Source -Force
        if ($item.LinkType -and $item.Target) {
            return $item.Target
        }

        return $cmd.Source
    }

    return $null
}

$ffmpeg = Resolve-ToolPath -ConfiguredPath $FFmpegPath -CommandName "ffmpeg"
$ffprobe = Resolve-ToolPath -ConfiguredPath $FFprobePath -CommandName "ffprobe"

if (-not $ffmpeg -or -not $ffprobe) {
    Write-Warning "ffmpeg/ffprobe not found. Published app will fall back to PATH."
    exit 0
}

$toolsDir = Join-Path $TargetDir "tools\ffmpeg"
New-Item -ItemType Directory -Force -Path $toolsDir | Out-Null

Copy-Item -LiteralPath $ffmpeg -Destination (Join-Path $toolsDir "ffmpeg.exe") -Force
Copy-Item -LiteralPath $ffprobe -Destination (Join-Path $toolsDir "ffprobe.exe") -Force

Write-Host "Copied FFmpeg tools to $toolsDir"
