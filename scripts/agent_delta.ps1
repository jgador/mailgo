<#
Creates a delta summary between a base ref and a compare ref.
Outputs
- AGENTS.delta.md

Usage
  pwsh scripts/agent_delta.ps1
  pwsh scripts/agent_delta.ps1 -BaseRef origin/master -CompareRef HEAD -Fetch
  pwsh scripts/agent_delta.ps1 -BaseRef origin/master -CompareRef HEAD -IncludeWorkingTree
#>

[CmdletBinding()]
param(
  [string]$BaseRef = "origin/master",
  [string]$CompareRef = "HEAD",
  [switch]$Fetch,
  [switch]$IncludeWorkingTree,

  [string]$OutputMarkdown = "AGENTS.delta.md",
  [int]$TopFolders = 20
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$utf8NoBom = [System.Text.UTF8Encoding]::new($false)
[Console]::OutputEncoding = $utf8NoBom
$OutputEncoding = $utf8NoBom
$PSDefaultParameterValues["Out-File:Encoding"]    = "utf8"
$PSDefaultParameterValues["Set-Content:Encoding"] = "utf8"
$PSDefaultParameterValues["Add-Content:Encoding"] = "utf8"

function Invoke-Git {
  param([Parameter(Mandatory = $true)][string[]]$Args)
  $gitArgs = @(
    "-c","i18n.logOutputEncoding=utf-8",
    "-c","i18n.commitEncoding=utf-8",
    "-c","core.quotepath=false"
  ) + $Args
  $out = & git @gitArgs
  if ($LASTEXITCODE -ne 0) {
    throw ("git failed " + ($Args -join " "))
  }
  return $out
}

function Get-TopArea {
  param([Parameter(Mandatory = $true)][string]$Path)
  $p = $Path -replace "^\./", "" -replace "^\.\\", ""
  $first = ($p -split "[/\\]" | Select-Object -First 1)
  if (-not $first) { return "other" }
  switch ($first.ToLowerInvariant()) {
    "backend"   { "backend" }
    "frontend"  { "frontend" }
    "infra"     { "infra" }
    "docs"      { "docs" }
    "scripts"   { "scripts" }
    "desktop"   { "desktop" }
    "data"      { "data" }
    "binaries"  { "binaries" }
    ".github"   { "ci" }
    default     { "other" }
  }
}

function Get-TopFolder {
  param([Parameter(Mandatory = $true)][string]$Path)
  $p = $Path -replace "^\./", "" -replace "^\.\\", ""
  $seg = ($p -split "[/\\]" | Select-Object -First 1)
  if ([string]::IsNullOrWhiteSpace($seg)) { return "." }
  return $seg
}

function Test-EntryPoint {
  param([Parameter(Mandatory = $true)][string]$Path)
  $p = $Path -replace "^\./", "" -replace "^\.\\", ""
  $lower = $p.ToLowerInvariant()

  if ($lower -eq "agents.md") { return $true }
  if ($lower -like "readme*") { return $true }

  if ($lower -like "*.sln") { return $true }
  if ($lower -like "*.csproj") { return $true }
  if ($lower -like "*.fsproj") { return $true }
  if ($lower -like "*.vbproj") { return $true }

  if ($lower -eq "global.json") { return $true }
  if ($lower -eq "nuget.config") { return $true }
  if ($lower -eq "directory.build.props") { return $true }
  if ($lower -eq "directory.build.targets") { return $true }

  if ($lower -eq "package.json") { return $true }
  if ($lower -eq "package-lock.json") { return $true }
  if ($lower -eq "pnpm-lock.yaml") { return $true }
  if ($lower -eq "yarn.lock") { return $true }

  if ($lower -like "dockerfile*") { return $true }
  if ($lower -like "*dockerfile") { return $true }
  if ($lower -eq "docker-compose.yml") { return $true }
  if ($lower -like "docker-compose.*.yml") { return $true }
  if ($lower -eq "compose.yml") { return $true }
  if ($lower -eq "compose.yaml") { return $true }

  if ($lower -like ".github/workflows/*") { return $true }
  if ($lower -like "scripts/*") { return $true }
  if ($lower -like "infra/*") { return $true }
  if ($lower -like "helm/*") { return $true }
  if ($lower -like "k8s/*") { return $true }

  return $false
}

function Parse-NameStatusLines {
  param([string[]]$Lines)

  if (-not $Lines -or $Lines.Count -eq 0) { return @() }

  $items = New-Object System.Collections.Generic.List[object]

  foreach ($line in $Lines) {
    if ([string]::IsNullOrWhiteSpace($line)) { continue }

    $parts = $line -split "`t"
    if ($parts.Count -lt 2) { continue }

    $code = $parts[0]
    $status = $code.Substring(0,1)

    if ($status -eq "R" -or $status -eq "C") {
      if ($parts.Count -ge 3) {
        $oldPath = $parts[1]
        $newPath = $parts[2]
        $items.Add([pscustomobject]@{
          status = $status
          score = $code
          path = $newPath
          oldPath = $oldPath
          area = Get-TopArea -Path $newPath
          topFolder = Get-TopFolder -Path $newPath
          isEntryPoint = (Test-EntryPoint -Path $newPath) -or (Test-EntryPoint -Path $oldPath)
        })
      }
      continue
    }

    $path = $parts[1]
    $items.Add([pscustomobject]@{
      status = $status
      score = $code
      path = $path
      oldPath = $null
      area = Get-TopArea -Path $path
      topFolder = Get-TopFolder -Path $path
      isEntryPoint = (Test-EntryPoint -Path $path)
    })
  }

  return $items
}

if ($Fetch) {
  Invoke-Git -Args @("fetch","--all","--prune") | Out-Null
}

$mergeBase = (Invoke-Git -Args @("merge-base",$BaseRef,$CompareRef) | Select-Object -First 1).Trim()
if ([string]::IsNullOrWhiteSpace($mergeBase)) {
  throw "merge base not found"
}

$ahead = [int]((Invoke-Git -Args @("rev-list","--count","$mergeBase..$CompareRef") | Select-Object -First 1).Trim())
$behind = [int]((Invoke-Git -Args @("rev-list","--count","$mergeBase..$BaseRef") | Select-Object -First 1).Trim())

$diffLines = Invoke-Git -Args @(
  "diff",
  "--name-status",
  "--find-renames",
  "--find-copies",
  "$mergeBase",
  "$CompareRef"
)
if ($null -eq $diffLines) { $diffLines = @() }

$changed = Parse-NameStatusLines -Lines $diffLines
$changed = @($changed)

$workingTree = @()
if ($IncludeWorkingTree) {
  $statusLines = Invoke-Git -Args @("status","--porcelain")
  if ($null -eq $statusLines) { $statusLines = @() }
  foreach ($s in $statusLines) {
    if ([string]::IsNullOrWhiteSpace($s)) { continue }
    $st = $s.Substring(0,2).Trim()
    $p = $s.Substring(3).Trim()
    $status = "M"
    if ($st -eq "A" -or $st -eq "??") { $status = "A" }
    if ($st -eq "D") { $status = "D" }
    $workingTree += [pscustomobject]@{
      status = $status
      score = "WT"
      path = $p
      oldPath = $null
      area = Get-TopArea -Path $p
      topFolder = Get-TopFolder -Path $p
      isEntryPoint = (Test-EntryPoint -Path $p)
    }
  }
}

$workingTree = @($workingTree)
$allChanges = @($changed + $workingTree)

$stats = [ordered]@{
  added = 0
  modified = 0
  deleted = 0
  renamed = 0
  copied = 0
  other = 0
}
foreach ($c in $allChanges) {
  switch ($c.status) {
    "A" { $stats.added++ }
    "M" { $stats.modified++ }
    "D" { $stats.deleted++ }
    "R" { $stats.renamed++ }
    "C" { $stats.copied++ }
    default { $stats.other++ }
  }
}

$entryPoints = $allChanges | Where-Object { $_.isEntryPoint } | Sort-Object path -Unique
$entryPoints = @($entryPoints)

$topFolderStats = $allChanges |
  Group-Object topFolder |
  Sort-Object Count -Descending |
  ForEach-Object {
    [pscustomobject]@{
      topFolder = $_.Name
      count = $_.Count
    }
  }
$topFolderStats = @($topFolderStats)

$areas = $allChanges |
  Group-Object area |
  Sort-Object Count -Descending |
  ForEach-Object {
    [pscustomobject]@{
      area = $_.Name
      count = $_.Count
    }
  }
$areas = @($areas)

$dirsTouched = $allChanges |
  ForEach-Object {
    $p = $_.path -replace "^\./", "" -replace "^\.\\", ""
    $d = Split-Path -Path $p -Parent
    if ([string]::IsNullOrWhiteSpace($d)) { "." } else { $d }
  } |
  Sort-Object -Unique
$dirsTouched = @($dirsTouched)

$driftScore =
  ($stats.modified * 1) +
  ($stats.added * 2) +
  ($stats.deleted * 2) +
  ($stats.renamed * 3) +
  ($entryPoints.Count * 5) +
  ([math]::Min(50, $ahead)) +
  ([math]::Min(50, $behind)) +
  ([math]::Min(20, $topFolderStats.Count))

$driftLevel = "low"
if ($driftScore -ge 80) { $driftLevel = "high" }
elseif ($driftScore -ge 30) { $driftLevel = "medium" }

$focusFolders = $topFolderStats | Select-Object -First $TopFolders
# Focus paths are emitted inside the markdown (no separate file).
$focusPaths = New-Object System.Collections.Generic.List[string]
foreach ($f in $focusFolders) {
  if ($f.topFolder -eq ".") { continue }
  $focusPaths.Add(($f.topFolder + "/"))
}
if ($focusPaths.Count -eq 0) {
  $focusPaths.Add("./")
}

$sb = [System.Text.StringBuilder]::new()
$null = $sb.AppendLine("# AGENTS delta")
$null = $sb.AppendLine("")
$null = $sb.AppendLine("- base ref " + $BaseRef)
$null = $sb.AppendLine("- compare ref " + $CompareRef)
$null = $sb.AppendLine("- merge base " + $mergeBase)
$null = $sb.AppendLine("- commits ahead " + $ahead)
$null = $sb.AppendLine("- commits behind " + $behind)
$null = $sb.AppendLine("- drift level " + $driftLevel)
$null = $sb.AppendLine("- drift score " + $driftScore)
$null = $sb.AppendLine("")

$null = $sb.AppendLine("## status counts")
$null = $sb.AppendLine("")
$null = $sb.AppendLine("| status | count |")
$null = $sb.AppendLine("| - | - |")
$null = $sb.AppendLine("| added | " + $stats.added + " |")
$null = $sb.AppendLine("| modified | " + $stats.modified + " |")
$null = $sb.AppendLine("| deleted | " + $stats.deleted + " |")
$null = $sb.AppendLine("| renamed | " + $stats.renamed + " |")
$null = $sb.AppendLine("| copied | " + $stats.copied + " |")
$null = $sb.AppendLine("| other | " + $stats.other + " |")
$null = $sb.AppendLine("")

$null = $sb.AppendLine("## top folders")
$null = $sb.AppendLine("")
$null = $sb.AppendLine("| folder | count |")
$null = $sb.AppendLine("| - | - |")
foreach ($f in ($topFolderStats | Select-Object -First 20)) {
  $null = $sb.AppendLine("| " + $f.topFolder + " | " + $f.count + " |")
}
$null = $sb.AppendLine("")

$null = $sb.AppendLine("## areas")
$null = $sb.AppendLine("")
$null = $sb.AppendLine("| area | count |")
$null = $sb.AppendLine("| - | - |")
foreach ($a in $areas) {
  $null = $sb.AppendLine("| " + $a.area + " | " + $a.count + " |")
}
$null = $sb.AppendLine("")

$null = $sb.AppendLine("## focus paths")
$null = $sb.AppendLine("")
foreach ($p in $focusPaths) {
  $null = $sb.AppendLine("- " + $p)
}
$null = $sb.AppendLine("")

$null = $sb.AppendLine("## entry points changed")
$null = $sb.AppendLine("")
if ($entryPoints.Count -eq 0) {
  $null = $sb.AppendLine("- none")
} else {
  foreach ($e in $entryPoints) {
    if ($null -ne $e.oldPath -and -not [string]::IsNullOrWhiteSpace($e.oldPath)) {
      $null = $sb.AppendLine("- " + $e.status + " " + $e.oldPath + " - " + $e.path)
    } else {
      $null = $sb.AppendLine("- " + $e.status + " " + $e.path)
    }
  }
}
$null = $sb.AppendLine("")

$null = $sb.AppendLine("## changed files")
$null = $sb.AppendLine("")
# Sort entry points first (desc), then topFolder/path ascending for readability.
$sortRules = @(
  @{ Expression = { $_.isEntryPoint }; Descending = $true },
  @{ Expression = { $_.topFolder } ; Descending = $false },
  @{ Expression = { $_.path } ; Descending = $false }
)
foreach ($c in ($allChanges | Sort-Object -Property $sortRules)) {
  if ($null -ne $c.oldPath -and -not [string]::IsNullOrWhiteSpace($c.oldPath)) {
    $null = $sb.AppendLine("- " + $c.status + " " + $c.oldPath + " - " + $c.path)
  } else {
    $null = $sb.AppendLine("- " + $c.status + " " + $c.path)
  }
}

$sb.ToString() | Out-File -FilePath $OutputMarkdown -Encoding UTF8

Write-Host ("Wrote " + $OutputMarkdown)
