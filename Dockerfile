# ���������� ���������� ������ .NET
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# �������� ������ ����� ������� �������
COPY *.sln .
COPY Lab2ETL/*.csproj ./Lab2ETL/
RUN dotnet restore

# �������� ��������� �����
COPY Lab2ETL/. ./Lab2ETL/

# ������ � ����������
RUN dotnet publish -c Release -o /app

# ��������� �����
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Lab2ETL.dll"]