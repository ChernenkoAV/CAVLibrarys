param ([string]$ProjectName)

Write-Host "Сборка пакета NuGet для проекта:" $ProjectName

$NuspecFiles = [System.IO.Path]::GetDirectoryName($MyInvocation.MyCommand.Definition)
$ProgectPath = [System.IO.Path]::GetDirectoryName($NuspecFiles)
$NuGetPath = [System.IO.Path]::Combine($ProgectPath, '.nuget')
$BuildPath = [System.IO.Path]::Combine($ProgectPath, '_Build')
$DocsPath = [System.IO.Path]::Combine($ProgectPath, 'Docs')

# Копируем .nuspec в папку со сборкой
Copy-Item -Path "$NuspecFiles\$ProjectName.nuspec" -Destination "$BuildPath\$ProjectName.nuspec" -Force

function Get-Version([string]$PathFile)
{
	return [System.Reflection.Assembly]::LoadFile($PathFile).GetName().Version.ToString(3)
}

$VersionFile = Get-Version "$BuildPath\$ProjectName.dll"

$NugetPackCommand = "& ""$NuGetPath\Nuget.exe"" pack ""$BuildPath\$ProjectName.nuspec"" -Version $VersionFile" 

Invoke-Expression -Command $NugetPackCommand

# Удаление .nuspec
Remove-Item "$BuildPath\$ProjectName.nuspec"

# Копируем файл документации
Copy-Item -Path "$BuildPath\$ProjectName.xml" -Destination "$DocsPath\$ProjectName.xml" -Force
