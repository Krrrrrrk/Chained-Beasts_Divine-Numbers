param([string]$GameDirectory = 'C:\Program Files (x86)\Steam\steamapps\common\Chained Beasts')
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $projectRoot 'src\ChainedBeasts.DivineNumbers\ChainedBeasts.DivineNumbers.csproj'

python (Join-Path $projectRoot 'scripts\verify-catalog.py') --game-dir $GameDirectory
if ($LASTEXITCODE -ne 0) { throw 'Catalog/build provenance checks failed' }
dotnet build $project -c Release --nologo "-p:ChainedBeastsGameDir=$GameDirectory"
if ($LASTEXITCODE -ne 0) { throw 'Plugin build failed' }
dotnet run --project (Join-Path $projectRoot 'tests\LogicChecks\LogicChecks.csproj') -c Release
if ($LASTEXITCODE -ne 0) { throw 'Offline logic checks failed' }

$stageDir = Join-Path $projectRoot ('local\package-' + [Guid]::NewGuid().ToString('N'))
$pluginDir = Join-Path $stageDir 'BepInEx\plugins\ChainedBeasts.DivineNumbers'
New-Item -ItemType Directory -Path $pluginDir -Force | Out-Null
$dll = Join-Path $projectRoot 'src\ChainedBeasts.DivineNumbers\bin\Release\net6.0\ChainedBeasts.DivineNumbers.dll'
Copy-Item -LiteralPath $dll -Destination $pluginDir
Copy-Item -LiteralPath (Join-Path $projectRoot 'INSTALL.txt') -Destination $stageDir
Copy-Item -LiteralPath (Join-Path $projectRoot 'LICENSE') -Destination $stageDir
Copy-Item -LiteralPath (Join-Path $projectRoot 'docs\ctrl-details.md') -Destination (Join-Path $stageDir 'MANUAL-CHECKS.md')
Copy-Item -LiteralPath (Join-Path $projectRoot 'docs\audit-report.md') -Destination (Join-Path $stageDir 'AUDIT.md')
$artifactDir = Join-Path $projectRoot 'artifacts'
New-Item -ItemType Directory -Path $artifactDir -Force | Out-Null
$version = ([xml](Get-Content -LiteralPath $project -Raw)).Project.PropertyGroup.Version
$zip = Join-Path $artifactDir "Divine-Numbers-$version-test-build.zip"
Compress-Archive -Path (Join-Path $stageDir '*') -DestinationPath $zip -Force
Get-FileHash -LiteralPath $dll, $zip -Algorithm SHA256
