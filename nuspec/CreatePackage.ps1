param ([string]$ProjectPath, [string]$MSBuid, [string]$AssemblyVersion)

$ProjectName = [System.IO.Path]::GetFileNameWithoutExtension($ProjectPath)

Write-Host ------ Подготовка к пакетированию $ProjectName

$MSBuid= "& ""$MSBuid\Bin\MSBuild.exe"" " 
$MSBuildParams = " /nologo /clp:ErrorsOnly /t:Rebuild /p:Configuration=""Release"
$Net40 = "Net40"
$Net45 = "Net45"
$Net461 = "Net461"

function Build([string]$ProjectPath, [string]$Config)
{
	Write-Host ------ Построение проекта: $ProjectName для $Config
	$MSBuidCmd = $MSBuid + """" + $ProjectPath + """" + $MSBuildParams + $Config + """"
	$Out = Invoke-Expression -Command $MSBuidCmd | Out-String
	if (-not $lastexitcode -eq 0) 
	{
		Write-Host $Out
		exit $lastexitcode
	}
}

function Get-Version([string]$PathFile)
{
	return [System.Reflection.Assembly]::LoadFile($PathFile).GetName().Version.ToString(3)
}

Build $ProjectPath $Net40
Build $ProjectPath $Net45
Build $ProjectPath $Net461

$NuspecFiles = [System.IO.Path]::GetDirectoryName($MyInvocation.MyCommand.Definition)
$SolutionPath = [System.IO.Path]::GetDirectoryName($NuspecFiles)
$BuildPath = [System.IO.Path]::Combine($SolutionPath, '_Build')
$DocsPath = [System.IO.Path]::Combine($SolutionPath, 'Docs')
$NugetTarget = [System.IO.Path]::Combine($BuildPath, 'nuget')
$NuGetPath = [System.IO.Path]::Combine($SolutionPath, '.nuget')

$VersionFile = Get-Version "$BuildPath\$ProjectName.dll" 
Write-Host Версия $ProjectName $VersionFile

# Создание целевой папки при ее отсутствии
if ((Test-Path "$NugetTarget") -eq $false)
{
	New-Item -Path "$NugetTarget" -ItemType Directory | Out-Null
}

# Удаление пакетов в целевой папке
Remove-Item "$NugetTarget\$ProjectName*" -Force

Write-Host Сборка пакета NuGet для проекта: $ProjectName

# Копируем .nuspec в папку построений
Copy-Item -Path "$NuspecFiles\$ProjectName.nuspec" -Destination "$BuildPath\$ProjectName.nuspec" -Force

$NugetPackCommand = "& ""$NuGetPath\Nuget.exe"" pack ""$BuildPath\$ProjectName.nuspec"" -OutputDir ""$NugetTarget"" -Version $VersionFile" 
$Out  = Invoke-Expression -Command $NugetPackCommand | Out-String
if (-not $lastexitcode -eq 0) 
{
	Write-Host $Out
	exit $lastexitcode
}

# Удаление .nuspec
Remove-Item "$BuildPath\$ProjectName.nuspec"

# Удаление собраных сборок под разные Net
if (Test-Path "$BuildPath\$Net40")
{
	Remove-Item "$BuildPath\$Net40" -Recurse -Force
}

if (Test-Path "$BuildPath\$Net45")
{
	Remove-Item "$BuildPath\$Net45" -Recurse -Force
}
if (Test-Path "$BuildPath\$Net461")
{
	Remove-Item "$BuildPath\$Net461" -Recurse -Force
}

# Копируем файл документации
Copy-Item -Path "$BuildPath\$ProjectName.xml" -Destination "$DocsPath\$ProjectName.xml" -Force

Write-Host ------ Сборка пакета NuGet для проекта $ProjectName $VersionFile успешно завершена