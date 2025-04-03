# Используем правильные версии .NET
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Копируем только файлы проекта сначала
COPY *.sln .
COPY Lab2ETL/*.csproj ./Lab2ETL/
RUN dotnet restore

# Копируем остальные файлы
COPY Lab2ETL/. ./Lab2ETL/

# Сборка и публикация
RUN dotnet publish -c Release -o /app

# Финальный образ
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Lab2ETL.dll"]