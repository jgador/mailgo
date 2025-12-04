FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Mailgo.sln ./
COPY src/Mailgo.Domain/ src/Mailgo.Domain/
COPY src/Mailgo.Api/ src/Mailgo.Api/

RUN dotnet restore src/Mailgo.Api/Mailgo.Api.csproj
RUN dotnet publish src/Mailgo.Api/Mailgo.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "Mailgo.Api.dll"]
