FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY AccessDesk.slnx ./
COPY src/AccessDesk.Domain/AccessDesk.Domain.csproj src/AccessDesk.Domain/
COPY src/AccessDesk.Application/AccessDesk.Application.csproj src/AccessDesk.Application/
COPY src/AccessDesk.Infrastructure/AccessDesk.Infrastructure.csproj src/AccessDesk.Infrastructure/
COPY src/AccessDesk.Api/AccessDesk.Api.csproj src/AccessDesk.Api/
RUN dotnet restore src/AccessDesk.Api/AccessDesk.Api.csproj

COPY src/ src/
RUN dotnet publish src/AccessDesk.Api/AccessDesk.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AccessDesk.Api.dll"]
