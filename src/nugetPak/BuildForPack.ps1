param (
[string]$Project, 
[string]$MSBuid)


$ProjectPath = [System.IO.Path]::GetDirectoryName($Project)
$ProjectName = [System.IO.Path]::GetFileNameWithoutExtension($Project)
$ProjectExt = [System.IO.Path]::GetExtension($Project)

Write-Host ------ Подготовка к пакетированию $ProjectName

$targetsNet = @(
	@{
		TargetNet="net461"; 
		MsBuldPath="& ""$MSBuid\Bin\MSBuild.exe"" "
	 }
	)

foreach ($tNet in $targetsNet)
{
	Write-Host ------- Посторение проекта для $tNet.TargetNet
	$targetProject = [System.IO.Path]::Combine($ProjectPath, $ProjectName + ".nugetPak.csproj")
	
    $MSBuildParams = ' /nologo /clp:ErrorsOnly /t:Rebuild /p:Platform=AnyCPU /p:Configuration="Release.' + $tNet.TargetNet + '" '
		
	$MSBuidCmd = $tNet.MsBuldPath + """" + $targetProject + """" + $MSBuildParams
		
	$Out = Invoke-Expression -Command $MSBuidCmd | Out-String
    if (-not $lastexitcode -eq 0) 
    {
        Write-Host $Out
        exit $lastexitcode
    }	
}