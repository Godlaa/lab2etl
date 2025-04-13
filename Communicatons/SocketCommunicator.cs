using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

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
        var listener = new TcpListener(IPAddress.Any, _port);
        listener.Start();
        Console.WriteLine($"Слушает на порту {_port}.");

        try
        {
            using var client = await listener.AcceptTcpClientAsync();
            using var networkStream = client.GetStream();
            using var reader = new BinaryReader(networkStream, Encoding.UTF8, leaveOpen: true);

            while (true)
            {
                int length;
                try
                {
                    length = reader.ReadInt32();
                }
                catch (EndOfStreamException)
                {
                    break;
                }

                byte[] data = await ReadExactlyAsync(networkStream, length);
                if (data.Length < length)
                {
                    Console.WriteLine("Получено неполное сообщение.");
                    break;
                }
                string line = Encoding.UTF8.GetString(data);
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

    private async Task<byte[]> ReadExactlyAsync(NetworkStream stream, int count)
    {
        byte[] buffer = new byte[count];
        int offset = 0;
        while (offset < count)
        {
            int bytesRead = await stream.ReadAsync(buffer, offset, count - offset);
            if (bytesRead == 0)
            {
                break;
            }
            offset += bytesRead;
        }
        if (offset < count)
        {
            Array.Resize(ref buffer, offset);
        }
        return buffer;
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
