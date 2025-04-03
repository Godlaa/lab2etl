FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# �������� ������ ����� ������� �������
COPY *.csproj .
RUN dotnet restore

# �������� ��������� �����
COPY . .
RUN dotnet publish "Lab2ETL.csproj" -c Release -o /app /p:GenerateAssemblyInfo=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Lab2ETL.dll"]