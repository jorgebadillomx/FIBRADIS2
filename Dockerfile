# Stage 1 — Build SPAs
FROM node:22-alpine AS spa-build
WORKDIR /app

# Instalar dependencias de todos los workspaces primero (caching de capas)
COPY package.json package-lock.json ./
COPY src/Web/Main/package.json src/Web/Main/
COPY src/Web/Ops/package.json src/Web/Ops/
COPY src/Web/SharedApiClient/package.json src/Web/SharedApiClient/
RUN npm ci

# Copiar fuentes y buildar
COPY src/Web/ src/Web/
RUN npm run build --workspace=src/Web/Main
RUN npm run build --workspace=src/Web/Ops

# Stage 2 — Build API .NET
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS api-build
WORKDIR /src

# Restaurar dependencias primero (caching de capas)
COPY Directory.Packages.props Directory.Build.props global.json ./
COPY src/Server/ src/Server/
RUN dotnet restore src/Server/Api

# Publicar
RUN dotnet publish src/Server/Api -c Release --no-restore -o /app/publish

# Stage 3 — Imagen final
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

# Binarios del API
COPY --from=api-build /app/publish .

# Main SPA → wwwroot/
COPY --from=spa-build /app/src/Web/Main/dist ./wwwroot/

# Ops SPA → wwwroot/ops/
COPY --from=spa-build /app/src/Web/Ops/dist ./wwwroot/ops/

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "Api.dll"]
