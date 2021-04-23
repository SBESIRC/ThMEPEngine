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

# $buildType build for AutoCAD R18
Task Compile.Assembly.R18.Common -Depends Requires.MSBuild {
    exec {
        & $msbuildExe /verbosity:minimal /property:OutDir="..\build\bin\$buildType\",IntermediateOutputPath="..\build\obj\$buildType\" ".\ThMEPEngine.sln" /p:Configuration=$buildType /t:restore
        & $msbuildExe /verbosity:minimal /property:OutDir="..\build\bin\$buildType\",IntermediateOutputPath="..\build\obj\$buildType\" ".\ThMEPEngine.sln" /p:Configuration=$buildType /t:rebuild
    }
}

Task Compile.Assembly.R18.FanSelection -Depends Requires.MSBuild {
    exec {
        & $msbuildExe /verbosity:minimal /property:OutDir="..\build\bin\$buildType\",IntermediateOutputPath="..\build\obj\$buildType\" ".\ThMEPEquipmentSelection.sln" /p:Configuration=$buildType /t:restore
        & $msbuildExe /verbosity:minimal /property:OutDir="..\build\bin\$buildType\",IntermediateOutputPath="..\build\obj\$buildType\" ".\ThMEPEquipmentSelection.sln" /p:Configuration=$buildType /t:rebuild
    }
}

Task Compile.Resource.R18 -Depends Requires.MSBuild {
    exec {
            & $msbuildExe /verbosity:minimal /property:OutDir="..\build\bin\$buildType\Dark\",IntermediateOutputPath="..\build\obj\$buildType\Dark\" ".\ThCuiRes\ThCuiRes.vcxproj" /t:rebuild
            & $msbuildExe /verbosity:minimal /property:OutDir="..\build\bin\$buildType\Light\",IntermediateOutputPath="..\build\obj\$buildType\Light\" ".\ThCuiRes\ThCuiRes_light.vcxproj" /t:rebuild
    }
}

# $buildType build for AutoCAD R19
Task Compile.Assembly.R19.Common -Depends Requires.MSBuild {
    exec {
        & $msbuildExe /verbosity:minimal /property:OutDir="..\build\bin\${buildType}-NET40\",IntermediateOutputPath="..\build\obj\${buildType}-NET40\" ".\ThMEPEngine.sln" /p:Configuration="${buildType}-NET40" /t:restore
        & $msbuildExe /verbosity:minimal /property:OutDir="..\build\bin\${buildType}-NET40\",IntermediateOutputPath="..\build\obj\${buildType}-NET40\" ".\ThMEPEngine.sln" /p:Configuration="${buildType}-NET40" /t:rebuild
    }
}

Task Compile.Assembly.R19.FanSelection -Depends Requires.MSBuild {
    exec {
        & $msbuildExe /verbosity:minimal /property:OutDir="..\build\bin\${buildType}-NET40\",IntermediateOutputPath="..\build\obj\${buildType}-NET40\" ".\ThMEPEquipmentSelection.sln" /p:Configuration="${buildType}-NET40" /t:restore
        & $msbuildExe /verbosity:minimal /property:OutDir="..\build\bin\${buildType}-NET40\",IntermediateOutputPath="..\build\obj\${buildType}-NET40\" ".\ThMEPEquipmentSelection.sln" /p:Configuration="${buildType}-NET40" /t:rebuild
    }
}

Task Compile.Resource.R19 -Depends Requires.MSBuild {
    exec {
        & $msbuildExe /verbosity:minimal /property:OutDir="..\build\bin\${buildType}-NET40\Dark\",IntermediateOutputPath="..\build\obj\${buildType}-NET40\Dark\" ".\ThCuiRes\ThCuiRes.vcxproj" /t:rebuild
        & $msbuildExe /verbosity:minimal /property:OutDir="..\build\bin\${buildType}-NET40\Light\",IntermediateOutputPath="..\build\obj\${buildType}-NET40\Light\" ".\ThCuiRes\ThCuiRes_light.vcxproj" /t:rebuild
    }
}

# $buildType build for AutoCAD R20
Task Compile.Assembly.R20.Common -Depends Requires.MSBuild {
    exec {
        & $msbuildExe /verbosity:minimal /property:OutDir="..\build\bin\${buildType}-NET45\",IntermediateOutputPath="..\build\obj\${buildType}-NET45\" ".\ThMEPEngine.sln" /p:Configuration="${buildType}-NET45" /t:restore
        & $msbuildExe /verbosity:minimal /property:OutDir="..\build\bin\${buildType}-NET45\",IntermediateOutputPath="..\build\obj\${buildType}-NET45\" ".\ThMEPEngine.sln" /p:Configuration="${buildType}-NET45" /t:rebuild
    }
}

