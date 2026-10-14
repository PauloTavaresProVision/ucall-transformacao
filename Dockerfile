FROM node:24-alpine AS frontend
WORKDIR /src/frontend
COPY frontend/package*.json ./
RUN npm ci --no-audit --no-fund
COPY frontend/ ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS backend
WORKDIR /src
COPY NuGet.Config ./
COPY api/Ucall.Api.csproj api/
RUN dotnet restore api/Ucall.Api.csproj --configfile NuGet.Config
COPY api/ api/
RUN dotnet publish api/Ucall.Api.csproj -c Release -o /app --no-restore
COPY --from=frontend /src/frontend/dist /app/wwwroot

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=backend /app ./
RUN mkdir -p /app/data && chown -R app:app /app/data
USER app
ENV ASPNETCORE_URLS=http://+:8080 Storage__Root=/app/data
EXPOSE 8080
ENTRYPOINT ["dotnet","Ucall.Api.dll"]
