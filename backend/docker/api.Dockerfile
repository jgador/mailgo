FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Mailgo.sln ./
COPY src/Mailgo.AppHost/ src/Mailgo.AppHost/

RUN dotnet restore src/Mailgo.AppHost/Mailgo.AppHost.csproj
RUN dotnet publish src/Mailgo.AppHost/Mailgo.AppHost.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "Mailgo.AppHost.dll"]
