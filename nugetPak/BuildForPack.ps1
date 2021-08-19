param (
[string]$Project, 
[string]$MSBuid, 
[string]$ProductVersion)


$ProjectPath = [System.IO.Path]::GetDirectoryName($Project)
$ProjectName = [System.IO.Path]::GetFileNameWithoutExtension($Project)
$ProjectExt = [System.IO.Path]::GetExtension($Project)

$VerAssembly = $ProductVersion.Split(".")[0]
	
$VerFile = $VerAssembly + [System.DateTime]::Now.Tostring(".yyyy.MM.dd")
$VerAssembly = $VerAssembly + ".0.0.0"

Write-Host ------ Подготовка к пакетированию $ProjectName Версия пакета $ProductVersion Версия сборки $VerAssembly Версия файла $VerFile

$targetsNet = @(
	@{
		TargetNet="net461"; 
		MsBuldPath="& ""$MSBuid\Bin\MSBuild.exe"" "
	 },
	 @{
		TargetNet="net48"; 
		MsBuldPath="& ""$MSBuid\Bin\MSBuild.exe"" "
	 }
	)

foreach ($tNet in $targetsNet)
{
	Write-Host ------- Посторение проекта для $tNet.TargetNet
	$targetProject = [System.IO.Path]::Combine($ProjectPath, $ProjectName + ".nugetPak.csproj")
	
    $MSBuildParams = ' /nologo /clp:ErrorsOnly /t:Rebuild /p:Platform=AnyCPU /p:Configuration="Release.' + $tNet.TargetNet + '" '	
	$MSBuildParams += '/p:ProductVersion="' + $ProductVersion + """ /p:VerAssembly=$VerAssembly /p:VerFile=$VerFile"
		
	$MSBuidCmd = $tNet.MsBuldPath + """" + $targetProject + """" + $MSBuildParams
		
	$Out = Invoke-Expression -Command $MSBuidCmd | Out-String
    if (-not $lastexitcode -eq 0) 
    {
        Write-Host $Out
        exit $lastexitcode
    }	
}