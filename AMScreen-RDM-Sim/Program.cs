using System;
using System.IO;
using System.Text.Json;
using Messaging;

namespace AMScreenRDM
{
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    class Program
    {
        /// <summary>
        /// The main method of the application.
        /// </summary>
        /// <param name="args">The command-line arguments.</param>
        static void Main(string[] args)
        {
            // Read configuration from config.json
            var config = JsonSerializer.Deserialize<Config>(File.ReadAllText("config.json"));
            if (config == null || config.RabbitMQ == null)
            {
                Console.WriteLine("Invalid configuration.");
                return;
            }

            var rabbitMQConfig = config.RabbitMQ;

            string hostname = rabbitMQConfig.Hostname;
            string queueName = rabbitMQConfig.QueueName;
            string exchangeName = rabbitMQConfig.ExchangeName;
            int port = rabbitMQConfig.Port;

            try
            {
                if (string.IsNullOrEmpty(hostname)) throw new ArgumentException("Hostname cannot be null or empty", nameof(hostname));
                if (string.IsNullOrEmpty(queueName)) throw new ArgumentException("Queue name cannot be null or empty", nameof(queueName));
                if (string.IsNullOrEmpty(exchangeName)) throw new ArgumentException("Exchange name cannot be null or empty", nameof(exchangeName));
                if (port <= 0) throw new ArgumentException("Port must be a positive integer", nameof(port));

                RabbitMQSender sender = new RabbitMQSender(hostname, queueName, exchangeName, port);

                // Dummy information with UK address
                int networkOwner = 1;
                int landlord = 2;
                int site = 3;
                int sign = 4;
                string siteCode = "ABC";
                string thirdPartyCmsID = "XYZ";
                string signSerialNumber = "123";
                string siteAddressLine1 = "221B Baker Street";
                string siteAddressPostcode = "NW1 6XE";
                string landlordName = "John Doe";
                string networkOwnerName = "Network Owner";
                string type = "Type";
                string category = "Category";
                string name = "Name";
                string raiseTime = DateTime.Now.ToString("o");
                string exceptionDescription = "Exception Description";
                int exceptionTypeID = 1;

                // Format to JSON
                JsonDataFormatter formatter = new JsonDataFormatter();
                string jsonData = formatter.FormatToJson(
                    networkOwner,
                    landlord,
                    site,
                    sign,
                    siteCode,
                    thirdPartyCmsID,
                    signSerialNumber,
                    siteAddressLine1,
                    siteAddressPostcode,
                    landlordName,
                    networkOwnerName,
                    type,
                    category,
                    name,
                    raiseTime,
                    exceptionDescription,
                    exceptionTypeID);

                // Send the JSON string as a message
                sender.SendMessage(jsonData);

                Console.WriteLine("Message sent: " + jsonData);
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"Parameter error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }
    }
}