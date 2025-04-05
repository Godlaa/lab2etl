FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY *.csproj .
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

RUN apt-get update && apt-get install -y python3 python3-pip && \
    pip3 install SQLAlchemy psycopg2 pandas openpyxl

RUN mkdir -p DDL

COPY --from=build /src/DDL ./DDL

COPY --from=build /src/DML ./DML

COPY --from=build /app .
ENTRYPOINT ["dotnet", "Lab2ETL.dll"]