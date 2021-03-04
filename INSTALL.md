# How to get started
Install the [psake](https://www.powershellgallery.com/packages/psake/4.9.0) and  [VSSetup](https://www.powershellgallery.com/packages/VSSetup/2.2.16) PowerShell Modules

````PowerShell
Install-Module -Name psake
Install-Module -Name VSSetup
````

# Script
Use the scripts without "-" in the name(gitpull, build, release)  
gitpull.ps1 Run it will git clone a new project in "./", best place it in a empty folder  
build.ps1 Run it located in */ExpressTools/App only  
release.ps1 It have no limit of location  
## gitpull  
**step**  
1:Copy gitpull.ps1 to a new folder  
2:Run it  
**usage**  
.\gitpull url version  
**example**  
.\gitpull.ps1 https://github.com/shichongdong/ExpressTools 1.0.1  
  
## build  
**usage**  
.\build buildtype (version)  
(The parameter version is not obligatory.If parameter with version, the build.ps1 will call script "bump-version.ps1")  
**example**  
.\build.ps1 release 1.0.1  
.\build.ps1 release  
  
## publish
**usage**  
.\publish <msi file path>  
**example**  
.\publish.ps1 *.msi  
(Pulling file in powershell as parameters is permited)  

## install
Firstly, install [psmsi](https://github.com/heaths/psmsi), a Windows Installer PowerShell Module:
````PowerShell
Install-Module MSI
````
Then, run the following command to install the MSI file:
````PowerShell
Install-MSIProduct <path>\ThCADInstaller.msi
````
And to run the following command to uninstall the MSI file:
````PowerShell
Uninstall-MSIProduct <path>\ThCADInstaller.msi
````

## other script(do the percise active)  
### bump-version  
**usage**  
.\bump-version folder version version  
**example**  
.\bump-version.ps1 ./ 1.0.0 1.0.1
(Change the version 1.0.0 to 1.0.1, if an assembly version is not 1.0.0, it won't be changed.)
(The first version can be "all". If you do this, all the assemblies will be changed whater their versions are)  

### run-build(Install paske)  
**Usage**  
Invoke-psake .\run-build.ps1 -Task "${buildType}.Build", Compile.Installer  
**example**  
Invoke-psake .\run-build.ps1 -Task release.build, Compile.Installer
  
### run-nunit(Install paske)  
**usage**  
Invoke-psake .\run-nunit.ps1 -Task "${buildType}.Build", Unit.Tests  
**example**  
Invoke-psake .\run-nunit.ps1 -Task release.Build, Unit.Tests  
Invoke-psake .\run-nunit.ps1 -Task release.Build, Gallio.Tests  

### run-harness(Install paske)  
**usage**  
Invoke-psake .\run-harness.ps1 -Task "${buildType}.Build", Harness  
**example**  
Invoke-psake .\run-harness.ps1 -Task release.Build, Harness  
**notice**  
If you want to run the ui-test, you must run command ("C:\Program Files (x86)\Windows Application Driver\WinAppDriver.exe") in cmd before  
First, install (WinAppDriver). [download](https://github.com/Microsoft/WinAppDriver/releases)  
Senond, enable Windows 10 developer mode [reference]https://blog.csdn.net/dyxcome/article/details/82948945  
third, run cmd and run command ("C:\Program Files (x86)\Windows Application Driver\WinAppDriver.exe")  
forth, run (Invoke-psake .\run-nunit.ps1 -Task release.Build, Unit.Tests) in powershell or run build.ps1  
[read more about WinAppDriver](https://github.com/Microsoft/WinAppDriver)
