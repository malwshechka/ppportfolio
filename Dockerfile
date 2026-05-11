FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 10000
# Оставляем этот параметр, он сообщает .NET, какой порт слушать
ENV ASPNETCORE_URLS=http://+:10000

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Исправляем копирование: сначала копируем .csproj для восстановления зависимостей
COPY ["WebApplication4/WebApplication4.csproj", "WebApplication4/"]
RUN dotnet restore "WebApplication4/WebApplication4.csproj"

# Копируем всё остальное
COPY . .

# Устанавливаем рабочую директорию там, где лежит файл проекта
WORKDIR "/src/WebApplication4"

# Собираем проект (здесь используем имя файла без пути, так как мы уже в папке)
RUN dotnet build "WebApplication4.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "WebApplication4.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WebApplication4.dll"]