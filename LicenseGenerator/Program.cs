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

namespace LicenseGenerator
{    class Program
    {
        static void Main(string[] args)
        {
           
            // Get MAC address of first active network adapter
            //string macAddress = "0A-00-27-00-00-04";
            //Console.WriteLine("MAC Address: " + macAddress);


            /* string privateKey = File.ReadAllText("C:\\Personal\\Reagan\\Work\\Projects\\Mohan\\ETL\\LicenseGenerator\\mastermohan292@gmail.com_privateKey.txt");
            DateTime expiryDate = DateTime.UtcNow.Date.AddDays(30);
            string deviceIdentifier = GetDeviceId();string passPhrase="HJpb&JINR^jg&zeI";
            License newLicense = License.New()
                .WithUniqueIdentifier(Guid.NewGuid())
                .ExpiresAt(expiryDate)
                .WithAdditionalAttributes(new Dictionary<string, string>
                {
                    { "DeviceIdentifier", deviceIdentifier }
                })
                .LicensedTo("Mohan","mastermohan292@gmail.com")
                .CreateAndSignWithPrivateKey(privateKey, passPhrase);
          
            // Save license to XML file           
            File.WriteAllText("license.xml", newLicense.ToString());            
            Console.WriteLine("License generated successfully!");      */   
            Console.WriteLine("License file generation started!");
            DateTime expiryDate = DateTime.UtcNow.Date.AddDays(15);
            string deviceIdentifier = GetDeviceId();
            string customerName = "Mohan";string passPhrase="BatchProcessor";
            string privateKey = File.ReadAllText("C:\\Personal\\Reagan\\Work\\Projects\\Mohan\\ETL\\LicenseGenerator\\mastermohan292@gmail.com_privateKey.txt");            
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
        static string GetDeviceId()
        {
            return new DeviceIdBuilder()
                                .AddMachineName()
                                .AddOsVersion()
                                .OnWindows(windows => windows
                                    .AddProcessorId()
                                    .AddMotherboardSerialNumber()
                                    .AddSystemDriveSerialNumber())
                                .ToString();
        }

    }
}