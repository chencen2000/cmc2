Write-Host "start: ++"
$a=Get-Process
Get-Date
Write-Host "start: --"
$ret =  @{errcode=0; result=$a}
return $ret