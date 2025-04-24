using RabbitMQ.Client;
using System.Text;
using System.Globalization;
using RabbitMQ.Client.Events;
using System.Threading;

public class QueueCommunicator : Communicator
{
    private readonly string _hostName;
    private readonly string _queueName;

    public QueueCommunicator(string hostName, string queueName)
    {
        this._hostName = hostName;
        this._queueName = queueName;
    }

    public override async Task<List<Dictionary<string, object>>> GetMessage()
    {
        var records = new List<Dictionary<string, object>>();
        var factory = new ConnectionFactory
        {
            HostName = this._hostName,
            Port = 5672,
            UserName = "admin",
            Password = "password"
        };

        using var connection = await factory.CreateConnectionAsync();
        using var channel = await connection.CreateChannelAsync();

        await channel.QueueDeclareAsync(
            queue: this._queueName,
            durable: false,
            exclusive: false,
            autoDelete: true,
            arguments: null
        );

        var consumer = new AsyncEventingBasicConsumer(channel);
        string consumerTag = await channel.BasicConsumeAsync(this._queueName, false, consumer);
        var waitForMessage = new TaskCompletionSource();

        consumer.ReceivedAsync += async (ch, ea) =>
        {
            try
            {
                waitForMessage.TrySetResult();

                var body = ea.Body.ToArray();
                string message = Encoding.UTF8.GetString(body);

                ProcessMessage(message, records);

                if (channel.IsOpen)
                {
                    await channel.BasicAckAsync(ea.DeliveryTag, false);
                }
                Console.WriteLine($"Сообщение обработано: {message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обработки: {ex.Message}");
            }
        };

        await Task.WhenAny(
            waitForMessage.Task,
            Task.Delay(10000)
        );

        await channel.BasicCancelAsync(consumerTag);

        await channel.CloseAsync();
        await connection.CloseAsync();

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
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка обработки сообщения {message}: {ex.Message}");
        }
    }
}
