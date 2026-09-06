# =========================
# Stage 1: Build
# =========================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy file solution + props trước để tận dụng cache restore
COPY ["Platform.slnx", "./"]
COPY ["common.props", "./"]
COPY ["NuGet.Config", "./"]

# Copy toàn bộ csproj để restore (tách layer giúp cache tốt hơn)
COPY ["src/Platform.Identity.Domain.Shared/Platform.Identity.Domain.Shared.csproj", "src/Platform.Identity.Domain.Shared/"]
COPY ["src/Platform.Identity.Domain/Platform.Identity.Domain.csproj", "src/Platform.Identity.Domain/"]
COPY ["src/Platform.Identity.Application.Contracts/Platform.Identity.Application.Contracts.csproj", "src/Platform.Identity.Application.Contracts/"]
COPY ["src/Platform.Identity.Application/Platform.Identity.Application.csproj", "src/Platform.Identity.Application/"]
COPY ["src/Platform.Identity.EntityFrameworkCore/Platform.Identity.EntityFrameworkCore.csproj", "src/Platform.Identity.EntityFrameworkCore/"]
COPY ["src/Platform.Identity.HttpApi/Platform.Identity.HttpApi.csproj", "src/Platform.Identity.HttpApi/"]
COPY ["src/Platform.Identity.HttpApi.Client/Platform.Identity.HttpApi.Client.csproj", "src/Platform.Identity.HttpApi.Client/"]
COPY ["src/Platform.Identity.HttpApi.Host/Platform.Identity.HttpApi.Host.csproj", "src/Platform.Identity.HttpApi.Host/"]
COPY ["src/Platform.Identity.DbMigrator/Platform.Identity.DbMigrator.csproj", "src/Platform.Identity.DbMigrator/"]

# Restore
RUN dotnet restore "src/Platform.Identity.HttpApi.Host/Platform.Identity.HttpApi.Host.csproj"

# Copy toàn bộ source
COPY . .

# Publish
WORKDIR "/src/src/Platform.Identity.HttpApi.Host"
RUN dotnet publish "Platform.Identity.HttpApi.Host.csproj" -c Release -o /app/publish /p:UseAppHost=false --no-restore

# =========================
# Stage 2: Runtime
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Platform.Identity.HttpApi.Host.dll"]