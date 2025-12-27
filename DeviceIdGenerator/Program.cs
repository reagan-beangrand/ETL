// See https://aka.ms/new-console-template for more information
using System.Text.Json;
using DeviceId;

Console.WriteLine("Process Started");

string deviceId = new DeviceIdBuilder()
                                .AddMachineName()
                                .AddOsVersion()
                                .OnWindows(windows => windows
                                    .AddProcessorId()
                                    .AddMotherboardSerialNumber()
                                    .AddSystemDriveSerialNumber())
                                .ToString();
string json = File.ReadAllText("config.json");
string deviceIdPath=string.Empty;
using var doc = JsonDocument.Parse(json);

if (doc.RootElement.TryGetProperty("DeviceId", out var key))
{
    deviceIdPath = key.GetString();
    //string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {deviceId}";
    Console.WriteLine(deviceId);
    Directory.CreateDirectory(deviceIdPath);
    string deviceIdFilePath = $"{deviceIdPath}\\device_id.txt";
    // if(File.Exists(deviceIdFilePath))    
    //     File.Delete(deviceIdFilePath);    
    File.WriteAllText(deviceIdFilePath, deviceId);
    Console.WriteLine("DeviceId created successfully at: " + deviceIdPath);
}
else
    Console.WriteLine("DeviceId path not found in config.json");


Console.WriteLine("Process Ended");