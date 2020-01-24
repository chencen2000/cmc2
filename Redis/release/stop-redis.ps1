$p=Start-Process -FilePath ".\Redis-x86-3.0.504\redis-cli.exe" -ArgumentList "shutdown" -NoNewWindow -PassThru
$p.WaitForExit()
