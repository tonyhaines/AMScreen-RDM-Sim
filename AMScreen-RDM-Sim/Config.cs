namespace AMScreenRDM
{
    /// <summary>
    /// Represents the configuration for the application.
    /// </summary>
    public class Config
    {
        /// <summary>
        /// Gets or sets the RabbitMQ configuration.
        /// </summary>
        public required RabbitMQConfig RabbitMQ { get; set; }
    }

    /// <summary>
    /// Represents the RabbitMQ configuration.
    /// </summary>
    public class RabbitMQConfig
    {
        /// <summary>
        /// Gets or sets the RabbitMQ hostname.
        /// </summary>
        public required string Hostname { get; set; }

        /// <summary>
        /// Gets or sets the RabbitMQ queue name.
        /// </summary>
        public required string QueueName { get; set; }

        /// <summary>
        /// Gets or sets the RabbitMQ exchange name.
        /// </summary>
        public required string ExchangeName { get; set; }

        /// <summary>
        /// Gets or sets the RabbitMQ port.
        /// </summary>
        public int Port { get; set; }
    }
}