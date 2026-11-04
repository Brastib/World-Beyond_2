# Echte Unity-Prüfung auf Windows: Einrichtung, EditMode-Tests, PlayMode-Tests, Windows-Build, Build-Smoke mit Screenshot.
# Aufruf im Repo-Ordner:  powershell -ExecutionPolicy Bypass -File Tools\run-unity-tests.ps1 [-Unity "C:\Pfad\Unity.exe"]
param([string]$Unity = "")
$ErrorActionPreference = "Stop"
$Project = (Resolve-Path "$PSScriptRoot\..").Path
$Out = Join-Path $Project "TestResults"
New-Item -ItemType Directory -Force -Path $Out | Out-Null

if ($Unity -eq "") {
  $cand = Get-ChildItem "C:\Program Files\Unity\Hub\Editor" -Directory -ErrorAction SilentlyContinue |
          Where-Object { $_.Name -like "6000*" } | Sort-Object Name -Descending | Select-Object -First 1
  if ($null -eq $cand) { throw "Kein Unity 6 (6000.x) unter C:\Program Files\Unity\Hub\Editor gefunden. Pfad mit -Unity angeben." }
  $Unity = Join-Path $cand.FullName "Editor\Unity.exe"
}
Write-Host "Unity: $Unity"

function Run-Unity([string]$name, [string[]]$uargs) {
  $log = Join-Path $Out "$name.log"
  $p = Start-Process -FilePath $Unity -ArgumentList (@("-batchmode", "-projectPath", "`"$Project`"", "-logFile", "`"$log`"") + $uargs) -Wait -PassThru -NoNewWindow
  Write-Host ("{0}: Exitcode {1} (Log: {2})" -f $name, $p.ExitCode, $log)
  return $p.ExitCode
}

$results = [ordered]@{}
$results["1_Einrichtung"] = Run-Unity "1_setup" @("-executeMethod", "WorldBeyond.EditorTools.ProjectSetup.RunFromCommandLine")
$results["2_EditMode"]   = Run-Unity "2_editmode" @("-runTests", "-testPlatform", "EditMode", "-testResults", "`"$Out\editmode.xml`"")
$results["3_PlayMode"]   = Run-Unity "3_playmode" @("-runTests", "-testPlatform", "PlayMode", "-testResults", "`"$Out\playmode.xml`"")
$results["4_Build"]      = Run-Unity "4_build" @("-executeMethod", "WorldBeyond.EditorTools.BuildSmoke.BuildWindows")

$exe = Join-Path $Project "Builds\Windows\WorldBeyond.exe"
if (Test-Path $exe) {
  $p = Start-Process -FilePath $exe -ArgumentList @("-wb-smoke", "-screen-fullscreen", "0", "-screen-width", "1920", "-screen-height", "1080", "-logFile", "`"$Out\5_smoke_player.log`"") -Wait -PassThru
  $results["5_BuildSmoke"] = $p.ExitCode
  $smokeDir = Join-Path $env:USERPROFILE "AppData\LocalLow\WorldBeyond\World Beyond Reworked\Smoke"
  if (Test-Path $smokeDir) { Copy-Item "$smokeDir\*" $Out -Force }
} else { $results["5_BuildSmoke"] = "kein Build" }

Write-Host "`n=== Ergebnis ==="
$results.GetEnumerator() | ForEach-Object { Write-Host ("{0}: {1}" -f $_.Key, $_.Value) }
foreach ($f in @("editmode.xml", "playmode.xml")) {
  $x = Join-Path $Out $f
  if (Test-Path $x) { [xml]$r = Get-Content $x; $t = $r.'test-run'; Write-Host ("{0}: {1} Tests, {2} bestanden, {3} fehlgeschlagen" -f $f, $t.total, $t.passed, $t.failed) }
}
