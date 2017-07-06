using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.Azure.Devices.Client;
using System.Net.Http.Formatting;

namespace SimulatedDevice
{
    class Program
    {
        static DeviceClient deviceClient;
        /*
        static string iotHubUri = "PAworkshop.azure-devices.net";
        static string deviceKey = "7cccfTV3dxbQwfUFqAdsOlkQAixm+KOPLNhNngJiJ38=";
        */
        static string iotHubUri = "artciottest.azure-devices.net";
        static string deviceKey = "e/02hnwdz2BmNe1iLM+0oOUrNG/Z0doGRx1iBnG3lFs=";

        static void Main(string[] args)
        {
            Console.WriteLine("Simulated device\n");
            deviceClient = DeviceClient.Create(iotHubUri, new DeviceAuthenticationWithRegistrySymmetricKey("firstSimulator", deviceKey), TransportType.Http1);

            SendDeviceToCloudMessagesAsync();
            Console.ReadLine();
        }

        private static async void SendDeviceToCloudMessagesAsync()
        {
            double minTemperature = 25; //change here to illustrate the danger
            double minHumidity = 63;
            int messageId = 1;
            Random rand = new Random(234);
            int messageCount = 0;

            while (messageCount < 300)
            {
                double currentTemperature = minTemperature + rand.NextDouble() * 15;
                double currentHumidity = minHumidity + rand.NextDouble() * 20;

                var telemetryDataPoint = new
                {
                    messageId = messageId++,
                    deviceId = "firstSimulator",
                    temperature = currentTemperature,
                    humidity = currentHumidity
                };
                var messageString = JsonConvert.SerializeObject(telemetryDataPoint);
                var message = new Message(Encoding.ASCII.GetBytes(messageString));
                message.Properties.Add("temperatureAlert", (currentTemperature > 30) ? "true" : "false");

                await deviceClient.SendEventAsync(message);
                Console.WriteLine("{0} > Sending message: {1}", DateTime.Now, messageString);

                messageCount++;
                await Task.Delay(500);
            }
        }
    }
}
