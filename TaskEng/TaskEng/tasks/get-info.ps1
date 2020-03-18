param(
    [string] $version
)

Write-Host "get-info: ++"


$ret=@{version=$version; error=0} | convertto-json
Write-Host "get-info: --"
return $ret 