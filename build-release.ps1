<#
.SYNOPSIS
    Сборка портативного релизного артефакта UkiChat (self-contained win-x64 + zip).

.DESCRIPTION
    Порядок шагов:
      1. Читает версию из UkiChat.csproj (AssemblyVersion), чтобы имя архива не разъезжалось с бинарником.
      2. Собирает фронтенд напрямую (pnpm generate), НЕ полагаясь на MSBuild-таргет BuildNuxtFrontend:
         тот пропускает сборку, если на :3000 слушает dev-сервер, и в релиз уехал бы устаревший wwwroot.
         Блок $production в nuxt.config.ts собирает в отдельный .nuxt-prod, поэтому запущенный
         dev-сервер при этом не ломается.
      3. dotnet publish с -p:SkipWebBuild=true — фронт уже собран на шаге 2, второй раз не нужен.
      4. Проверяет комплектность выхода (exe, wwwroot, локализации) — публиковать битый артефакт хуже,
         чем не собрать вовсе.
      5. Пакует в zip.

.PARAMETER OutputDir
    Куда складывать. По умолчанию publish/ в корне репозитория (он в .gitignore).

.PARAMETER SkipFrontend
    Не пересобирать фронт — использовать текущий wwwroot. Для быстрых повторных прогонов.

.PARAMETER NoPdb
    Не класть .pdb в архив. По умолчанию pdb включаются: на тестовых сборках они дают
    номера строк в стектрейсах от тестировщиков.

.EXAMPLE
    .\build-release.ps1
    .\build-release.ps1 -SkipFrontend -NoPdb
#>
[CmdletBinding()]
param(
    [string]$OutputDir,
    [switch]$SkipFrontend,
    [switch]$NoPdb
)

$ErrorActionPreference = 'Stop'

$RepoRoot = $PSScriptRoot
$WebDir = Join-Path $RepoRoot 'ukichat-web'
$ProjectPath = Join-Path $RepoRoot 'UkiChat\UkiChat\UkiChat.csproj'
$Runtime = 'win-x64'

if (-not $OutputDir) { $OutputDir = Join-Path $RepoRoot 'publish' }

function Invoke-Step {
    param([string]$Name, [scriptblock]$Action)
    Write-Host ""
    Write-Host "==> $Name" -ForegroundColor Cyan
    & $Action
}

function Assert-LastExitCode {
    param([string]$What)
    if ($LASTEXITCODE -ne 0) { throw "$What завершился с кодом $LASTEXITCODE" }
}

# --- Версия ---------------------------------------------------------------
if (-not (Test-Path $ProjectPath)) { throw "Не найден проект: $ProjectPath" }
$csproj = Get-Content $ProjectPath -Raw
if ($csproj -notmatch '<AssemblyVersion>([\d.]+)</AssemblyVersion>') {
    throw "В $ProjectPath не найден <AssemblyVersion>"
}
# 0.6.4.0 -> 0.6.4: четвёртый компонент в имени артефакта только шумит
$Version = ($Matches[1] -split '\.')[0..2] -join '.'

$StageDir = Join-Path $OutputDir "UkiChat-$Version-$Runtime"
$ZipPath = Join-Path $OutputDir "UkiChat-$Version-$Runtime.zip"

Write-Host "UkiChat release build" -ForegroundColor Green
Write-Host "  версия : $Version"
Write-Host "  runtime: $Runtime (self-contained)"
Write-Host "  выход  : $OutputDir"

# --- Чистим прошлый прогон ------------------------------------------------
Invoke-Step "Очистка предыдущего артефакта" {
    if (Test-Path $StageDir) { Remove-Item $StageDir -Recurse -Force }
    if (Test-Path $ZipPath) { Remove-Item $ZipPath -Force }
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
}

# --- Фронтенд -------------------------------------------------------------
if ($SkipFrontend) {
    Write-Host ""
    Write-Host "==> Фронтенд пропущен (-SkipFrontend), используется текущий wwwroot" -ForegroundColor Yellow
} else {
    Invoke-Step "Сборка фронтенда (pnpm generate)" {
        Push-Location $WebDir
        try {
            if (-not (Test-Path (Join-Path $WebDir 'node_modules'))) {
                pnpm install
                Assert-LastExitCode 'pnpm install'
            }
            pnpm generate
            Assert-LastExitCode 'pnpm generate'
        } finally {
            Pop-Location
        }
    }
}

# --- Backend --------------------------------------------------------------
Invoke-Step "dotnet publish (self-contained $Runtime)" {
    dotnet publish $ProjectPath `
        -c Release `
        -r $Runtime `
        --self-contained true `
        -p:SkipWebBuild=true `
        -o $StageDir `
        --nologo
    Assert-LastExitCode 'dotnet publish'
}

# --- Проверка комплектности ----------------------------------------------
Invoke-Step "Проверка артефакта" {
    $required = @(
        'UkiChat.exe',
        'wwwroot\index.html',
        'Localization\ru.json',
        'Localization\en.json'
    )
    $missing = @()
    foreach ($rel in $required) {
        if (-not (Test-Path (Join-Path $StageDir $rel))) { $missing += $rel }
    }
    if ($missing.Count -gt 0) {
        throw "В сборке не хватает файлов: $($missing -join ', ')"
    }

    # Self-contained обязан принести собственный рантайм — иначе на чужой машине
    # молча не стартует. Проверяем по хостовой библиотеке WPF.
    if (-not (Test-Path (Join-Path $StageDir 'PresentationFramework.dll'))) {
        throw "Не найден PresentationFramework.dll — сборка не self-contained"
    }

    if ($NoPdb) {
        Get-ChildItem $StageDir -Filter '*.pdb' -Recurse | Remove-Item -Force
    }

    $files = Get-ChildItem $StageDir -Recurse -File
    Write-Host "  файлов: $($files.Count)"
    Write-Host "  размер: $([math]::Round(($files | Measure-Object Length -Sum).Sum / 1MB, 1)) МБ"
}

# --- Упаковка -------------------------------------------------------------
Invoke-Step "Упаковка в zip" {
    # ZipFile вместо Compress-Archive: последний на PS 5.1 крайне медленный на сотнях МБ
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [System.IO.Compression.ZipFile]::CreateFromDirectory(
        $StageDir, $ZipPath,
        [System.IO.Compression.CompressionLevel]::Optimal,
        $true)  # includeBaseDirectory: внутри архива будет папка UkiChat-<версия>-win-x64
}

$zipMb = [math]::Round((Get-Item $ZipPath).Length / 1MB, 1)
Write-Host ""
Write-Host "Готово." -ForegroundColor Green
Write-Host "  $ZipPath"
Write-Host "  $zipMb МБ"
