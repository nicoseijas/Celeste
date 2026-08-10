#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Restores the packed Celeste.Wpf into a project that has never seen this repository, and builds it.

.DESCRIPTION
    Packing proves a .nupkg came out of the build. It does not prove anyone can use it: a missing lib
    folder, an assembly whose XmlnsDefinition did not ship, or a dependency that only resolves through
    a project reference all pack cleanly and fail on the far side.

    The consumer is created outside the repository on purpose, so it inherits none of its
    Directory.Build.props, and its XAML resolves the library's own xmlns — which is the part a plain
    `dotnet add package` would not exercise.

.PARAMETER PackageSource
    Folder holding the .nupkg to test. Defaults to the repository's artifacts folder.

.PARAMETER TargetFrameworks
    Which target frameworks to consume the package from. Defaults to both the package offers.

.PARAMETER WorkingDirectory
    Where to build the throwaway consumers. Defaults to a temporary folder.
#>

[CmdletBinding()]
param(
    [string]$PackageSource,
    [string[]]$TargetFrameworks = @('net8.0-windows', 'net10.0-windows'),
    [string]$WorkingDirectory
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot

if (-not $PackageSource) {
    $PackageSource = Join-Path $repositoryRoot 'artifacts'
}

if (-not $WorkingDirectory) {
    $WorkingDirectory = Join-Path ([System.IO.Path]::GetTempPath()) 'celeste-package-validation'
}

$PackageSource = (Resolve-Path $PackageSource).Path

$packages = @(Get-ChildItem -Path $PackageSource -Filter '*.nupkg' -File |
    Where-Object { $_.Name -notlike '*.symbols.nupkg' })

if ($packages.Count -eq 0) {
    throw "No .nupkg in '$PackageSource'. Run dotnet pack first."
}

Write-Host "Validating $($packages[0].Name) from $PackageSource"

function Invoke-Dotnet {
    param([Parameter(Mandatory)][string[]]$Arguments)

    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet $($Arguments -join ' ') failed with exit code $LASTEXITCODE."
    }
}

if (Test-Path $WorkingDirectory) {
    Remove-Item -Recurse -Force $WorkingDirectory
}

New-Item -ItemType Directory -Path $WorkingDirectory | Out-Null

# Only these two feeds. <clear /> so a machine-wide source cannot satisfy the reference with some
# other Celeste.Wpf and hide the fact that the local one is broken.
$nugetConfig = @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
    <add key="celeste-local" value="$PackageSource" />
  </packageSources>
</configuration>
"@

Set-Content -Path (Join-Path $WorkingDirectory 'nuget.config') -Value $nugetConfig -Encoding utf8

# Uses the library from XAML rather than just referencing it: the markup compiler has to resolve the
# xmlns out of the packaged assembly, and the styles have to be findable by key.
$appXaml = @"
<Application x:Class="Consumer.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:celeste="https://celeste-ui.dev/wpf"
             StartupUri="MainWindow.xaml">
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <celeste:ThemesDictionary Theme="System" />
                <celeste:ControlsDictionary />
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Application.Resources>
</Application>
"@

$mainWindowXaml = @"
<Window x:Class="Consumer.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:celeste="https://celeste-ui.dev/wpf"
        Title="Consumer"
        Width="400"
        Height="220"
        Style="{StaticResource Celeste.Window}">
    <celeste:Card Margin="16" Header="It works">
        <StackPanel>
            <celeste:ToggleSwitch Content="Public" IsChecked="True" />
            <celeste:Badge Margin="0,12,0,0" Content="Passing" Variant="Success" />
            <celeste:Avatar Margin="0,12,0,0" Initials="NS" />
        </StackPanel>
    </celeste:Card>
</Window>
"@

foreach ($framework in $TargetFrameworks) {
    Write-Host "`n=== $framework ==="

    $projectDirectory = Join-Path $WorkingDirectory $framework
    Invoke-Dotnet @('new', 'wpf', '--output', $projectDirectory, '--name', 'Consumer')

    $projectFile = Join-Path $projectDirectory 'Consumer.csproj'

    # The template targets whatever the SDK defaults to; the point here is to consume every framework
    # the package claims to support.
    $project = Get-Content -Path $projectFile -Raw
    $project = [System.Text.RegularExpressions.Regex]::Replace(
        $project,
        '<TargetFramework>[^<]+</TargetFramework>',
        "<TargetFramework>$framework</TargetFramework>")
    Set-Content -Path $projectFile -Value $project -Encoding utf8

    Set-Content -Path (Join-Path $projectDirectory 'App.xaml') -Value $appXaml -Encoding utf8
    Set-Content -Path (Join-Path $projectDirectory 'MainWindow.xaml') -Value $mainWindowXaml -Encoding utf8

    Invoke-Dotnet @('add', $projectFile, 'package', 'Celeste.Wpf', '--prerelease')
    Invoke-Dotnet @('build', $projectFile, '--configuration', 'Release', '--nologo')
}

Write-Host "`nThe package restores and builds on: $($TargetFrameworks -join ', ')"
