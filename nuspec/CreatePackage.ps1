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

$NuspecFiles = [System.IO.Path]::GetDirectoryName($MyInvocation.MyCommand.Definition)
$SolutionPath = [System.IO.Path]::GetDirectoryName($NuspecFiles)
$BuildPath = [System.IO.Path]::Combine($SolutionPath, '_Build')
$NugetTarget = [System.IO.Path]::Combine($BuildPath, 'nuget')
$BuildPath = [System.IO.Path]::Combine([System.IO.Path]::Combine($SolutionPath, '_Build'), $ProjectName)
$NuGetPath = [System.IO.Path]::Combine($SolutionPath, '.nuget')

Build $ProjectPath $Net40
Build $ProjectPath $Net45
Build $ProjectPath $Net461

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

Write-Host ------ Сборка пакета NuGet для проекта $ProjectName $VersionFile успешно завершена