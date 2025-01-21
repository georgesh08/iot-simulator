# docker build -t iot:1 .
# docker run -it iot:1 

FROM mcr.microsoft.com/dotnet/sdk:8.0

WORKDIR /RuleEngine

COPY RuleEngine/bin/Release/net8.0/publish /RuleEngine//

# EXPOSE 18686

ENTRYPOINT ["dotnet","RuleEngine.dll"]