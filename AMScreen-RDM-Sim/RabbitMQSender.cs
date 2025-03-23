using RabbitMQ.Client;
using System;
using System.IO;
using System.Text;
using System.Text.Json;

namespace Messaging
{
    /// <summary>
    /// This class is responsible for sending messages to RabbitMQ.
    /// </summary>
    public class RabbitMQSender
    {
        private readonly string _hostname;
        private readonly string _queueName;
        private readonly string _exchangeName;
        private readonly int _port;
        private readonly string _username;
        private readonly string _password;

        /// <summary>
        /// Initializes a new instance of the <see cref="RabbitMQSender"/> class.
        /// </summary>
        /// <param name="hostname">The RabbitMQ hostname.</param>
        /// <param name="queueName">The RabbitMQ queue name.</param>
        /// <param name="exchangeName">The RabbitMQ exchange name.</param>
        /// <param name="port">The RabbitMQ port. Default is 5672.</param>
        /// <exception cref="ArgumentException">Thrown when any of the required parameters are null or empty, or when the port is not a positive integer.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the secrets configuration is invalid.</exception>
        public RabbitMQSender(string hostname, string queueName, string exchangeName, int port = 5672)
        {
            if (string.IsNullOrEmpty(hostname)) throw new ArgumentException("Hostname cannot be null or empty", nameof(hostname));
            if (string.IsNullOrEmpty(queueName)) throw new ArgumentException("Queue name cannot be null or empty", nameof(queueName));
            if (string.IsNullOrEmpty(exchangeName)) throw new ArgumentException("Exchange name cannot be null or empty", nameof(exchangeName));
            if (port <= 0) throw new ArgumentException("Port must be a positive integer", nameof(port));

            _hostname = hostname;
            _queueName = queueName;
            _exchangeName = exchangeName;
            _port = port;

            // Read credentials from secrets.json
            var secrets = JsonSerializer.Deserialize<Secrets>(File.ReadAllText("secrets.json"));
            if (secrets == null || secrets.RabbitMQ == null)
            {
                throw new InvalidOperationException("Invalid secrets configuration.");
            }
            _username = secrets.RabbitMQ.Username ?? throw new InvalidOperationException("Username cannot be null.");
            _password = secrets.RabbitMQ.Password ?? throw new InvalidOperationException("Password cannot be null.");
        }

        /// <summary>
        /// Sends a message to the specified RabbitMQ queue.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <exception cref="ArgumentException">Thrown when the message is null or empty.</exception>
        /// <exception cref="Exception">Thrown when an error occurs while sending the message.</exception>
        public void SendMessage(string message)
        {
            if (string.IsNullOrEmpty(message)) throw new ArgumentException("Message cannot be null or empty", nameof(message));

            try
            {
                var factory = new ConnectionFactory()
                {
                    HostName = _hostname,
                    Port = _port,
                    UserName = _username,
                    Password = _password
                };
                using (var connection = factory.CreateConnection())
                using (var channel = connection.CreateModel())
                {
                    // Declare the exchange as a direct exchange
                    channel.ExchangeDeclare(exchange: _exchangeName, type: ExchangeType.Direct);

                    // Declare the queue
                    channel.QueueDeclare(queue: _queueName,
                                         durable: false,
                                         exclusive: false,
                                         autoDelete: false,
                                         arguments: null);

                    // Bind the queue to the exchange
                    channel.QueueBind(queue: _queueName, exchange: _exchangeName, routingKey: _queueName);

                    var body = Encoding.UTF8.GetBytes(message);

                    // Publish the message to the exchange
                    channel.BasicPublish(exchange: _exchangeName,
                                         routingKey: _queueName,
                                         basicProperties: null,
                                         body: body);
                    Console.WriteLine(" [x] Sent {0}", message);
                }
            }
            catch (Exception ex)
            {
                // Log the exception (you can replace this with your logging mechanism)
                Console.WriteLine($"Error sending message: {ex.Message}");
                // Re-throw the exception to ensure the application does not continue with invalid data
                throw;
            }
        }

        /// <summary>
        /// Represents the secrets configuration for RabbitMQ.
        /// </summary>
        private class Secrets
        {
            /// <summary>
            /// Gets or sets the RabbitMQ credentials.
            /// </summary>
            public RabbitMQCredentials RabbitMQ { get; set; } = null!;
        }

        /// <summary>
        /// Represents the RabbitMQ credentials.
        /// </summary>
        private class RabbitMQCredentials
        {
            /// <summary>
            /// Gets or sets the RabbitMQ username.
            /// </summary>
            public string Username { get; set; } = null!;

            /// <summary>
            /// Gets or sets the RabbitMQ password.
            /// </summary>
            public string Password { get; set; } = null!;
        }
    }
}