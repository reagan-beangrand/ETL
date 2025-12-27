using System;
using System.IO;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using DeviceId;
using DeviceId.Encoders;
using DeviceId.Windows;
using DeviceId.Formatters;
using Standard.Licensing;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json;

namespace LicenseGenerator
{    class Program
    {
        private static string privateKeyPath;
        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("License file generation started!");
                DateTime expiryDate = DateTime.UtcNow.Date.AddDays(15);
                string deviceIdentifier = GetDeviceId();
                string privateKey="";
                string customerName = "Mohan";string passPhrase="BatchProcessor";
                 if (!File.Exists(privateKeyPath))
                     Console.WriteLine("PrivateKey file not found.");
                 else      
                     privateKey = File.ReadAllText(privateKeyPath);
                

                License newLicense = License.New()
                .WithUniqueIdentifier(Guid.NewGuid())
                .As(LicenseType.Trial)
                .ExpiresAt(expiryDate)
                .WithAdditionalAttributes(new Dictionary<string, string>
                {
                    { "DeviceIdentifier", deviceIdentifier }
                })
                .LicensedTo((c) => c.Name = customerName)            
                .CreateAndSignWithPrivateKey(privateKey, passPhrase);                
                string licenseKey = Base64UrlEncoder.Encode(newLicense.ToString());

                Console.WriteLine(licenseKey);
                File.WriteAllText($"license.txt", licenseKey);
                Console.WriteLine("License file generated successfully!");  
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }             
              
        }        
        static string GetDeviceId()
        {
            string json = File.ReadAllText("config.json");
            string deviceIdPath;string deviceId="";
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("DeviceId", out var key))
            {
                deviceIdPath = key.GetString()+"\\device_Id.txt";
                if (!File.Exists(deviceIdPath))
                    Console.WriteLine("DeviceId file not found.");
                else      
                    deviceId = File.ReadAllText(deviceIdPath); 
            }
            else
                Console.WriteLine("DeviceId path not found in config.json");

            if (doc.RootElement.TryGetProperty("PrivateKey", out var privateKeyValue))
                privateKeyPath = privateKeyValue.GetString();

            return deviceId;
        }
    }
}