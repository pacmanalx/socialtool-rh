# SocialTool RH — imagem única: o backend .NET serve o front compilado pelo wwwroot.
# Nenhum segredo entra na imagem: configuração e credenciais vêm por variável de ambiente.

# ── 1. front ────────────────────────────────────────────────────────────────
FROM node:22-alpine AS front
WORKDIR /src
COPY frontend/package*.json ./
RUN npm ci
COPY frontend/ ./
# Sem VITE_API_URL: o backend serve este front, então a origem é a mesma.
RUN npm run build

# ── 2. backend ──────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS back
WORKDIR /src
# Restore antes do código: a camada de pacotes só invalida quando um .csproj muda.
COPY backend/*.slnx ./
COPY backend/src/SocialTool.Domain/*.csproj         src/SocialTool.Domain/
COPY backend/src/SocialTool.Application/*.csproj    src/SocialTool.Application/
COPY backend/src/SocialTool.Infrastructure/*.csproj src/SocialTool.Infrastructure/
COPY backend/src/SocialTool.Api/*.csproj            src/SocialTool.Api/
RUN dotnet restore src/SocialTool.Api
COPY backend/ ./
RUN dotnet publish src/SocialTool.Api -c Release -o /app --no-restore --nologo

# ── 3. runtime ──────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=back  /app ./
COPY --from=front /src/dist ./wwwroot

# A imagem da Microsoft já traz o usuário `app`, sem privilégio.
USER app
ENV ASPNETCORE_URLS=http://0.0.0.0:8080 \
    DOTNET_CLI_TELEMETRY_OPTOUT=1
EXPOSE 8080
ENTRYPOINT ["dotnet", "SocialTool.Api.dll"]
