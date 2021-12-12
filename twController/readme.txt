input
-path="C:\ProgramData\FutureDial\TetherWing\info" -sts -max=9 -quitevent=TetherWingQuitEvent

status:

109; //MobileQ "Completed Successfully" status with "Detailed Report" link
108; // incomplete... maybe user disconnected device in between task 
110; //complete with some items skipped 


feature:

1. linstern for change xml from socket:
a http linsten on local host 1210, 
request format:
curl.exe http://127.0.0.1:1210/modifyinfo --data-urlencode label=1 --data-urlencode xpath="/labelinfo/device/carrier" --data-urlencode value=ATT

2. add status configure in config.ini
add a section "[status] in config.ini
[status]
9=89
