dotnet publish -c Release --force --self-contained -r linux-x64

Remove-Item -Recurse \\Synology\Share\linux-x64
Copy-Item -Recurse bin\Release\netcoreapp2.1\linux-x64 \\Synology\Share\linux-x64

ssh 192.168.178.97 -p 5555