# Generic runtime image for any Kafadanat .NET 10 service.
#
# Strategy: publish on the HOST (where your Onspay local feed and the
# Kafadanat.Contracts GitHub Packages token already restore), then copy the
# output in. This avoids wiring private NuGet feeds into the image build.
#
# Drop this file in EACH service repo root, then from that repo run:
#   dotnet publish <startup-project> -c Release -o publish
# (publish-all.sh does this for all seven at once.)

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY publish/ ./
# APP_DLL is passed per-service from compose (e.g. Web.Api.dll, Gateway.dll,
# Kafadanat.Engine.Api.dll). exec form so signals reach the process.
ARG APP_DLL
ENV APP_DLL=${APP_DLL}
ENTRYPOINT ["sh", "-c", "exec dotnet \"$APP_DLL\""]
