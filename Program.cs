using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.Sqlite;
using Npgsql;
using System.IO;
using Lab2ETL;
using System.Text;
using Lab2ETL.Export;

class Program
{
    static async Task Main(string[] args)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        string host = Environment.GetEnvironmentVariable("POSTGRES_HOST") ?? "postgres";
        string port = Environment.GetEnvironmentVariable("POSTGRES_PORT") ?? "5432";
        string dbName = Environment.GetEnvironmentVariable("POSTGRES_DB") ?? "orders";
        string user = Environment.GetEnvironmentVariable("POSTGRES_USER") ?? "postgres";
        string password = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD") ?? "postgres";
        string pgConnString = $"Host={host};Port={port};Database={dbName};Username={user};Password={password};";
        string[] actions = { "1. Создать таблицу в SQLite", "2. Создать таблицу в PostgreSQL", "3. Заполнить таблицу в SQLite", "4. Экспортировать данные в PostgreSQL", "5. Экспорт в Excel", "6. Выбор метода получения данных Сокеты/Очередь сообщений", "7. Выход" };
        bool done = false;
        int communticator = -1;

        DataTransformer transformer = new DataTransformer(pgConnString);

        while (!done)
        {
            Console.WriteLine("Выберите действие:");
            foreach (var action in actions)
            {
                Console.WriteLine(action);
            }

            string choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    transformer.CreateSqliteTable();
                    break;
                case "2":
                    transformer.CreatePostgresTable();
                    break;
                case "3":
                    transformer.FillSqliteTable();
                    break;
                case "4":
                    if (communticator == -1)
                    {
                        Console.WriteLine("Нужно выбрать тип коммуникатора");
                        return;
                    }
                    else
                    {
                        await transformer.TransformAndLoadAsync(communticator);
                    }
                    break;
                case "5":
                    string pythonPath = "/app/venv/bin/python";
                    string scriptPath = "/app/Export/export_report.py";
                    string outputFile = "/app/Export/report.xlsx";
                    ExportModule.ExportReportUsingPython(pythonPath, scriptPath, outputFile, dbName, user, password, host, port);
                    break;
                case "6":
                    Console.Clear();
                    Console.Write("1. Сокеты\n2. Очередь сообщений\n");
                    switch (int.Parse(Console.ReadLine()) - 1)
                    {
                        case (int)Communicator.Type.Socket:
                            communticator = (int)Communicator.Type.Socket;
                            break;
                        case (int)Communicator.Type.Queue:
                            communticator = (int)Communicator.Type.Queue;
                            break;
                        default:
                            Console.WriteLine("Выбран несуществующий тип");
                            break;
                    }
                    break;
                case "7":
                    done = true;
                    break;
                default:
                    Console.WriteLine("Неверный выбор, попробуйте снова.");
                    break;
            }

            Thread.Sleep(3000);
            Console.Clear();
        }
    }
}