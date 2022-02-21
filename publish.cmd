taskkill /f /im w3wp*
iisreset
git pull
dotnet build swap-faces\swap-faces.csproj
dotnet publish swap-faces\swap-faces.csproj

