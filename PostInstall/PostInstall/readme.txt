PostInstall will executed after installation.
1. input the folder of cmc installation, the config.ini should already exists.
2. download the CMC-INSTALLATION, from CMC base on the config.ini
config.ini
pitype=<prl|phonedll>
piid=[readableid]




### get cinfig.ini
fdcheckserial.exe -s 521eb3dd-47f0-40ef-9b54-30466dfe6cc7 -d .
OR
### ps.futuredial.com api
GET https://ps.futuredial.com/profiles/clients/_find
  ?criteria={"_id": "521eb3dd-47f0-40ef-9b54-30466dfe6cc7"}
Content-Type: application/json

### list prl packages 
GET http://cmcqa.futuredial.com/api/listpkgs/
    ?type=prl    
    &solutionid=45
    &productid=55
