# Set Working Directory
Split-Path $MyInvocation.MyCommand.Path | Push-Location
[Environment]::CurrentDirectory = $PWD

Remove-Item "$env:RELOADEDIIMODS/p3ppc.multiplemovies/*" -Force -Recurse
dotnet publish "./p3ppc.multiplemovies.csproj" -c Release -o "$env:RELOADEDIIMODS/p3ppc.multiplemovies" /p:OutputPath="./bin/Release" /p:ReloadedILLink="true"

# Restore Working Directory
Pop-Location