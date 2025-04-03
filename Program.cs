using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.Sqlite;
using Npgsql;
using System.IO;
using Lab2ETL;
using System.Text;

class Program
{
    static void Main(string[] args)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        string host = "localhost";
        string port = "5432";
        string dbName = "postgres";
        string user = "postgres";
        string password = "12345";
        string sqliteConnString = @"Data Source=D:\sqliteDB\orders_denormalized.db";
        string pgConnString = $"Host={host};Port={port};Database={dbName};Username={user};Password={password};";
        string[] actions = { "1. Создать таблицу в SQLite", "2. Создать таблицу в PostgreSQL", "3. Заполнить таблицу в SQLite", "4. Экспортировать данные в PostgreSQL", "5. Экспорт в Excel", "6. Выход" };
        bool done = false;

        DataTransformer transformer = new DataTransformer(sqliteConnString, pgConnString);

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
                    transformer.TransformAndLoad();
                    break;
                case "5":
                    string pythonPath = @"C:\\Python313\python.exe";
                    string scriptPath = @"D:\Lab2ETL\export_report.py";
                    string outputFile = @"D:\Lab2ETL\report.xlsx";
                    ExportModule.ExportReportUsingPython(pythonPath, scriptPath, outputFile, dbName, user, password, host, port);
                    break;
                case "6":
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