Task Compile.Assembly.R20.FanSelection -Depends Requires.MSBuild {
    exec {
        & $msbuildExe /verbosity:minimal /property:OutDir="..\build\bin\${buildType}-NET45\",IntermediateOutputPath="..\build\obj\${buildType}-NET45\" ".\ThMEPEquipmentSelection.sln" /p:Configuration="${buildType}-NET45" /t:restore
        & $msbuildExe /verbosity:minimal /property:OutDir="..\build\bin\${buildType}-NET45\",IntermediateOutputPath="..\build\obj\${buildType}-NET45\" ".\ThMEPEquipmentSelection.sln" /p:Configuration="${buildType}-NET45" /t:rebuild
    }
}

Task Compile.Resource.R20 -Depends Requires.MSBuild {
    exec {
        & $msbuildExe /verbosity:minimal /property:OutDir="..\build\bin\${buildType}-NET45\Dark\",IntermediateOutputPath="..\build\obj\${buildType}-NET45\Dark\" ".\ThCuiRes\ThCuiRes.vcxproj" /t:rebuild
        & $msbuildExe /verbosity:minimal /property:OutDir="..\build\bin\${buildType}-NET45\Light\",IntermediateOutputPath="..\build\obj\${buildType}-NET45\Light\" ".\ThCuiRes\ThCuiRes_light.vcxproj" /t:rebuild
    }
}

# $buildType build for AutoCAD R22
Task Compile.Assembly.R22.Common -Depends Requires.MSBuild {
    exec {
        & $msbuildExe /verbosity:minimal /property:OutDir="..\build\bin\${buildType}-NET46\",IntermediateOutputPath="..\build\obj\${buildType}-NET46\" ".\ThMEPEngine.sln" /p:Configuration="${buildType}-NET46" /t:restore
        & $msbuildExe /verbosity:minimal /property:OutDir="..\build\bin\${buildType}-NET46\",IntermediateOutputPath="..\build\obj\${buildType}-NET46\" ".\ThMEPEngine.sln" /p:Configuration="${buildType}-NET46" /t:rebuild
    }
}

Task Compile.Assembly.R22.FanSelection -Depends Requires.MSBuild {
    exec {
        & $msbuildExe /verbosity:minimal /property:OutDir="..\build\bin\${buildType}-NET46\",IntermediateOutputPath="..\build\obj\${buildType}-NET46\" ".\ThMEPEquipmentSelection.sln" /p:Configuration="${buildType}-NET46" /t:restore
        & $msbuildExe /verbosity:minimal /property:OutDir="..\build\bin\${buildType}-NET46\",IntermediateOutputPath="..\build\obj\${buildType}-NET46\" ".\ThMEPEquipmentSelection.sln" /p:Configuration="${buildType}-NET46" /t:rebuild
    }
}

Task Compile.Resource.R22 -Depends Requires.MSBuild {
    exec {
        & $msbuildExe /verbosity:minimal /property:OutDir="..\build\bin\${buildType}-NET46\Dark\",IntermediateOutputPath="..\build\obj\${buildType}-NET46\Dark\" ".\ThCuiRes\ThCuiRes.vcxproj" /t:rebuild
        & $msbuildExe /verbosity:minimal /property:OutDir="..\build\bin\${buildType}-NET46\Light\",IntermediateOutputPath="..\build\obj\${buildType}-NET46\Light\" ".\ThCuiRes\ThCuiRes_light.vcxproj" /t:rebuild
    }
}

Task Requires.BuildType {
    if ($buildType -eq $null) {
        throw "No build type specified"
    }

    Write-Host "$buildType build confirmed"
}

Task Compile.Engine -Depends Requires.BuildType, Compile.Assembly.R18.Common, Compile.Assembly.R19.Common, Compile.Assembly.R20.Common, Compile.Assembly.R22.Common
{

}

Task Compile.Resource -Depends Compile.Resource.R18, Compile.Resource.R19, Compile.Resource.R20, Compile.Resource.R22
{

}

Task Compile.FanSelection -Depends Requires.BuildType, Compile.Assembly.R18.FanSelection, Compile.Assembly.R19.FanSelection, Compile.Assembly.R20.FanSelection, Compile.Assembly.R22.FanSelection
{
    
}

# temporarily disable code sign
# $buildType build for ThCADPluginInstaller
Task Compile.Installer -Depends Compile.Engine, Compile.Resource {
    if ($buildType -eq $null) {
        throw "No build type specified"
    }
    exec {
        & $msbuildExe /verbosity:minimal ".\ThMEPInstaller\ThMEPInstaller.wixproj" /p:Configuration=$buildType /t:rebuild
    }
}
