$server=$null
if([System.Environment]::Is64BitOperatingSystem){
    $server = Start-Process -FilePath ".\Redis-x64-3.2.100\redis-server.exe" -ArgumentList ".\redis.windows.conf" -NoNewWindow -PassThru
}
else{
    $server = Start-Process -FilePath ".\Redis-x86-3.0.504\redis-server.exe" -ArgumentList ".\redis.windows.conf" -NoNewWindow -PassThru
}
if($null -eq $server){
    Write-Host "Start Redis failure"
    exit 1
}
else{
    $p=Start-Process -FilePath ".\Redis-x86-3.0.504\redis-cli.exe" -ArgumentList "Flushall" -NoNewWindow -PassThru
    $p.WaitForExit()
}
exit 0
