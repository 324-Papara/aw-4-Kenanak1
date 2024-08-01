using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;
using System.Text.Json;

namespace Para.Bussiness.Notification
{
    public class EmailSenderJob
    {
        private readonly INotificationService _notificationService;
        private readonly RabbitMQClientService _rabbitMQClientService;

        public EmailSenderJob(INotificationService notificationService, RabbitMQClientService rabbitMQClientService)
        {
            _notificationService = notificationService;
            _rabbitMQClientService = rabbitMQClientService;
        }

        public void ProcessQueue()
        {
            using var connection = _rabbitMQClientService.ConnectionFactory.CreateConnection();
            using var channel = connection.CreateModel();
            var queueName = "emailQueue";

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var emailMessage = JsonSerializer.Deserialize<EmailMessage>(message);

                _notificationService.SendEmail(emailMessage.Subject, emailMessage.Email, emailMessage.Content);
            };

            channel.BasicConsume(queue: queueName, autoAck: true, consumer: consumer);
        }
    }

    public class EmailMessage
    {
        public string Subject { get; set; }
        public string Email { get; set; }
        public string Content { get; set; }
    }
}
