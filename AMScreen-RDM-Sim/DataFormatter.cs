using System;
using System.Text.Json;

namespace Messaging
{
    /// <summary>
    /// This class is responsible for formatting data to JSON.
    /// </summary>
    public class JsonDataFormatter
    {
        /// <summary>
        /// Formats the provided data into a JSON string.
        /// </summary>
        /// <param name="networkOwner">The network owner ID.</param>
        /// <param name="landlord">The landlord ID.</param>
        /// <param name="site">The site ID.</param>
        /// <param name="sign">The sign ID.</param>
        /// <param name="siteCode">The site code.</param>
        /// <param name="thirdPartyCmsID">The third-party CMS ID.</param>
        /// <param name="signSerialNumber">The sign serial number.</param>
        /// <param name="siteAddressLine1">The site address line 1.</param>
        /// <param name="siteAddressPostcode">The site address postcode.</param>
        /// <param name="landlordName">The landlord name.</param>
        /// <param name="networkOwnerName">The network owner name.</param>
        /// <param name="type">The type of the message.</param>
        /// <param name="category">The category of the message.</param>
        /// <param name="name">The name associated with the message.</param>
        /// <param name="raiseTime">The time the message was raised.</param>
        /// <param name="exceptionDescription">The description of the exception.</param>
        /// <param name="exceptionTypeID">The exception type ID.</param>
        /// <param name="notificationType">The type of notification (warning or alarm).</param>
        /// <returns>A JSON string representing the formatted data.</returns>
        /// <exception cref="ArgumentException">Thrown when any of the required parameters are null, empty, or invalid.</exception>
        /// <exception cref="Exception">Thrown when an error occurs during JSON serialization.</exception>
        public string FormatToJson(
            string sensorState,
            int networkOwner,
            int landlord,
            int site,
            int sign,
            string siteCode,
            string thirdPartyCmsID,
            string signSerialNumber,
            string siteAddressLine1,
            string siteAddressPostcode,
            string landlordName,
            string networkOwnerName,
            string type,
            string category,
            string name,
            string raiseTime,
            string exceptionDescription,
            int exceptionTypeID,
            string notificationType)
        {
            // Parameter checking for strings
            if (string.IsNullOrEmpty(sensorState)) throw new ArgumentException("Sensor state cannot be null or empty", nameof(sensorState));
            if (string.IsNullOrEmpty(siteCode)) throw new ArgumentException("Site code cannot be null or empty", nameof(siteCode));
            if (string.IsNullOrEmpty(thirdPartyCmsID)) throw new ArgumentException("Third party CMS ID cannot be null or empty", nameof(thirdPartyCmsID));
            if (string.IsNullOrEmpty(signSerialNumber)) throw new ArgumentException("Sign serial number cannot be null or empty", nameof(signSerialNumber));
            if (string.IsNullOrEmpty(siteAddressLine1)) throw new ArgumentException("Site address line 1 cannot be null or empty", nameof(siteAddressLine1));
            if (string.IsNullOrEmpty(siteAddressPostcode)) throw new ArgumentException("Site address postcode cannot be null or empty", nameof(siteAddressPostcode));
            if (string.IsNullOrEmpty(landlordName)) throw new ArgumentException("Landlord name cannot be null or empty", nameof(landlordName));
            if (string.IsNullOrEmpty(networkOwnerName)) throw new ArgumentException("Network owner name cannot be null or empty", nameof(networkOwnerName));
            if (string.IsNullOrEmpty(type)) throw new ArgumentException("Type cannot be null or empty", nameof(type));
            if (string.IsNullOrEmpty(category)) throw new ArgumentException("Category cannot be null or empty", nameof(category));
            if (string.IsNullOrEmpty(name)) throw new ArgumentException("Name cannot be null or empty", nameof(name));
            if (string.IsNullOrEmpty(raiseTime)) throw new ArgumentException("Raise time cannot be null or empty", nameof(raiseTime));
            if (string.IsNullOrEmpty(exceptionDescription)) throw new ArgumentException("Exception description cannot be null or empty", nameof(exceptionDescription));
            if (string.IsNullOrEmpty(notificationType)) throw new ArgumentException("Notification type cannot be null or empty", nameof(notificationType));

            // Parameter checking for integers
            if (networkOwner < 0) throw new ArgumentException("Network owner cannot be negative", nameof(networkOwner));
            if (landlord < 0) throw new ArgumentException("Landlord cannot be negative", nameof(landlord));
            if (site < 0) throw new ArgumentException("Site cannot be negative", nameof(site));
            if (sign < 0) throw new ArgumentException("Sign cannot be negative", nameof(sign));
            if (exceptionTypeID < 0) throw new ArgumentException("Exception type ID cannot be negative", nameof(exceptionTypeID));

            try
            {
                var data = new
                {
                    SensorState = sensorState,
                    NetworkOwner = networkOwner,
                    Landlord = landlord,
                    Site = site,
                    Sign = sign,
                    SiteCode = siteCode,
                    ThirdPartyCmsID = thirdPartyCmsID,
                    SignSerialNumber = signSerialNumber,
                    SiteAddressLine1 = siteAddressLine1,
                    SiteAddressPostcode = siteAddressPostcode,
                    LandlordName = landlordName,
                    NetworkOwnerName = networkOwnerName,
                    Type = type,
                    Category = category,
                    Name = name,
                    RaiseTime = raiseTime,
                    ExceptionDescription = exceptionDescription,
                    ExceptionTypeID = exceptionTypeID,
                    NotificationType = notificationType
                };

                return JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            }
            catch (Exception ex)
            {
                // Log the exception (you can replace this with your logging mechanism)
                Console.WriteLine($"Error formatting to JSON: {ex.Message}");
                // Re-throw the exception to ensure the application does not continue with invalid data
                throw;
            }
        }
    }
}