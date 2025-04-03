using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System.Text;
using System.Globalization;

public class QueueCommunicator : Communicator
{
    private readonly string _hostName;
    private readonly string _queueName;

    public QueueCommunicator(string hostName, string queueName)
    {
        _hostName = hostName;
        _queueName = queueName;
    }

    public override async Task<List<Dictionary<string, object>>> GetMessage()
    {
        var records = new List<Dictionary<string, object>>();
        var factory = new ConnectionFactory { HostName = _hostName };

        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();
        var consumer = new EventingBasicConsumer(channel);

        var tcs = new TaskCompletionSource();
        consumer.Received += (model, ea) =>
        {
            var message = Encoding.UTF8.GetString(ea.Body.ToArray());
            ProcessMessage(message, records);
            channel.BasicAck(ea.DeliveryTag, false);
        };

        channel.BasicConsume(_queueName, false, consumer);

        await Task.Delay(-1, new CancellationToken(true));

        return records;
    }

    private void ProcessMessage(string message, List<Dictionary<string, object>> records)
    {
        var fields = message.Split(',', StringSplitOptions.RemoveEmptyEntries);

        if (fields.Length != 7)
        {
            Console.WriteLine($"Некорректный формат сообщения: {message}");
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
            Console.WriteLine($"Обработано сообщение: {message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка обработки сообщения {message}: {ex.Message}");
        }
    }
}