using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Messaging;
using System.Linq;

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
        static async Task Main(string[] args)
        {
            // Read configuration from /home/user/Development/AMScreen-RDM-config/config.json
            var configPath = Path.Combine("/home", "user", "Development", "AMScreen-RDM-config", "config.json");
            var config = JsonSerializer.Deserialize<Config>(File.ReadAllText(configPath));
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

                // Load data from arrays.js
                var jsonData = File.ReadAllText("/home/user/Development/AMScreen-RDM-Sim/AMScreen-RDM-Sim/arrays.json");
                var jsonContent = jsonData.Replace("module.exports = ", "").TrimEnd(';');
                var data = JsonSerializer.Deserialize<JsonElement>(jsonContent);
                if (data.ValueKind == JsonValueKind.Undefined || data.ValueKind == JsonValueKind.Null)
                {
                    Console.WriteLine("Failed to load data from arrays.js.");
                    return;
                }

                // Loop to send messages ensuring each name has one RAISE and one CLEAR
                for (int i = 0; i < data.GetProperty("names").GetArrayLength(); i++)
                {
                    foreach (var sensorState in data.GetProperty("sensorStates").EnumerateArray())
                    {
                        if (sensorState.ValueKind == JsonValueKind.Undefined || sensorState.ValueKind == JsonValueKind.Null) continue;

                        var siteCode = data.GetProperty("siteCodes")[i].GetString();
                        var thirdPartyCmsID = data.GetProperty("thirdPartyCmsIDs")[i].GetString();
                        var signSerialNumber = data.GetProperty("signSerialNumbers")[i].GetString();
                        var siteAddressLine1 = data.GetProperty("siteAddressLine1s")[i].GetString();
                        var siteAddressPostcode = data.GetProperty("siteAddressPostcodes")[i].GetString();
                        var landlordName = data.GetProperty("landlordNames")[i].GetString();
                        var networkOwnerName = data.GetProperty("networkOwnerNames")[i].GetString();
                        var name = data.GetProperty("names")[i].GetString();
                        var notificationType = data.GetProperty("notificationTypes")[i % data.GetProperty("notificationTypes").GetArrayLength()].GetString();

                        if (siteCode == null || thirdPartyCmsID == null || signSerialNumber == null || siteAddressLine1 == null ||
                            siteAddressPostcode == null || landlordName == null || networkOwnerName == null || name == null || notificationType == null)
                        {
                            Console.WriteLine("One or more required properties are null.");
                            continue;
                        }

                        var sensorStateString = sensorState.GetString();
                        if (sensorStateString == null)
                        {
                            Console.WriteLine("Sensor state is null.");
                            continue;
                        }

                        string formattedJsonData = new JsonDataFormatter().FormatToJson(
                            sensorStateString,
                            1, // networkOwner
                            2, // landlord
                            3, // site
                            4, // sign
                            siteCode,
                            thirdPartyCmsID,
                            signSerialNumber,
                            siteAddressLine1,
                            siteAddressPostcode,
                            landlordName,
                            networkOwnerName,
                            "Type", // type
                            "Category", // category
                            name,
                            DateTime.Now.ToString("o"), // raiseTime
                            "Exception Description", // exceptionDescription
                            1, // exceptionTypeID
                            notificationType
                        );

                        sender.SendMessage(formattedJsonData);
                        Console.WriteLine("Message sent: " + formattedJsonData);
                        await Task.Delay(10000); // Wait for 1 second
                    }
                }
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
