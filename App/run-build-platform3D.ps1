#
# PSake build script for ThCADPlugin Apps
#
Task Release.Build {
    $script:buildType = "Release"
}

Task Debug.Build {
    $script:buildType = "Debug"
}
function Get-LatestMsbuildLocation
{
  Param 
  (
    [bool] $allowPreviewVersions = $false
  )
    if ($allowPreviewVersions) {
      $latestVsInstallationInfo = Get-VSSetupInstance -All -Prerelease | Sort-Object -Property InstallationVersion -Descending | Select-Object -First 1
    } else {
      $latestVsInstallationInfo = Get-VSSetupInstance -All | Sort-Object -Property InstallationVersion -Descending | Select-Object -First 1
    }
    Write-Host "Latest version installed is $($latestVsInstallationInfo.InstallationVersion)"
    if ($latestVsInstallationInfo.InstallationVersion -like "15.*") {
      $msbuildLocation = "$($latestVsInstallationInfo.InstallationPath)\MSBuild\15.0\Bin\msbuild.exe"
    
      Write-Host "Located msbuild for Visual Studio 2017 in $msbuildLocation"
    } else {
      $msbuildLocation = "$($latestVsInstallationInfo.InstallationPath)\MSBuild\Current\Bin\msbuild.exe"
      Write-Host "Located msbuild in $msbuildLocation"
    }

    return $msbuildLocation
}

Task Requires.MSBuild {
    $script:msbuildExe = Get-LatestMsbuildLocation

    if ($msbuildExe -eq $null)
    {
            throw "Failed to find MSBuild"
    }

    Write-Host "Found MSBuild here: $msbuildExe"
}

# $buildType build for AutoCAD R20
Task Compile.Assembly.R20.Common -Depends Requires.MSBuild {
    exec {
        & $msbuildExe /verbosity:minimal /property:OutDir="..\build\bin\${buildType}-NET45\",IntermediateOutputPath="..\build\obj\${buildType}-NET45\" ".\ThPlatform3D.sln" /p:Configuration="${buildType}-NET45" /t:restore
        & $msbuildExe /verbosity:minimal /property:OutDir="..\build\bin\${buildType}-NET45\",IntermediateOutputPath="..\build\obj\${buildType}-NET45\" ".\ThPlatform3D.sln" /p:Configuration="${buildType}-NET45" /t:rebuild
    }
}

# $buildType build for AutoCAD R22
Task Compile.Assembly.R22.Common -Depends Requires.MSBuild {
    exec {
        & $msbuildExe /verbosity:minimal /property:OutDir="..\build\bin\${buildType}-NET46\",IntermediateOutputPath="..\build\obj\${buildType}-NET46\" ".\ThPlatform3D.sln" /p:Configuration="${buildType}-NET46" /t:restore
        & $msbuildExe /verbosity:minimal /property:OutDir="..\build\bin\${buildType}-NET46\",IntermediateOutputPath="..\build\obj\${buildType}-NET46\" ".\ThPlatform3D.sln" /p:Configuration="${buildType}-NET46" /t:rebuild
    }
}

Task Requires.BuildType {
    if ($buildType -eq $null) {
        throw "No build type specified"
    }

    Write-Host "$buildType build confirmed"
}

Task Compile.Engine -Depends Requires.BuildType, Compile.Assembly.R20.Common, Compile.Assembly.R22.Common
{

}

Task Compile.Installer -Depends Compile.Engine {
    if ($buildType -eq $null) {
        throw "No build type specified"
    }
    exec {
        & $msbuildExe /verbosity:minimal ".\ThPlatform3DInstaller\ThPlatform3DInstaller.wixproj" /p:Configuration=$buildType /t:rebuild
    }
}

