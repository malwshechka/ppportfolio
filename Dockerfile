FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 10000
ENV ASPNETCORE_URLS=http://+:10000

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
# ВАЖНО: Путь должен быть WebApplication4/WebApplication4.csproj
COPY ["WebApplication4/WebApplication4.csproj", "WebApplication4/"]
RUN dotnet restore "WebApplication4/WebApplication4.csproj"
COPY . .
WORKDIR "/src/WebApplication4"
RUN dotnet build "WebApplication4.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "WebApplication4.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WebApplication4.dll"]