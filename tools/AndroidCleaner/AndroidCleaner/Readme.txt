AndroidCleaner.exe 

command line:
-adb=<fullpath to adb.exe>
-sn=<Android serial number>, can be optinal if there is only one android device connected. if more then one android devices, this is must.

error code:
0, success erase the sd card
1, adb.exe not found
2, sn not specified and there are more than one android devices.
3, device with serial number=sn, not found
4, device with serial number=sn, offline
5, device with serial number=sn, unauthorized
6, device with serial number=sn, unknown state
7, rm command not found on system
8, EXTERNAL_STORAGE not found in system environment values.