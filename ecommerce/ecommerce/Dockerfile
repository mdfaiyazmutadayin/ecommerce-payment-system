escape=`,
FROM mcr.microsoft.com/dotnet/framework/aspnet:4.8-windowsservercore-ltsc2022

RUN powershell -Command 
    Install-WindowsFeature Web-Server; 
    Install-WindowsFeature NET-Framework-45-ASPNET; `
    Install-WindowsFeature Web-Asp-Net45

WORKDIR /inetpub/wwwroot
COPY ./publish/ .

EXPOSE 80