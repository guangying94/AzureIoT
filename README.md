# Azure IoT Suite Hands-on Workshop

## Agenda

#### 1. Overview of solution architecture
#### 2. Preparation / Software needed
#### 3. Create simulator
#### 4. IoT Hub
#### 5. Azure Stream Analytics
#### 6. Azure Storage
#### 7. Azure Event Hub
#### 8. Azure Machine Learning
#### 9. Azure Function
#### 10. Power BI
#### 11. Others

The following discussion is based on normal IoT lambda architecture, and this tutorial is served as hands-on familiarization on various Azure services.

### 1. Overview of solution architecture

The diagram below shows a typical IoT solution reference architecture.

![Reference architecture](https://izcdxq-dm2306.files.1drv.com/y4mkIH4Zf9IxzEIXbisAPpP0Sz5uuQEPqJxuUoREGhPVhrsWrsrtgNj560-3f5c5sXm-kK80hHQFjrqVCrHrKlkFsYQ53FhUUVYVSycamjbTp-E0maW7JONBljar8jk7DwXzEu-I6A36i4LvWzozZD6ksPKRO0Fuc1ClFY6ZIeKCaUdX-X4gsMGZfdqXlfmyYNphAdiFyxabg9LMXCw-fGHhQ?width=1267&height=516&cropmode=none)


Let's map out the respective component with respective Azure services.

![Overview of solution architecture](https://jwm75w-dm2306.files.1drv.com/y4mSiTxWs3bFc30dNM4hZGUTZm7HL2CFpYVGFhENo9RLi9YYdB59n5eK2Fh4d_SSXuIp20xWNQXSY1W4z8tuSvxKQBmGBOzPzhlSJDmDtzyZeHA489khprkC8Kn2oPdqZzdGo0n0sePRRZB5b8nkSndo22JqR60JjBpz2GlJe8THIn5MzIBXtWfhlup2XpEpRybt__GRkdKUkIMFocZZFBPpA?width=1024&height=626&cropmode=none)


We will start from building a simulator that sends random temperature and humidity readings into **IoT Hub**. Then, the data will pass thru **Azure Stream Analytics**, which allow us to store the data in **Azure Table Storage**, and at the same time, computer the average of reading value in a 10 seconds windows.

Another output from **Azure Stream Analytics** is send the data to **Azure Event Hub**, which act as a data receiver. **Azure Event Hub** allows high speed data ingestion with low latency. From **Azure Stream Analytics**, we have computed the average value of readings, and will send a message to **Azure Event Hub** every 10 seconds. Once **Azure Event Hub** receive a message, it will trigger **Azure Function**, which help us send the readings to our **Azure Machine Learning Service** to predict the status. 

Concurrently, once the machine learning module predicted the alert, **Azure Function** moves the predicted results into **Azure Table Storage** for web app. The web app can be built using any language such as Java, or .Net. For this workshop purpose, we are using **Power BI** dashboard as our dashboard. Two tiles are pinned, which show the live streaming data of temperature and humidity. Then, a textbox is pinned to indicate the status on machine. 
 
Noted that this solution architecture is for reference / introductory purpose, there are many different services that the team can leverage on, based on the requirement. 

### 2. Preparation / Software needed

We are leveraging the power of cloud to perform most of the tasks, nevertheless, we need code editor to create simulator, and some light code editing that will be inserted into Azure Services.

The following application will be helpful on setting up the development environment.
1. [**Visual Studio 2017**](https://www.visualstudio.com/downloads/)
1. [**Microsoft Azure Storage Explorer**](http://storageexplorer.com/)
1. [**Device Explorer for IoT Hub devices**](https://github.com/Azure/azure-iot-sdk-csharp/releases)

To complete the exercise, you will need an Azure account. You can sign up for a trial account easily here: [Create Azure account](https://azure.microsoft.com/en-us/free/). 

In this tutorial, we will be using C# as the main development language. The whole idea of this tutorial is to assist the team on understand the concept of various Azure services. Once the understanding is established, the team can use any supported language for development.

During the process of installation, please choose C# or .Net to install the components. 

### 3. Create Simulator

First, we will create a simple simulator that generate temperature and humidity readings. To do this, launch Visual Studio 2017, and create a new project by navigating to **File** -> **New** -> **Project...**. Under **Visual C#**, navigate to _Windows CLassic Desktop_ and choose **Console App (.Net Framework)**. Rename your project and choose the location of where the file should be stored.

![Simulator setup](https://vavfqw-dm2306.files.1drv.com/y4mh95G2b8TVd2bOYLqcFfPUaDQ3g3MuaTgCEt7vUdjuzdAj9Kmj-KhYl_yGV5iyCTx6BcGHf3wMCcIgAASioT698WDq5OhMUiS74-1n48M_h03SLWFyu5LhnQq--wPkkZn0oG2XoIAjGABstgYd4mP_BvvCLjfS3jEVuI7qsCPb80k9vabxuuGQCbSPv-axvchnurehD5Ikg_vqb5ELubvCA?width=1024&height=576&cropmode=none)

#### 3.1 Manage NuGet Package
Once you created the project, on the top navigation bar, click **Tools** -> **NuGet Package Manager** -> **Manage Nuget Packages for solution...**. Then select **Browse**, search for **Microsoft.Azure.Devices.Client**, and select install to install the package. This procedure downloads, installs and adds a reference to the _Azure IoT Service SDK_ NuGet package and its dependencies.

![Simulator setup 2](https://v4bsog-dm2306.files.1drv.com/y4muW_Xhf-YWIZgvHbxvm81ZklGxsVxSvi7v-QwcEgiRvhW3zawZ20Gy5xnm73jCZAsKj7oysmJ0hSnt2VIO1TqD7JLVD_Vduf-2hCnEfudfF3gnfNkVdObhZwe-D27YS0nkpGzxXwcfAt8NxlmCAVFpkNG7bnDNnd_iKeqvrmcB3wWUL9xC2Bwy8BhwBdwmUr2TD5GzQVuSen5cBAjRsAD7w?width=1440&height=810&cropmode=none)

#### 3.2 Code
##### Library
Add the following _using_ statement at the top of the **Program.cs** file:

```csharp
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
```
##### IoT Device Identity
Then, add the following fields to the **Program** class. Note that there are 2 placeholders, and we will replace it later once we created **Azure IoT Hub**. 

```csharp
static DeviceClient deviceClient;
static string intHubUri = "{iot hub hostname}";
static string deviceKey = "{device key}";
```

##### Methods
Add the following methodto the **Program** class.

This method will generate random _temperature_ and _humidity_ readings, and then send it over to Azure IoT Hub using the connection string that we established in previous section. 

```csharp
private static async void SendDeviceToCloudMessagesAsync()
{
    double minTemperature = 25;
    double minHumidity = 63;
    int messageId = 1;
    Random rand = new Random(234); //you can change the random seed by change the number
    int messageCount = 0; //we will send a fixed number of message. Alternatively, this can be removed.

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
        await Task.Delay(500); //the message is sent every 500ms
    }
}
```

This method sends a new device-to-cloud message every 0.5 second. The message contains a JSON-serialized object, with the device ID and randomly generated numbers to simulate a temperature sensor and a humidity sensor.

##### Main Method
Lastly, add the following lines to the **Main** method:

```csharp
Console.WriteLine("Simulated device\n");
deviceClient = DeviceClient.Create(iotHubUri, new DeviceAuthenticationWithRegistrySymmetricKey("myFirstDevice", deviceKey), TransportType.Mqtt);

SendDeviceToCloudMessagesAsync();
Console.ReadLine();
```

Note that when we establish connection with **Azure IoT Hub**, we are using _MQTT_ protocol, other supported protocol includes _HTTP_ and _AMQP_.

Note that if you want to use _HTTP_ protocol, you should also add the **Microsoft.AspNet.WebApi.Client** NuGet Package to the project and include the **System.Net.Http.Formatting** namespace.

### 4. Azure IoT Hub

Now, let us look at device connectivity, which is the most important part for the whole solution. When we look at _Device Connectivity_, there are several ways where we can connect the device to Azure.

![Image](https://hij2ug-dm2306.files.1drv.com/y4mBiQJfrs0nZMP7p6u6_udkkc7MMoUPdQIxFf_qP-zpDOiGEPol43lZaBWpJv1LV794XSxmG6n0q1XZrpkJptpQpcWqgqma5g3Cbaoe1MSfQwxvxec0MS0yRQiOiTpt6sS7_jCOP8XmnbaH2iPhie5wTsBOtFMrVsN912lFQk4lkAvL2WFBLjBvRfcALcAJAlI4al1p8QtL5eCTBE5nYOHuA?width=580&height=600&cropmode=none)

Let's take a closer look on all 4 different connection.
##### 1. Direct Device Connection
The device or IoT devices has internet connection and is possible to connect to Azure directly. Devices such as Raspberry Pi, Intel Edisson, or other enterprise grade devices are capable of doing this.

##### 2. Field Gateway
If the environment requires a custom field gateway, then the devices can connect tp the field gateway directly, and from gateway, the system will process the data and send it over to Azure.

##### 3. Custom Cloud Gateway
This is similar to method 1, but it will go thru another cloud gateway instead of sending telemtry to Azure directly. 

##### 4. Field Gateway + Custom Cloud Gateway
This is combination of method 1 and 2, and the connection between field gateway and custom cloud gateway is established via VPN.

Azure IoT Hub act as a cloud gateway as pointed in the diagram above. Now, let us replace cloud gateway with Azure IoT Hub and include the data path, together with available connection protocol.

![Image](https://udkndq-dm2306.files.1drv.com/y4m0PT-mIg0YE3n9EGALP3plykRDp13efulqXKubQACtfxCdaLGh587ZiDctuxwhZTcVOYmVbTWfE20uKnzKhkACgBISA4ZemOTpkFovlbQ66PrwLLVB9syLctV5d6JTM4bgYUJ9W9zhtptSJxaLuLFwDlbsQqlf-kBFRk2iUl4xwiCiDiscXlt8-8Zg609a0WqZxtA9mK6LHHcfeZTOHApsA?width=947&height=662&cropmode=none)

##### Why Azure IoT Hub?
1. **Designed for IoT**
    1. Connect up to 10 million devices
    
1. **Cloud-scale messaging**
    1. Device-to-cloud and Cloud-to-device
    1. Durable messages (at least once semantics)
    
1. **Per-device authentication**
    1. Individual device identities and credentials
    
1. **Multi-protocol support**
    1. Natively supports AMQP, HTTP, MQTT
    1. Designed for extensibility to custom protocols
    
1. **Service assisted communications**
    1. Secure bi-directional communication
    1. Command and control
    
1. **Cloud-facing telemetry ingestion**
    1. Delivery receipts, expired messages
    1. Device communication errors
    
1. **Connection multiplexing**
    1. Single device-cloud connection for all communications (C2D, D2C)
    
1. **Multi-platform**
    1. Device SDKs available for multiple platforms
    1. Multi-platform Service SDK
    
#### Setup an Azure IoT Hub
Create an Azure IoT Hub is straightforward. Login to Azure Portal, then on the left panel, choose "**+**" and search for "**iot hub**". Then you will see **IoT Hub**. Click _create_ and you are prompt to enter several parameters.

![image](https://663ofw-dm2306.files.1drv.com/y4mcCbCd11dFwE6W7rYxzu0I5PgX9PtzyaWO9T8pdyEK5Ebta8SFHae4QYlGCJYoOaW6KYKlFjLmzUiptb3s4oR3NL0ujdySc_yXfIyuaZGsv5-7j0hshcFiFq0Pld6SyPYdpStqTLq0f6L3LGBCIUhRpaozqYTHB431X3ctZza9K2ZxRBn03FqvnGvv6V_v8eUDG4p8QD7zsSnx6rxqpGEvw?width=1440&height=810&cropmode=none)

![image](https://gldylw-dm2306.files.1drv.com/y4monjl9A2Gix-nDzqXNjIMibGfovZis29uyGJXjmuaiy4HCTC6ldHM7xRRyz7TUigcMK0wEsWIVdbJxptZETiVjSQTPjC30Q9g0TYLCDfmdfKAtQvHaXgLD80WpbWXGxyRJFtaM8iWgwA0QGUZgwOqoRL6m7AyGiJcYAZoZCWi23mAFYqujOvOLhI-yo2EzRHgXZetn6i2PDrEAGSbGD9iTQ?width=1440&height=810&cropmode=none)

**Name** is the name of your **Azure IoT Hub**. You can click on **Pricing** to understand the difference between each plan. Essentially, different plan cater for different number of devices and daily messages limit. 

**Resource Group** is the place where you place all project-related Azure Services under same folder for easier navigation. We chose _Southeast Asia_ in **Location**, which indicate we want to spin the services in Singapore datacenter. 

This is how it's look like once you have provisioned **Azure IoT Hub**.

![Image](https://tcrq0g-dm2306.files.1drv.com/y4m8GRn0i1FHcQdBf92xeqz5TAAPhieCmwkqi-WnZ-3tS0SGWVpgy5BT7ymd4vrSz4K9rEqTXUn71BWYh8Ay_7dCJ6ggjDxa8prTtPEJ7VAtMcIo5eqeAGS35JtiV2LG-_0Z9XFSWSo9vKtdaqWqH7LjBe0Ws8_prGQKoYAuDvlEou7DBYO7K5LfuLOoowk5G7DiiN54XETbOWJoR13rUX45Q?width=1440&height=810&cropmode=none)

#### Create IoT Devices
First, navigate to **Shared access policy**, you can see the list of policy name and what's the rights associated with each policy. Click on _iothubowner_, and copy the **connection string - Primary Key**.

Now, launch **Device Explorer for IoT Hub Devices**. Under the "Configutation" tab, paste the _IoT Hub Connection String_ that we copied just now, and click "Update". Device explorer helps us to manage the devices and monitor the message between devices and **Azure IoT Hub**. 

To create a device, go to "Management" tab, click "Create". Enter the device ID and copy the device Key. Once we created the IoT Hub, head back to our simulator app, replace the placeholder with the information.

![Create IoT Hub Device](https://8vqsrq-dm2306.files.1drv.com/y4mv0SNHDuD8F563W5lDm-_lHhIM8LpdHQvYP4wQaa70R5k9ju3vWnRW86xIaqHmAhR7eKXgR1AkmcBxcXl_8FtHxWUQ9wQP1WJS90EpewZv7XvesFitONxIeSGUpdZqvegeGi7MlJOxFpo1qjHqf-IImi_uLXbXXouoeQ8MypT4YYXygC_odKQC7dYLfEhVn2eDDXIi_bkj0DiEh4cdz74pQ?width=785&height=678&cropmode=none)

The code should looks something like this:

```csharp
static string iotHubUri = "PAworkshop.azure-devices.net";
static string deviceKey = "7cccfTV3dxbQwfUFqAdsOlkQAixm+KOPLNhNngJiJ38=";
```

Also, in the main method, ensure that the deviceID is the same as the deviceID you have just created. In my case, my deviceID is _firstSimulator_.

```csharp
deviceClient = DeviceClient.Create(iotHubUri, new DeviceAuthenticationWithRegistrySymmetricKey("firstSimulator", deviceKey), TransportType.Http1);
```

Once you have entered the placeholder, you can run the simulator and see what message is generated. To check how the message is sent to IoT Hub, head back to **Device Explorer for IoT Hub Devices**, choose the device that you have created, and click **Monitor**. Under the "Event Hub Data", you can see how the message is sent to **Azure IoT Hub**. The message is in the form of JSON. 

![Create IoT Hub](https://jwm65w-dm2306.files.1drv.com/y4me-mvyk5o8yxJqGVuQ_ek5p2DklZJL8FqtKT0OUJZ84IPgO2-Di2cF72qkkDRQkA3S7naeS0v-mnj75E0MUyzrD0huVHzQWUP6pXI50FIxq_tjW_x3mL_ZPhZnIjm_4ICX6zfYbxXAgcLxWDRj7dqllpghexeLEqtIM25a-QtvEk08mCh2b_OWLs2MrRpmYIdXcqlWb_8Z8BKguZXCc1ZUA?width=785&height=678&cropmode=none)

This conclude **Azure IoT Hub**. You can find more information here: [**Azure IoT Hub Documentation**](https://docs.microsoft.com/en-us/azure/iot-hub/).

### 5. Azure Stream Analytics

With the events/telemetry sending to Azure via **Azure IoT Hub**, now let's look at the second component of the architecture, which is _Event Processing_.

**Azure Stream Analytics** offers the processing, analytics and handling of massive ammounts of real time data. Besides that, **Azure Stream Analytics** can be configured to be expose for the settings of rules and alarms. **Azure Stream Analytics** then runs these rules as it processes the incoming data ingestion and flags what needs to be escalated for attention. To configure stream analytics, usrs can use simple SQL syntax to program _if-this-then-that_ style rules and instruction. 

**Azure Stream Analytics** can not only handle millions of events per second but it can also correlate across multiple independent streams of data simultaneously. This high speed event processing allows for the real time detection of anomalies or escalations vased on threshold breaches or alarm settings as the data is ingested. The architecture is simple and easily scalable to an enterprise ready solution.

To create **Azure Stream Analytics**, go to Azure portal. On the left hand panel, click "**+**", search for "_stream analytics_", and click **"Stream Analytics job"**. Click create, and fill up the name and choose the resource group. Here, you can choose the same resource group as **Azure IoT Hub**. 

![image](https://vaveqw-dm2306.files.1drv.com/y4mSnvF0vkw2kayzoIgYBBUtxfVFhjWTgufdMNgx48UpReuwCExSq41KYrJ34lmyCHggk_MFtqeEqmTvqwTHWa4LZIYqgsEXJmK2LGV9omDZMUZ6O9-A7_Ua5oHiws3oWzArcLydQdOmiJHENTggFDNUhQx2eUwWXSdTvrOzstzPbgd8K4zgvIaTQzPnwAmMN7TBAQI1gpKho2fJslyluCZ2A?width=1440&height=810&cropmode=none)

Once **Azure Stream Analytics** is created, you will see the screen as below. 3 important components of **Azure Stream Analytics** are:
1. Inputs
1. Query
1. Outputs

We will configure all 3 components to enable the streaming job.

![image](https://hij1ug-dm2306.files.1drv.com/y4ml8KtlnId56PZQhUnqauzhiYAGFs0L9zlzbKb0ncIpS4ExDPhEywATbDF-cT6hT5G35GWnQQ7lpfRC89tax95-Vwm2RSR1PfCNdfAge6rk3-mq1AquCBxkAKY123MAO00Li3EH06OFenm-arNmiCPohUEsrBv50cd2pqeZyYy173UoFxtzypziu-znV-BkikZ1txbGMn32f6z8hAndSYFHQ?width=1440&height=810&cropmode=none)

##### 5.1 Inputs
First, we configure the input of **Azure Stream Analytics**. We will use the event from **Azure IoT Hub** as our input. Click the "_Inputs_" box, on the top panel, click "**+** Add". Give the "_Input alias_" a name, and the "_Source Type_" choose "Data Stream". 

In "_Import option_", choose "Use IoT hub from current subscription", and choose the IoT hub that we just created. For "_Shared access policy name_", choose "iothubowner". Note that the "_Event serialization format_" should be "**JSON**", as the simulator sends telemetry in JSON format. Once done, click "Create".

![image](https://izccxq-dm2306.files.1drv.com/y4mRTb0o4Lc0aJPzbr9YCbNZnJChejbEnewhA9jn0QX0Yzplx6Xeh5R8_OLSCmgLouNtVLPfxvhDP_O_QLE15tNiF4dtkFXKXLo3h-8YOAQzBr9i55zChQz_THsba1AoTvFAV_4hhS-If_TwYJ7SAw3PCUJCjSZ84n6HgcPXjRfWYM5vi7o9iKVQ7KplJlGPFo9SnwMKYloDF-OsjEr4epIpg?width=1440&height=810&cropmode=none)

In this exercise, we are creating a single input. However, **Azure Stream Analytics** allows multiple inputs, and user can configure respectively. 

##### 5.2 Outputs
To configure output, the process is similar. Users create the output depends on the requirement. In this exercise, we will create an output to store every single telemetry in **Azure Table Storage**, and stream the data to **Power BI** for visualization. Finally, we will calculate the average value of the sensors in a 10-seconds windows, and send it to **Azure Event Hub** as new message, which will then be push to **Azure Machine Learning** for analysis.

###### 5.2.1 Table Storage
On the output panel, click "**+** Add", give the output a name, and under "_Sink_", choose "**Table Storage**". Choose "**Use table storage from current subscription**", and choose the storage account that we created. _Please refer to next section on how to create **Azure Storage** account._

Give the table a name, and under "_Partition key_", enter "deviceId". For "_Row key_", enter "messageId". Both "deviceId" and "messageId" are actually one of the parameter when the simulator is sending the telemetry to **Azure IoT Hub**, here we are just using them to identify which device is sending what data. 

Once done, click "Create".

![image](https://663nfw-dm2306.files.1drv.com/y4m4P2YTDZoLCSETjNzLwVcRhbPFwp_9bj9XF4N4VnBGJ2aBRnqwdMo0_aWWnqGvHHzCADN5hKh5nLolD9Y3Tinlebg41foLO6SS_JlKjC3e3kSsSulvp10AxjSEHPdwEf34qblxLR-j6QAxrRtvkk4KvMN33Vv2Pn8fWVjz4O6ovR1i1lV0nuiWpVJ3ADm-Sn8fBZ3e1QWMkKzDIF22-ozsw?width=1440&height=810&cropmode=none)

###### 5.2.2 Power BI
Similarly, now we add **Power BI** as output stream. Give the output stream a name, and choose the "_Sink_" to be "**Power BI**". Then, you will be prompt to authorize **Power BI** account. Login with your **Power BI** account. Then choose "_Group Workplace_" of your choice, and give both "_Dataset Name_" and "_Table Name_" a name for visualization purpose. 

Once done, click "Create".

![image](https://gldxlw-dm2306.files.1drv.com/y4mZygfXG6rKNGF335wKx-v_KROH27GVuYY9rQqkoRXeIG5TQqiqEO9hEZqieQqOgxdTcppN5LlfGPpAN_LYn5CV3lOVV62kHjHeV0VDpAC3vb11ZbTqRwVfOO2VXiAdqQlQ7_9Xfv5yKV6oBAW0z5a2SgrUj7nfKvKrM0LZYPtYDnaO-ZtvHBrvVs6F1qFMKztldi16WlRnSomLkVXDACkKQ?width=1440&height=810&cropmode=none)

###### 5.2.3 Event Hub
Again, we create **Azure Event Hub** as third output. Give the output a name, choose "_Event hub_" as sink and for the import option, choose "_Provide event hub settings manually_". For "_Service bus namespace_" and "_Event hub name_", please refer to section 7 on how to get the name. Enter "RootManagerSharedAccessKey" under "_Event hub policy name_", and paste the key, which can be obtained from **Azure Event Hub**. For "_partition key column_", enter "deviceId".

Make sure that **JSON** is selected under "_Event serialization format_", and choose **Line separated** under "_Format_".

Once done, click "Create".

![image](https://8qqbrq-dm2306.files.1drv.com/y4mdzJ4VKIHUAW3hQQ5dfcF2WyhZgVWLvZ0ipSQp6J9i2H8Qo7DxOI7-Q8TRP1FaH4Ub8paGj3tWIWsXy10CA1ygdQ6sDbGwmbFWVJYnYOMva9TM5g8FT_ZQAehxsZ3mGXgPoropaoNr5xDIUPhV_B28LeiajnghOzK1o1zGr6xbNga77mStVTRwOL_YIVjyffmi3j58el-vzbVDBM1IfJ7iA?width=1440&height=810&cropmode=none)


##### 5.3 Query
Now, with 3 different output stream, we will define the query and output the respective data. Click on the "**Query**" box. Let's start from stream the telemetry to **Azure Table Storage**.

First, we define the input stream. We are using SQL-like language while creating the query.

```SQL
WITH [StreamData] AS (
SELECT *
FROM
[IoTHub])
```

We define the input stream as _StreamData_, while the source is _IoTHub_, where we have define the input name previously. Now, we will select _messageId_, _deviceId_, _temperature_, and _humidity_ into the table. Insert the following code below the code that we entered previously.

```SQL
SELECT
    messageId,
    deviceId,
    temperature,
    humidity
INTO
    [TelemetryTable]
FROM
    [StreamData]
```

Now for Power BI stream, we will be using similar code, but noticed that previous SQL statement didn't include time, but we need time variable in order to visualize the telemetry data. We just need to add "_System.Timestamp time_" to indicate additional variable. 

```SQL
SELECT
    messageId,
    deviceId,
    temperature,
    humidity,
    System.Timestamp time
INTO
    [PowerBIStream]
FROM
    [StreamData]
```

Lastly, we want to calculate the average value of telemetry data in a time frame of 10 seconds, and send them as 1 message to **Azure Event Hub**. Noticed that we group the telemetry data using "_TumblingWindow_". This is window function within **Azure Stream Analytics**, check here for more information: [Azure Stream Analytics Windows Functions](https://docs.microsoft.com/en-us/azure/stream-analytics/stream-analytics-window-functions).

```SQL
SELECT
    deviceId,
    AVG(temperature) AS temperature,
    AVG(humidity) AS humidity,
    System.Timestamp time
INTO
    [TelemetrySummary]
FROM
    [StreamData]
GROUP BY
    deviceId,
    TumblingWindow(second,10)
```

Once done, click "save". To verify the query and test the connection, go back to **Azure Straem Analytics** page, and click "Start" on the top panel. Then, run the simulator app that we created earlier on. If the query is successful, we will see the telemetry in **Azure Table Storage** and **Azure Event Hub** will receive messages. 

**Azure Table Storage**
![Image](https://vqvnqw-dm2306.files.1drv.com/y4mMRtN21qSr1lIv7SndWGV1dgVslLUk2HbSlC4DmkDDNx_aQQifoIvKoRZLG9GTrjEZwfv3fDXuJ3OKYgP5oLYh4BdlP0RzAzMtIxFlyKEamQoM6iqNCiKgoedAlLe1U8AkUFnKeASD5r4zDyGngwr8PllVgjTqE4VV67PITqE4k0Ik6uZfqeiyUyWEl3QNvrrixO4n60hUD29vNWTcHG2Nw?width=1440&height=810&cropmode=none)

**Event Hub incoming messages**
![image](https://jmnd5w-dm2306.files.1drv.com/y4mVsDYmy4Nw2kxCcgpERqj-C9soTYvu78xCMhQda3Wrx5LpNQz8EYpodaNFOVzqvv133Acc1DRrA4tltxfKkWBfjvuV9wdSVfv3YguRS0h9Plh30xRlsI58MHg7rAGj3wkeaa93kJ8DuXMXASDlqL1VjD7PFLos0iirgXbnT_v9jkADdQUKP6OXAmNLPi5aMAHDDu-VWemvhL-EThCxYdaVg?width=1440&height=810&cropmode=none)

Click here to find out more about [**Azure Stream Analytics**](https://docs.microsoft.com/en-us/azure/stream-analytics/stream-analytics-introduction). You can check other [**Query examples for common stream analytics usage pattern**](https://docs.microsoft.com/en-us/azure/stream-analytics/stream-analytics-stream-analytics-query-patterns). 

### 6. Azure Storage

**Azure Table Storage** enables new scenarios for applications that requires scalable, durable, and highly available storage for their data. 

The advantages of **Azure Table Storage** are:
1. Massively scalable
1. Elastic
1. Auto-partitioning system
1. Accessible from anywhere in the world
1. Multi-platform support 

An **Azure Table Storage** provides 4 different type of services, which is 
1. Blob Storage
1. Table Storage
1. Queue Storage
1. File Storage

Find out more about **Azure Table Storage** [here](https://docs.microsoft.com/en-us/azure/storage/storage-introduction)

To create **Azure Storage**, go to Azure portal, on the left hand panel, click "**+**", search for "_azure storage_", and click **"Storage account - blob, files, table, queue"**. Click create, and fill up the information. Again, you can choose the same resource group as previous services. Note that on "_Replication_", you have several options, and choose the one that suites your requirement. 

![image](https://v4brog-dm2306.files.1drv.com/y4mkCDxYdgkcmxQCdqNZwbarp-ydbCrYqhMamQ_yFhBa-TpeECjZTJdqgqYJeaXTwizDtqo0vwCj5DLiCcP9xz0RlCK2Fc-Ivn7j0bEUQUHfDFPkr80DGxvMoXojFih_Aqh-dErfH7J8zcJJvTdCKcMJRTmWAjlTZdlAmGKpt0JqyyCwsFXY4yQ7TouUSd45mUV59i4Of2HEUSqmeOGPka1qQ?width=1440&height=810&cropmode=none)

Now, let us navigate to **Microsoft Azure Storage Explorer**. On the left hand panel, click on the second icon and login using the Azure account. Once the storage account is created, you should be able to see the list of services under the storage account.

![image](https://wib0og-dm2306.files.1drv.com/y4mvxyyqdN79-CMaI4c4dSD0jvj70f9mcNjhcRk-lCMSQF9s1YDk1rdMQQMFEXwSVLiMNIyDqsIMMzjH6BKZxWRWU9il2s8LKVuXnp-kTjGovUsfEWWdPrvMUNG6oXXGw8QFozRhcZe1Qs9RoV4wgIKCg_-V9Jk-jHXN7jCdu2D5IGC-N3qzgUNwBti-49YRHjgvtPfKKAZd6daGOlS5gUnLg?width=506&height=448&cropmode=none)

### 7. Azure Event Hub

**Azure Event Hub** is a highly scalable data streaming platform and event ingestion service capable of receiving and processing millions of events per second. **Azure Event Hubs** can process and store  events, data, or telemetry produced by distributed software and devices. Data sent to an **event hub** can be transformed and stored using any real-time analytics provider or batching/storage adapters. With the ability to provide publish-subscribe capabilities with low latency and at massive scale, **Azure Event Hubs** serves as the "on ramp" for Big Data.

To create **Azure Event Hub**, go to Azure portal, on the left hand panel, click "**+**", search for "_event hub_", and click **"Event Hubs"**. Click create, and fill up the information. Give it a name, and again, choose the same resource group. The name given here is the name for this _service bus_, the name is needed when integrating to **Azure Stream Analytics**. 

![image](https://udkmdq-dm2306.files.1drv.com/y4mL2wZ2kXSjt34VRULOXdw_2dQ-mKspuqqVOsR1gieI-V0pEqzcavvYQA9nkQjXZcERCBsGpXaols99w9Old8QTA_eVl0dT8ikT7yydq99zsn7Ij7leoIA01ehs5LloAbhJr8Sf6IquVnqsNgYG307kTXeEqAYQfUB4GEUgsvZXdbEjZDlSJkmBlm528L3f0OPLA81UHsYQWCMHwE8ROVZlw?width=1440&height=810&cropmode=none)

This step will create a service bus collection, so now we will create an Event Hub to receive the message. First, navigate to your event hub service, on the top panel, click "**+** Event Hub". 

![image](https://tcrp0g-dm2306.files.1drv.com/y4moESxa1mTTuymaztCDgQWcKZnXP7u1A5ttS94ax2zOhgQ2MqdBoBdEvvGQUekSOlJsd5hWzTijniqtyoH55dUKtiWqueRZruK6ST61mWUJbk4GDE-YDi-9CRdO-Etdy62OnDwCaBBMi6LV_FIIxFK8BPMvQA07NOBXmzh5aGnnaml1SB5IXCUDnZNzFpBe4BcwYlTDqHTRuxFx5-v9wvAvw?width=1440&height=810&cropmode=none)

Give the event hub a name, and choose configure the "_Message Retention_" and "_Archive_" if needed. Once done, click "Create". With that, we have created an **Event Hub**. 

To enable integration to **Azure Event Hub**, on the left panel, click "_Shared access policy_", then select "_RootManageSharedAccessKey_". Here, you can find the connection string to integrate **Azure Stream Analytics** into **Azure Event Hub**. 

![image](https://hyjpug-dm2306.files.1drv.com/y4m2UFfMPRgz0qHJK6f8twcPimqigQxo_jfQWXCzhdEQT5hx2aJrUEajf3NPFiOOjDeLDIfmlEIwHUuVGPH6nMMkwvhnYQMw0WU6ahHul5AWQPlAlIKR77jNgIO4cBUR5tUobJPIddOcuU10rOY0NQ5Pj4ggNG5Tc3MsM6wexaeYaTZR1pLRy9n6w3xsNHUMfqRTxuG9cMkjOyGgY64igOJJg?width=1440&height=810&cropmode=none)

When integrating with **Azure Stream Analytics**, we setup **Azure Event Hub** to be capable of receiving event. For sending event to other services, please check **Azure Function** section.

This concludes **Azure Event Hub**, to find out more, click [here](https://docs.microsoft.com/en-us/azure/event-hubs/event-hubs-what-is-event-hubs). 

### 8. Azure Machine Learning

In this section, we will look at how we setup **Azure Machine Learning** and publish it as web services.

First, navigate to [**Azure ML Studio**](https://studio.azureml.net/), and login using the Azure account. Then, click [here](https://1drv.ms/u/s!AsmpFVEoNfZ8oNBNAm1Pp_Zb4mGu-Q) to download training dataset. Then upload the training dataset in ML studio by clicking at left bottom corner "**+**" sign, choose "_Dataset_", then click "_From Local File_".

Now, let's understand the data before we start to train the model.

First, let's list out 10 rows of the dataset and plot the data point in scatter plot. We want to check the correlation between temperature and humidity with the alert. 

```python
import pandas as pd

data = pd.read_csv('https://msinternalportalborfo3vz.blob.core.windows.net/fileshare/Telemetry%20Training.csv?st=2017-06-25T17%3A57%3A00Z&se=2019-06-26T17%3A57%3A00Z&sp=rwl&sv=2015-12-11&sr=c&sig=r6lPaduQM9aRz4pMjPNkDWUpLu9ZEf59ankncWTEcqs%3D')

print(data.temperature.mean())
print(data.humidity.mean())
data.head(10)
```

![Image](https://vqvlqw-dm2306.files.1drv.com/y4mKmX9QMcbzS-ZfpF2dHT0Vc7B3upvnVjqvSSA82WQdL9B6njraQUmTaVOV-i0psw3s9PNNzWKsON4ysZZsp7whpgGUa4QVQe-pL9627J_hZhv5J4euqLAQiSeDpdwPh99adV8PV3-K7pTdO5vuZAOC9rEVzykpApacwMf6iD2xk4kF_e8ub0OGR4iOamh8rALs8azc105n-kxSjdDsqugAA?width=743&height=374&cropmode=none)

From the scatter plot, we can see that "red" or "warning" is concentrated on top right corner, which indicate that when temperature and humidity is high, the alert will become "warning".

```python
import pandas as pd
import numpy as np
import matplotlib.pyplot as plt
import plotly.plotly as py

data = pd.read_csv('https://msinternalportalborfo3vz.blob.core.windows.net/fileshare/Telemetry%20Training.csv?st=2017-06-25T17%3A57%3A00Z&se=2019-06-26T17%3A57%3A00Z&sp=rwl&sv=2015-12-11&sr=c&sig=r6lPaduQM9aRz4pMjPNkDWUpLu9ZEf59ankncWTEcqs%3D')
x = data.temperature
y = data.humidity
z = data.Alert

indices = z == 1
plt.scatter(x[indices],y[indices],marker='x',color='r')
plt.scatter(x[~indices],y[~indices],marker = 'o',color='b')
plt.title('Scatter plot of data point')
plt.xlabel('temperature')
plt.ylabel('humidity')
plt.xlim([min(x)-0.5,max(x)+0.5])
plt.ylim([min(y)-0.5,max(y)+0.5])
plt.show()
```

![image](https://wibyog-dm2306.files.1drv.com/y4mQzry30xcFTaNcM5okzMt8o5JwkDD58zdNCLFI-BxiYe-ALBN3ctZ3E7CCkXVOdWq0V1CY2L1YJLAZ-I2UyGfBsqMYfKTdG8jsCVBRhU3fJ-XLKXZiTAddYLXeO8VVKHXRIDOqkcfTaI4uay7Sl6ONF_B4TLzluMTQ3LdHmu2hRH7794mmvrgMHNuSa-qzqmzICBPJ7b-fUj4nDM9kQ7Lyw?width=386&height=279&cropmode=none)

In this exercise, we are doing Two-class classification. From the scatter plot, it seems like the correlation is linear, thus one potential candidate is **Logistic Regression**. Let's try to run the regression in Python.

```python
import pandas as pd
import numpy as np
import matplotlib.pyplot as plt
import plotly.plotly as py
from patsy import dmatrices
from sklearn.linear_model import LogisticRegression
from mpl_toolkits.mplot3d import Axes3D

data = pd.read_csv('https://msinternalportalborfo3vz.blob.core.windows.net/fileshare/Telemetry%20Training.csv?st=2017-06-25T17%3A57%3A00Z&se=2019-06-26T17%3A57%3A00Z&sp=rwl&sv=2015-12-11&sr=c&sig=r6lPaduQM9aRz4pMjPNkDWUpLu9ZEf59ankncWTEcqs%3D')
x = data.temperature
y = data.humidity
z = data.Alert

indices = z == 1
fig = plt.figure()
ax = fig.add_subplot(111, projection='3d')
ax.scatter(x[indices], y[indices], z[indices], c='r', marker='o')
ax.scatter(x[~indices], y[~indices], z[~indices], c='b', marker='^')
ax.set_xlabel('temperature')
ax.set_ylabel('humidity')
ax.set_zlabel('alert')
plt.show()

y,X = dmatrices('Alert ~ temperature + humidity',data, return_type="dataframe")
y = np.ravel(y)

model = LogisticRegression()
model = model.fit(X,y)

print(model.score(X,y))
```

![image](https://jmnb5w-dm2306.files.1drv.com/y4mAabrRXuVWEr7UUDUGVWQxFHjyJC6EdSvDE9Hjhe_euATBla9irWK97SifwTt8ZdRSuiqeVmnksNK6cXqlbvmXmlzkBByOKoWyprrfaTJDp2M9YP6wCHylB1bzPJpWWGr9UCGIQWuw6IcfokAEyjH6_pzBLoEecgw8DGvSEc-xPELKbCoSPp1ZdVHciLFQuatNOsxXXRlXs0fGPGLnr9GTg?width=365&height=256&cropmode=none)

The logistic regression gave a scoring of _0.862_, looks fine to use this algoritm. With this code, we will use **Azure Machine Learning** to reproduce similar algorithm and deploy it as web services.

In previous exercise, we have uploaded the training dataset into **Azure ML Studio**. Now, from **Azure ML Studio**, on the bottom left corner, click "**+** New", choose "_Experiment_" tab, and click on "_Blank Experiment_".

#### 8.1 Import Data
On the left hand side panel, navigate to "Saved Dataset", and drag it to the canvas. To check the dataset, right click the small dot below the box, and click "Visualize". Here you can see a quick summary of the data.

![Image](https://utkvdq-dm2306.files.1drv.com/y4mfgdokmEEsGSR21aSWp7BNkH3evApmm-6zyBw1Rru-5nDzI2jjnmWo7kqdAnSwMOd7rA74wW376Z7aeyn5IbrMaTMWDLIQIjeSRukKVOdvHe5KbRXC_sGKWRINPXgF28pAGFpCDTX3l0G2QNLmiBulznqW0EG3q1jC1V0sEkEW_6sRDATsEU-5K7BONCqzKDrAy9JJZkGHAvkjfz9HgFobA?width=1440&height=810&cropmode=none)

#### 8.2 Data Transformation
Here, we will only require 3 columns, which is _temperature_, _humidity_, and _alert_. Also, the variable _alert_ is not categorical, so we need to transform it. 

Similarly, from the left panel, under "Data Transformation", choose "_Select Columns in Dataset_", and drag to the canvas. Connect the dot between the dataset and the dot above the column selection.

Next, we will transform the data to categorical data. Again, from the "Data Transformation", choose "_Edit Metadata_", and drag it below the canvas. Then click the box and edit the setting. For "Data type" and "Fields", choose "_Unchanged_", and for "Categorical", select "_Make categorical_". 

#### 8.3 Data Partition
Once the data is transformed, let's do the partition. Under "Data Transformation", choose "Partition and Sample". Again, choose "_Sampling_" for "Partition or sample mode", and enter the "Rate of sampling" of your choice. 

#### 8.4 Data Modelling
Under "Machine Learning", drag "Train Model" into the canvas. Again, from the sub-section of "Machine Learning", navigate to "Classification", drag "Two-Class Logistic Regression" into the canvas. Click on "Train Model", launch the column selector and choose "**Alert**". 

Once done, under "Machine Learning" tab, navigate to "Score", choose "Score Model" and drag it into canvas. Once we complete the model scoring, we want to evaluate the model. This can be done by "Evaluate Model", which can be found under "Evaluate" tab.

Lastly, connect the boxes and the canvas should looks something below:

![Image](https://7k3wfw-dm2306.files.1drv.com/y4mXx7bx57yXM8sI2x_jIgwq4fTDyNORGs289IOj9PX2LxNzqiCNGePq2PQUrO8eCkWjuhKHxRtsUTZMruOUUbN9HrXF6PJI06WGXI-DhmttcB4uMfxlpEk-mfag-aAbC5dTsYdjHX4HMLgDWefkNqr08rHUPVIqOLCSf6cttckbo1FDxORFzCyPHfbZ5xH3DFhoQ9d_tPzOC_qMQXrpNg7XA?width=1440&height=810&cropmode=none)

Then, at the bottom canvas, click "**RUN**". Once finished, right click the bottom dot of "Evaluate Model", and click "Visualize". Here, you can see the performance of this model.

In this case, the default threshold value is 0.5, and the accuracy is 90.3%. 

![Image](https://tssh0g-dm2306.files.1drv.com/y4motAh_PtcLjaojzzczkoz73JnGhjdbjS5p9mjRmdUIn0iKw7Jko2v7PSFMANAO7ZQRJ0q35lwN4C_PfjlvKUvPPBileaREo-9CUwvDmMcLO4FiD9E0eFXnBMvaDvgUQyUMACu-QMQm2qElw7tEYVj5FV4w9xf2Fm4hQBWA2Ofbi4GKcZ8Xl43MOZaYKsn-o_ls955NYU79FOlWw4s8SEcLA?width=1440&height=810&cropmode=none)

#### 8.5 Publish Web Services
The final step is setup web services using this algorithm. On the bottom panel, click "**SET UP WEB SERVICES**", and then click "**RUN**" again. Once done, on the left hand panel, click the "Globe", which is "_Web Sercices_". You will be able to find the web services, together with API Key.

To learn how to use this API, click "**REQUEST/RESPONSE**", and there's sample code on how to integrate this into your application.

### 9. Azure Function

Recall from section 7 where we configure **Azure Event Hub**. The message sent to **Azure Event Hub** is the average value of temperature and humidity for a duration of 10 seconds. Now, we want to leverage on **Azure Function** to send the message to the machine learning module and store the result in **Azure Table Storage**. 

**Azure Function** is a serverless compute service that enables developer to run code on-demand without having explicitly provision or manage infrastructure.

There are many ways to trigger **Azure Function**, such as 
1. Timer
1. Http Trigger
1. Github
1. EventHub Trigger
1. Blob Trigger
etc

For this exercise, we use use _EventHub Trigger_, which means the **Azure Function** will be triggered when there's new message in **Azure Event Hub**, and the message will be sent to machine learning web services, and finally store the data in **Azure Table Storage**.

To create **Azure Function**, again, in Azure Portal, click on "**+**" and search for "function app". Choose "**Function App**" and click create.

![image](https://8qqarq-dm2306.files.1drv.com/y4mXB7d2RvtPEmSHuNaBvkJWhNE-_49dgpBU92USXvZrWS0v56VZlXiwLdIu-lD86B2yDk9CqGG9qJR5lQ6EKkGWRwrD43m20R0RCE1SDs3cUr_oH_0LMq2doqW8YzB359Yp96D1ru3Rq9zXWbPup--S_bkxRbMKFRHLZXEebxAFXcaO8zOZpxhkUfg6CE84hp7xe_nwlF7oGmyGSdUpdxmFA?width=1440&height=810&cropmode=none)

Give the app a name, and under "Hosting Plan", choose "_Comsumption Plan_" if the usage is low, or choose "_App Service Plan_" if the usage is high. For "Storage", select the existing storage account that we have created.

Now, click on the **Azure Function**, and on the left hand panel, you will see 
1. Functions
1. Proxies (preview)
1. Slots (preview)

Click on the **+** button beside _Functions_. Then, at the bottom under "Get started on your oen", click "_Custom function_". Change the language to "C#" and scenario to "all". Choose **EventHub Trigger - C#**. Give this function a name, and under "Event Hub connection", connect to the **Azure Event Hub** that we created and replace the Event Hub name with the one we created. For the policy, choose "RootManagerSharedAccessKey". Once done, click "Create".

#### 9.1 Configure Trigger and Output

Once the function is created, click "Integrate". We have configured _Triggers_ when we setup the function, so now we configure the _Outputs_. Click "**+** New Output", and select "_Azure Table Storage_". Again, under "Storage account connection", choose the storage account that we created previously. For the "Table name", give it a name, where results will be stored here. 

![image](https://jmnc5w-dm2306.files.1drv.com/y4mdQkBU8W3Ay-8ICRmNFkZPwuRVYY1QLZkmDJ7zLhB3M31LTbUMf3mPS4-Ou030Po50b_VLdAQt1N2_d6ihqYtK0Cmn63CsPo5AOo_9Itr4VXol2Nx3qnpmi6z8d5ZQcESvz2C_0C5C7Ebeup5KUis-EPk14HBx6H0AzBEL0IhTQcSESWSCHBsc5b6ge1rRT3fHKNL6QLhJ-WIp5dTnsOU7g?width=1440&height=810&cropmode=none)

Once done, click save. Navigate to canvas by clicking the function name.

#### 9.2 Coding
##### 9.2.1 Add packages
First, add the following above "_using System;_".

```csharp
#r "Newtonsoft.Json"
#r "Microsoft.WindowsAzure.Storage"
```

This include the packages needed for the function.

##### 9.2.2 Add libraries
Then, add the following libraries under "_using System;_".

```csharp
using Newtonsoft.Json;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Formatting;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;
```

##### 9.2.3 Add JSON object
We will add the message JSON class so that the function can deserialize the JSON object. Add the following class below **Run** method.

**Event Hub Message**

```csharp
public class message
{
    public string deviceid { get; set; }
    public double temperature { get; set; }
    public double humidity { get; set; }
    public string time { get; set; }
}
```

**Table Storage Schema**

```csharp
public class resultTable : TableEntity
{
    public string deviceId {get; set;}
    public double temperature {get; set;}
    public double humidity {get; set;}
    public string alert {get; set;}
}
```

**Azure ML Request JSON Object**

```csharp
public class StringTable
{
    public string[] ColumnNames { get; set; }
    public string[,] Values { get; set; }
}
```

**Azure ML Response JSON Object**

```csharp
public class Value
{
    public List<string> ColumnNames { get; set; }
    public List<string> ColumnTypes { get; set; }
    public List<List<string>> Values { get; set; }
}

public class Output1
{
    public string type { get; set; }
    public Value value { get; set; }
}

public class Results
{
    public Output1 output1 { get; set; }
}

public class MLResult
{
    public Results Results { get; set; }
}
```

##### 9.2.4 Call Azure ML Function

Recall that when we create web services for **Azure Machine Learning**, there's a sample code which show us how to use the API.

Now, we slightly modify the method, and insert the method before **Run** method.

The code as below:

```csharp
public class AzureML
{
    public static async Task<string> callMLFunction(double temp, double hum)
    {
        string tempString = temp.ToString();
        string humString = hum.ToString();
        using (var client = new HttpClient())
            {
                var scoreRequest = new
                {

                    Inputs = new Dictionary<string, StringTable>() {
                        {
                            "input1",
                            new StringTable()
                            {
                                ColumnNames = new string[] {"PartitionKey", "RowKey", "Timestamp", "deviceid", "deviceid@type", "humidity", "humidity@type", "messageid", "messageid@type", "temperature", "temperature@type", "Alert"},
                                Values = new string[,] {  { "value", "0", "", "value", "value", humString, "value", "0", "value", tempString, "value", "" },  { "value", "0", "", "value", "value", "63", "value", "0", "value", "28", "value", "" },  }

                            }
                        },
                    },
                    GlobalParameters = new Dictionary<string, string>()
                    {
                    }
                };
                const string apiKey = "ML API Key"; // Replace this with the API key for the web service
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);    
                client.BaseAddress = new Uri("https://ussouthcentral.services.azureml.net/workspaces/49560a6cf7a64ee19712b1daaad6b208/services/f2a9b48fa0374fe480515d0e62665bb3/execute?api-version=2.0&details=true");

                HttpResponseMessage response = await client.PostAsJsonAsync("", scoreRequest);

                if (response.IsSuccessStatusCode)
                {
                    string result = await response.Content.ReadAsStringAsync();
                    MLResult resultJSON = JsonConvert.DeserializeObject<MLResult>(result);
                    string alert = resultJSON.Results.output1.value.Values[0][3];
                    if(alert == "0")
                    {
                        return "Safe";
                    }
                    else
                    {
                        return "Warning";
                    }
                }
                else
                {
                    return "Error";
                }
            }
    }

}
```

Noticed that here, we will return 3 different response, depends on the machine learning result. If the predicted value for "alert" is 0, then this method will return "Safe", otherwise "Warning".

##### 9.2.5 Run Method

Now, replace the "Run" method will the following code:

```csharp
public static async Task<string> Run(string myEventHubMessage, ICollector<resultTable> outputTable, TraceWriter log)
{
    message messageJSON = JsonConvert.DeserializeObject<message>(myEventHubMessage);
    string device = messageJSON.deviceid;
    double tempML = messageJSON.temperature;
    double humML = messageJSON.humidity;
    string status = await AzureML.callMLFunction(tempML, humML);
    outputTable.Add(new resultTable()
    {
        PartitionKey = "Functions",
        RowKey = Guid.NewGuid().ToString(),
        deviceId = device,
        temperature = tempML,
        humidity = humML,
        alert = status
    });
    log.Info($"C# Event Hub trigger function processed a message: {status}");
    return "done";
}
```

Essentially, the "Run" method will deserialize incoming message, and pass the temperature, humidity value into ML service to predict the alert. Once done, the result will then be stored in **Azure Table Storage**. 

#### 9.3 Testing
Once everything is setup, run the simulator, and check the log of Azure Function. You should see the predicted status below.

**Azure Function Log**
![Image](https://vqvmqw-dm2306.files.1drv.com/y4mjo-Qwet-K2jJSbllc7cYJ8PmyZ6ZvoQ2p_Z41ousr5f5F4lqQC_TOU8pXSzMcMd5VYmWFdlCkVlHgiy6Asr4dGQhSwemVJvJRBr9DKCQoEy9JDTROHqHKmlmolvu8hyML-32JZ0Q8cnwqK9BIt3eGsUFvXKB_UsLCYzY5EvhmIugoKa13Uhnv46thYe91ASqfwDKcwCVMhGBFUGFWJyT9g?width=1440&height=810&cropmode=none)

**Azure Table Storage**
![Image](https://wibzog-dm2306.files.1drv.com/y4mUEugv1zl62baWt88BWTiPtaG-h17sPfXboI9fodg5Zl0Gfyj_qVkCUUJo96R5CEaVbMNTsUWDi-ohsOEmpDkjWY7Renvgb-l00Hw0gyyplC6gfR3mRcN23kELjoormVjPMLEWtYsvIdgDbNB1Ihv-jWgrfnb3PbMEcGPmNfjusqAAFtGGXo_bCOTjEqbIY73k6CcfE7-0cUcawmN2vfu7w?width=1440&height=810&cropmode=none)

With the results stored in **Azure Table Storage**, developer can create business application using this as database. 

For more info, check out the link below:
1. [Azure Function](https://docs.microsoft.com/en-us/azure/azure-functions/)
1. [Azure Function Rest API](https://docs.microsoft.com/en-us/rest/api/)

### 10. Power BI

The final part of this solution is visualize the streaming data in Power BI.

Recall that when we setup **Azure Stream Analytics**, we defined "_Group Workplace_" and "_Dataset Name_". So, if we login to [Power BI](https://app.powerbi.com), on the left hand panel, we should see the "**Workspaces**". Select the workspace that is defined in **Azure Stream Analytics**.

You will see 4 tabs on top:
1. Dashboards
1. Reports
1. Workbooks
1. Datasets

Now, navigate to "_Datasets_", and you can see the streaming dataset that we defined.

![Image](https://hyj9ug-dm2306.files.1drv.com/y4m37GVvzfMKnAukVj_YxYPyM6EFi1dnsq8tCRm74F21GpQohMgnI7IdfM8z-9QKiLXq_cxK3dLjW8YVQbnHRyUAP17aTv_9-LBUQcEr7QFI3r5Iq1KL5chglridJHFTlhShVARUrZkSRADDFAU76Y781QlV2G6D7-Ms1W0XMymQ5alb8Nc4HT6qyQh8nzJzBI-sHYTufzLofdhD7vyZ33qtg?width=1440&height=810&cropmode=none)

Now we want to create a dashboard view with streaming data. Go to "_Dashboards_", on right top corner, click " + Create" and choose "Dashboard". Give it a name, and launch the dashboard once you have created it.

On the top panel, you will see "+ Add tile". Click on it, and choose "_REAL-TIME DATA  CUSTOM STREAMING DATA_". Click next and you should see the datasets that we defined in **Azure Stream Analytics**. Click on the datasets and click next. 

On the "_Visualization Type_", choose "Line chart". Then you will need to define the graph. Choose **time** for "_Axis_", and for "_Values_", choose **temperature**. Select the Time Windows that you want. 

Click next and complete the remaining details such as "Title" and "Subtitle". Once done, click apply, and the dashboard is done.

Try run the simulator and observe the line graph!

![image](https://ipckxq-dm2306.files.1drv.com/y4mL6dcg2MGh6PuRdTsP6HlHkqoAkQ3Q8-OCyzpJuGvblBPKD3hkPZmaKrg61ftiu_IoRfxFg2H8_PYPeKultm4NorSEy9O3b6v9cEai0vktswItWgYuXJsFlILK-EdtvEvQLGJejZLXcxrAuGLZcly7eLDd1hJvarhU3knIpF0z1W1mfLEwm35383ffIOYX337SbVB7VS-fW19s3FU9smakQ?width=1440&height=810&cropmode=none)

#### Optional
In previous example, we are leveraging **Azure Stream Analytics** to create streaming data set in **Power BI**. In this section, let's discuss on how to create your own streaming data set in **Power BI**, for instance, we want to stream predicted value into dashboard.

To create a streaming data, navigate to "_Datasets_", and click ** + Create ** on top right corner, select _Streaming dataset_ and choose _API_. Then, give your dataset a name, and define the schema of your data. In this case, I want to stream predicted value, thus i define the schema as following:

![image](https://gyjxwg-dm2306.files.1drv.com/y4mze7b-l7v0BukHWa9hIRsvKm0ODnJtgQg6MSksHESlzhkLb8jUQ37n2TJf4fX-DvwYjf8HG0nOYM8oZpbdBTlHBLQPyC0YD-pejBRvyRaqoPXeTYPW1dWdnx5ybEyNwhHHcCaggGrXC8J2S67lYMWWy1j_Fn3bFanPU7Tv8uZX4PgTSQ-IAeIoFfQsJz6m8TUjHoJfziHdcS69nWRz9tVOg?width=1600&height=860&cropmode=none)

Click create, and copy the **Push URL**. The idea is we will perform HTTP Post to push the data into **Power BI** as streaming data, then we can leverage this data and pin it in our dashboard.

Back to **Azure Function**, let's do some modification. First, add a new class, which is essentially a HTTP Client and we perform HTTP Post. Replace the uri with the **Push URL** that you copied. 

```csharp
public class PowerBIStream
{
	public static async void pushBI(string status)
	{
		string content;
		string uri = "<PowerBI Streaming Dataset>"; //replace this with the Power BI Streaming Dataset Push API

		if(status == "Safe")
		{
			content = "{\"Status\":\"0\"}";
		}
		else if(status == "Warning")
		{
			content = "{\"Status\":\"1\"}";
		}
		else
		{
			content = "{\"Status\":\"404\"}";
		}

    	using (var client = new HttpClient())
    	{
        	var response = await client.PostAsync(uri, new StringContent(content, Encoding.UTF8, "application/json"));
    	}
		
	}	
}
```
Then, in the main function, add in this line:

```csharp
	await PowerBIStream.pushBI(status);
 ```
 
 Then you are done! Whenever there's a new predicted value, **Azure Function** will call this URL and push the data into **Power BI** as streaming dataset.
 
 Now, you can create a new streaming tile, just like what we did previously to visualize the value. This is a sample dashboard.
 
 ![image](https://hpcyzq-dm2306.files.1drv.com/y4m6BLi-9uVnG86aq7xVXShsgM7Ply5UsUH_iGQQpGE-piIGZg7WVqOhS7rwLyUii1sWzQX6Xth2u4Q22k8RvXzI0JrgmCEi_YOsMD6Y8B-oRMyzcAppZZItpum1DR8-_FI8eU0grp2HtUXtHN3kIyuEqnCut0WsZH-w5cgZIxEbOGRRzxMByzRx5B-47QTg2DPXGFyXdoWldim6LRwROsYzw?width=1600&height=860&cropmode=none)
 
### 11. Others

This exercise serves as an exercise to understand Azure IoT offering. The architecture used in this exercise is for reference, and depends on the need, you can customize the solution.

#### Additional Information
1. [**Microsoft Azure IoT Reference Architecture**](http://download.microsoft.com/download/A/4/D/A4DAD253-BC21-41D3-B9D9-87D2AE6F0719/Microsoft_Azure_IoT_Reference_Architecture.pdf)
1. [**Microsoft Azure IoT Security**](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-security-ground-up)
1. [**GitHub Repository**](https://github.com/guangying94/AzureIoT)

