function Get-ProtocLocation
{
    $root = Resolve-Path -Path "..\"
    return Join-Path -Path $root -ChildPath "Tools\protoc-21.5-win64\bin\protoc.exe"
}

Task Compile.Protoc {
    $protoc = Get-ProtocLocation
    $path = "$PSScriptRoot\ThMEPTCH\proto"
    Push-Location $path
    foreach($proto in Get-ChildItem "$path" -Recurse -Include "*.proto") {
        exec {
            & $protoc --csharp_out="..\Data" (Get-Item $proto).Name
        }
    }
    Pop-Location
}
