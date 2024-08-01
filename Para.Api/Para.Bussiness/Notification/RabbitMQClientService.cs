using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace Para.Bussiness.Notification
{
    public class RabbitMQClientService
    {
        private readonly IConfiguration _configuration;
        private readonly ConnectionFactory _factory;

        public RabbitMQClientService(IConfiguration configuration)
        {
            _configuration = configuration;
            _factory = new ConnectionFactory()
            {
                HostName = _configuration["RabbitMQ:Host"],
                UserName = _configuration["RabbitMQ:Username"],
                Password = _configuration["RabbitMQ:Password"]
            };
        }

        public ConnectionFactory ConnectionFactory => _factory;

        public void Publish(string queueName, object message)
        {
            using var connection = _factory.CreateConnection();
            using var channel = connection.CreateModel();

            channel.QueueDeclare(queue: queueName,
                                 durable: true,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

            channel.BasicPublish(exchange: "",
                                 routingKey: queueName,
                                 basicProperties: null,
                                 body: body);
        }
    }
}
