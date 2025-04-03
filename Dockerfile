FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY *.csproj .
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

RUN mkdir -p DDL

COPY --from=build /src/DDL ./DDL

COPY --from=build /src/DML ./DML

COPY --from=build /app .
ENTRYPOINT ["dotnet", "Lab2ETL.dll"]