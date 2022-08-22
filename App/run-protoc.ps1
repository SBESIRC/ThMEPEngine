function Get-ProtocLocation
{
    $root = Resolve-Path -Path "..\"
    return Join-Path -Path $root -ChildPath "Tools\protoc-21.5-win64\bin\protoc.exe"
}

Task CSharp {
    $script:out= "csharp"
}

Task Ruby {
    $script:out= "ruby"
}

Task Compile {
    $protoc = Get-ProtocLocation
    $path = "$PSScriptRoot\ThMEPTCH\proto"
    Push-Location $path
    foreach($proto in Get-ChildItem "$path" -Recurse -Include "*.proto") {
        exec {
            if ($script:out -eq "csharp") {
                & $protoc --csharp_out=".\" (Get-Item $proto).Name
            } elseif ($script:out -eq "ruby") {
                & $protoc --ruby_out=".\" (Get-Item $proto).Name
            }
        }
    }
    Pop-Location
}
