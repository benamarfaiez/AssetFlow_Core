# =============================================================================
# AssetFlowCore — Dockerfile Multi-Stage
# Framework  : .NET 8.0
# Base image : mcr.microsoft.com/dotnet/aspnet:8.0-alpine (légère, ~100MB)
# =============================================================================

# -----------------------------------------------------------------------------
# STAGE 1 — restore
# Restaure les packages NuGet en isolant les fichiers .csproj
# Avantage : Docker cache cette couche tant que les dépendances n'ont pas changé
# -----------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS restore

WORKDIR /src

# Copie uniquement les fichiers projet pour maximiser le cache Docker
COPY AssetFlowCore.Domain/AssetFlowCore.Domain.csproj               AssetFlowCore.Domain/
COPY AssetFlowCore.Application/AssetFlowCore.Application.csproj     AssetFlowCore.Application/
COPY AssetFlowCore.Infrastructure/AssetFlowCore.Infrastructure.csproj AssetFlowCore.Infrastructure/
COPY AssetFlowCore.WebApi/AssetFlowCore.WebApi.csproj               AssetFlowCore.WebApi/

# Restaure toutes les dépendances NuGet
RUN dotnet restore AssetFlowCore.WebApi/AssetFlowCore.WebApi.csproj \
    --runtime linux-musl-x64 \
    /p:PublishReadyToRun=false

# -----------------------------------------------------------------------------
# STAGE 2 — build
# Compile le projet en Release
# -----------------------------------------------------------------------------
FROM restore AS build

WORKDIR /src

# Copie tout le code source
COPY AssetFlowCore.Domain/        AssetFlowCore.Domain/
COPY AssetFlowCore.Application/   AssetFlowCore.Application/
COPY AssetFlowCore.Infrastructure/ AssetFlowCore.Infrastructure/
COPY AssetFlowCore.WebApi/        AssetFlowCore.WebApi/

RUN dotnet build AssetFlowCore.WebApi/AssetFlowCore.WebApi.csproj \
    --configuration Release \
    --no-restore \
    --runtime linux-musl-x64 \
    -o /app/build

# -----------------------------------------------------------------------------
# STAGE 3 — publish
# Produit les artefacts de publication optimisés (ReadyToRun désactivé pour Alpine)
# -----------------------------------------------------------------------------
FROM build AS publish

RUN dotnet publish AssetFlowCore.WebApi/AssetFlowCore.WebApi.csproj \
    --configuration Release \
    --no-restore \
    --no-build \
    --runtime linux-musl-x64 \
    --self-contained false \
    -o /app/publish \
    /p:UseAppHost=false

# -----------------------------------------------------------------------------
# STAGE 4 — final (image de production)
# Image finale légère : uniquement le runtime ASP.NET, sans le SDK
# -----------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final

# ── Métadonnées de l'image ──────────────────────────────────────────────────
LABEL maintainer="AssetFlowCore Team"
LABEL app="assetflow-api"
LABEL version="1.0.0"
LABEL description="AssetFlowCore — API de gestion du parc informatique"

# ── Variables d'environnement ───────────────────────────────────────────────
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# ── Dépendances système (Alpine) ────────────────────────────────────────────
# icu-libs  : requis pour la globalisation .NET (dates, cultures)
# tzdata    : fuseaux horaires (migrations EF avec DateTimeOffset)
# ca-certificates : SSL/TLS vers SQL Server
RUN apk add --no-cache \
    icu-libs \
    tzdata \
    ca-certificates \
    && rm -rf /var/cache/apk/*

WORKDIR /app

# ── Copie des artefacts de publication ──────────────────────────────────────
COPY --from=publish /app/publish .

# ── Sécurité : utilisateur non-root ─────────────────────────────────────────
# Crée un utilisateur dédié sans shell ni home directory
RUN addgroup -S assetflow \
    && adduser -S assetflow -G assetflow -H -s /sbin/nologin \
    && chown -R assetflow:assetflow /app

USER assetflow

# ── Port exposé ─────────────────────────────────────────────────────────────
# Port interne 8080 (non-root ne peut pas binder < 1024)
EXPOSE 8080

# ── Health check ────────────────────────────────────────────────────────────
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health \
    || exit 1

# ── Commande de démarrage ────────────────────────────────────────────────────
ENTRYPOINT ["dotnet", "AssetFlowCore.WebApi.dll"]