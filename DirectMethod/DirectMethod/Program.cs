using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;

namespace DirectMethod
{
    class Program
    {
        //replace the placeholder once you created iot hub
        static DeviceClient deviceClient;
        static string iotHubUri = "<iot hub>";
        static string deviceKey = "<deviceKey>";
        static string deviceId = "<deviceID>";

        static Task<MethodResponse> startSimulator(MethodRequest methodRequest, object userContext)
        {
            Console.WriteLine("\nReturning response for method {0}", methodRequest.Name);            
            string result = "'started'";
            SendDeviceToCloudMessagesAsync();
            return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(result), 200));
        }

        static Task<MethodResponse> stopSimulator(MethodRequest methodRequest, object userContext)
        {
            Console.WriteLine("\nReturning response for method {0}", methodRequest.Name);
            string result = "'stopped'";
            endApp();
            return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(result), 200));
        }

        private static async void endApp()
        {
            await Task.Delay(50);
            Console.WriteLine("Stopping...");
            Environment.Exit(-1);
        }

        private static async void SendDeviceToCloudMessagesAsync()
        {
            double minTemperature = 25; //change here to illustrate the danger
            double minHumidity = 63;
            int messageId = 1;
            Random rand = new Random(234);

            while (true)
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

                await deviceClient.SendEventAsync(message);
                Console.WriteLine("{0} > Sending message: {1}", DateTime.Now, messageString);

                await Task.Delay(1000);
            }
        }

        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("Connecting to iot hub...");
                deviceClient = DeviceClient.Create(iotHubUri, new DeviceAuthenticationWithRegistrySymmetricKey(deviceId, deviceKey), Microsoft.Azure.Devices.Client.TransportType.Mqtt);
                // setup callback for direct method
                deviceClient.SetMethodHandlerAsync("start", startSimulator, null).Wait();
                deviceClient.SetMethodHandlerAsync("end", stopSimulator, null).Wait();
                Console.WriteLine("Waiting for direct method call.");
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("Error in sample: {0}", ex.Message);
            }
        }
    }
}
