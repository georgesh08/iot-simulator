# docker build -t mops:1 .
# docker run -it -p 16848:16848 mops:1 arg1 arg2 
FROM mcr.microsoft.com/dotnet/sdk:8.0

WORKDIR /DataSimulator

COPY DataSimulator/bin/Release/net8.0/publish /DataSimulator/

EXPOSE 16848

ENTRYPOINT ["dotnet","DataSimulator.dll"]