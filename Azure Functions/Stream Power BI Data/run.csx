#r "Newtonsoft.Json"
#r "Microsoft.WindowsAzure.Storage"

using System;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Formatting;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;

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
                                ColumnNames = new string[] {"PartitionKey", "RowKey", "Timestamp", "deviceid", "humidity", "messageid", "temperature", "Alert"},
                                Values = new string[,] {  { "value", "0", "", "value", humString, "0", tempString, "0" },  { "value", "0", "", "value", "0", "0", "0", "0" },  }
                            }
                        },
                    },
                    GlobalParameters = new Dictionary<string, string>()
                    {
                    }
                };
                 const string apiKey = "abc123"; // Replace this with the API key for the web service
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue( "Bearer", apiKey);
                client.BaseAddress = new Uri("https://ussouthcentral.services.azureml.net/workspaces/49560a6cf7a64ee19712b1daaad6b208/services/9a7d48889fa54323b992864121e3982a/execute?api-version=2.0&details=true");
                
                HttpResponseMessage response = await client.PostAsJsonAsync("", scoreRequest);

                if (response.IsSuccessStatusCode)
                {
                    string result = await response.Content.ReadAsStringAsync();               
                    MLResult resultJSON = JsonConvert.DeserializeObject<MLResult>(result);
                    string alert = resultJSON.Results.output1.value.Values[1][3];
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

//Class for doing Power BI Http Post
//Results in streaming the results of machine learning
public class PowerBIStream
{
	public static async void pushBI(string status)
	{
		string content;
		string uri = "<PowerBI Streaming Dataset"; //replace this with the Power BI Streaming Dataset Push API

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

public static async Task<string> Run(string myEventHubMessage, ICollector<resultTable> outputTable, ICollector<string> outputEventHubMessage, TraceWriter log)
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
	
	await PowerBIStream.pushBI(status);
    log.Info($"C# Event Hub trigger function processed a message: {status}");
    return "done";
}


// declare class for different object

//Event Hub message JSON schema
public class message
{
    public string deviceid { get; set; }
    public double temperature { get; set; }
    public double humidity { get; set; }
    public string time { get; set; }
}

//Azure Table Storage schema
public class resultTable : TableEntity
{
    public string deviceId {get; set;}
    public double temperature {get; set;}
    public double humidity {get; set;}
    public string alert {get; set;}
}

//Azure ML request JSON object
public class StringTable
{
    public string[] ColumnNames { get; set; }
    public string[,] Values { get; set; }
}

//Azure ML result JSON object
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