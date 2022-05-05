# Frequently Asked Questions

## How to remove a server on the list I no longer need?
Right click on the server name. A menu should popup. Select _Forget_.

## How to reset all my client settings?
Close the eduVPN and Let's Encrypt! clients first. Open a command prompt and paste one or both lines:

```cmd
for /d %i in ("%LOCALAPPDATA%\SURF\eduVPN.Client.exe_Url_*") do rd /s /q "%i"
for /d %i in ("%LOCALAPPDATA%\SURF\LetsConnect.Client.exe_Url_*") do rd /s /q "%i"
```

Don't forget to press _Enter_ key after the line is pasted. This will reset eduVPN and Let's Encrypt! clients to factory defaults.
