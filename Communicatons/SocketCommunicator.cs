using System.Net.Sockets;
using System.Net;
using System.Globalization;

public class SocketCommunicator : Communicator
{
    private readonly int _port;

    public SocketCommunicator(int port)
    {
        _port = port;
    }

    public override async Task<List<Dictionary<string, object>>> GetMessage()
    {
        var records = new List<Dictionary<string, object>>();
        var listener = new TcpListener(IPAddress.Any, this._port);
        listener.Start();

        try
        {
            using var client = await listener.AcceptTcpClientAsync();
            using var stream = client.GetStream();
            using var reader = new StreamReader(stream);

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (!string.IsNullOrEmpty(line))
                {
                    ProcessLine(line, records);
                }
            }
        }
        finally
        {
            listener.Stop();
        }

        return records;
    }

    private void ProcessLine(string line, List<Dictionary<string, object>> records)
    {
        var fields = line.Split(',', StringSplitOptions.RemoveEmptyEntries);

        if (fields.Length != 7)
        {
            Console.WriteLine($"Некорректный формат строки: {line}");
            return;
        }

        try
        {
            var record = new Dictionary<string, object>
            {
                ["order_date"] = DateTime.ParseExact(fields[0].Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture),
                ["customer_name"] = fields[1].Trim(),
                ["customer_phone"] = fields[2].Trim(),
                ["product_name"] = fields[3].Trim(),
                ["product_category"] = fields[4].Trim(),
                ["product_price"] = decimal.Parse(fields[5].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture),
                ["quantity"] = int.Parse(fields[6].Trim(), NumberStyles.Integer)
            };

            records.Add(record);
            Console.WriteLine($"Обработана строка: {line}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка обработки строки {line}: {ex.Message}");
        }
    }
}