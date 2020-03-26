twLauncher.exe

step 1
copy twLauncher.exe to c:\programdata\futuredial folder
and run as it.

step 2
if %apsthome%startui.ps1 exists then run it
else
<###
$env:APSTHOME\FDAutoUpdate.ini
[config]
status=12 
###>
if download completed then start deploy, go to step 2
else messagebox.show.