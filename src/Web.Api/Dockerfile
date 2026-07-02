# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
# GITHUB_USER / GITHUB_TOKEN feed the %GITHUB_USER%/%GITHUB_TOKEN% placeholders in
# nuget.config to restore the private Kafadanat.Contracts feed.
ARG GITHUB_USER
ARG GITHUB_TOKEN
WORKDIR /src
COPY . .
RUN dotnet restore "src/Web.Api/Web.Api.csproj"
RUN dotnet publish "src/Web.Api/Web.Api.csproj" \
    -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "Web.Api.dll"]
