using System;
using System.Diagnostics;

namespace Lab2ETL
{
    class ExportModule
    {
        public static void ExportReportUsingPython(string pythonPath, string scriptPath, string outputFile, string dbName, string user, string password, string host, string port)
        {
            try
            {
                string arguments = $"\"{scriptPath}\" \"{outputFile}\" \"{dbName}\" \"{user}\" \"{password}\" \"{host}\" \"{port}\"";

                Console.WriteLine($"Запуск Python: {pythonPath} {arguments}");

                ProcessStartInfo start = new ProcessStartInfo
                {
                    FileName = pythonPath,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = new Process { StartInfo = start })
                {
                    process.Start();
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (!string.IsNullOrEmpty(error))
                    {
                        Console.WriteLine($"Ошибка при выполнении Python-скрипта:\n{error}");
                    }
                    else
                    {
                        Console.WriteLine($"Отчёт успешно экспортирован: {outputFile}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка запуска Python-скрипта: {ex.Message}");
            }
        }
    }
}
