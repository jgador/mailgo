FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY EmailMarketing.sln ./
COPY EmailMarketing.Domain/ EmailMarketing.Domain/
COPY EmailMarketing.Api/ EmailMarketing.Api/

RUN dotnet restore EmailMarketing.Api/EmailMarketing.Api.csproj
RUN dotnet publish EmailMarketing.Api/EmailMarketing.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "EmailMarketing.Api.dll"]
