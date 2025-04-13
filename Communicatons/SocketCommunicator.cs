using System.Net.Sockets;
using System.Net;
using System.Globalization;
using System.Text;

public class SocketCommunicator : Communicator
{
    private readonly int _port;

    public SocketCommunicator(int port)
    {
        _port = port;
    }

    private async Task<string> ReadMessageAsync(NetworkStream stream)
    {
        byte[] lengthBuffer = new byte[42];
        int totalRead = 0;
        while (totalRead < 42)
        {
            int bytesRead = await stream.ReadAsync(lengthBuffer, totalRead, 42 - totalRead);
            if (bytesRead == 0)
                throw new Exception("Соединение разорвано до получения заголовка!");
            totalRead += bytesRead;
        }

        int messageLength = BitConverter.ToInt32(lengthBuffer, 0);

        byte[] messageBuffer = new byte[messageLength];
        totalRead = 0;
        while (totalRead < messageLength)
        {
            int bytesRead = await stream.ReadAsync(messageBuffer, totalRead, messageLength - totalRead);
            if (bytesRead == 0)
                throw new Exception("Соединение разорвано до получения всего сообщения!");
            totalRead += bytesRead;
        }

        return Encoding.UTF8.GetString(messageBuffer);
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

            while (true)
            {
                string message = await ReadMessageAsync(stream);
                if (!string.IsNullOrEmpty(message))
                {
                    ProcessLine(message, records);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Ошибка: " + ex.Message);
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