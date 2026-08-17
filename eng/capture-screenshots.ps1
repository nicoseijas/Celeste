#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Renders the gallery to the PNG files the documentation links to.

.DESCRIPTION
    Screenshots pasted into a README age silently: the control changes, the picture does not, and
    nothing fails. These are produced from the sample itself, so regenerating them is one command
    and a stale picture shows up as a diff.

    Every tab is captured in both themes, plus a hero shot cut down the middle from the light and
    dark renders of the same tab.

    The files are not byte-for-byte reproducible across machines — font rasterization and the
    installed display face differ — so this is not a visual regression test. Look at the diff.

.PARAMETER OutputDirectory
    Where the PNGs go. Defaults to docs/images.

.PARAMETER Configuration
    Build configuration for the gallery. Defaults to Release, which is what the pictures should show.
#>

[CmdletBinding()]
param(
    [string]$OutputDirectory,
    [string]$Configuration = 'Release'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot

if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path $repositoryRoot 'docs' 'images'
}

New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$OutputDirectory = (Resolve-Path $OutputDirectory).Path

$gallery = Join-Path $repositoryRoot 'samples' 'Celeste.Gallery'

Write-Host "Capturing the gallery into $OutputDirectory"

& dotnet run --project $gallery --configuration $Configuration -- --capture $OutputDirectory

if ($LASTEXITCODE -ne 0) {
    throw "The gallery exited with code $LASTEXITCODE. Nothing was captured."
}

$pictures = @(Get-ChildItem -Path $OutputDirectory -Filter '*.png' -File | Sort-Object Name)

if ($pictures.Count -eq 0) {
    throw "The gallery reported success but wrote no PNG into '$OutputDirectory'."
}

foreach ($picture in $pictures) {
    Write-Host ("  {0,-28} {1,7:N0} KB" -f $picture.Name, ($picture.Length / 1KB))
}

Write-Host "`n$($pictures.Count) pictures. Open them before committing: this script cannot tell you they look right."
