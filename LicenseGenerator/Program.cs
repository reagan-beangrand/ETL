using System;
using System.IO;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using Newtonsoft.Json;
using Standard.Licensing;

namespace LicenseGenerator
{    class Program
    {
        static void Main(string[] args)
        {
            /*if (args.Length < 4)
            {
                Console.WriteLine("Usage: LicenseGenerator <machineId> <product> <version> <expiry-date>");
                 Console.WriteLine("Example: LicenseGenerator ABC123 BatchProcessor 1.0.0 2026-12-31");
                 return;
            }

            string machineId = args[0];
            string product = args[1];
            string version = args[2];
            DateTime expiry = DateTime.Parse(args[3]);

            
            
            var license = new LicenseData
            {
                MachineId = machineId,
                Product = product,
                Version = version,
                Expiry = expiry
            };
            
            // Load private key (XML format or PEM converted to XML)
            string privateKey = File.ReadAllText("C:\\Personal\\Reagan\\Work\\Projects\\Mohan\\ETL\\LicenseKeyPairGenerator\\privateKey.xml");

            // Sign license
            using (var rsa = new RSACryptoServiceProvider())
            {
                rsa.FromXmlString(privateKey);
                //rsa.FromXmlString("<>");

                string dataJson = JsonConvert.SerializeObject(new
                {
                    license.MachineId,
                    license.Product,
                    license.Version,
                    license.Expiry
                });

                byte[] dataBytes = Encoding.UTF8.GetBytes(dataJson);
                byte[] sigBytes = rsa.SignData(dataBytes, new SHA256CryptoServiceProvider());

                license.Signature = Convert.ToBase64String(sigBytes);
            }

            // Save license file
            string licenseJson = JsonConvert.SerializeObject(license, Formatting.Indented);
            */
            //string licenseJson = GenerateLicense(license);
            // Get MAC address of first active network adapter
            string macAddress = "0A-00-27-00-00-04";
            Console.WriteLine("MAC Address: " + macAddress);


            string privateKey = File.ReadAllText("C:\\Personal\\Reagan\\Work\\Projects\\Mohan\\ETL\\LicenseGenerator\\privateKey.txt");
            var license = License.New()  
                        .WithUniqueIdentifier(Guid.NewGuid())  
                        .As(LicenseType.Trial)  
                        .ExpiresAt(DateTime.Now.AddDays(30))
                        .WithAdditionalAttributes(new Dictionary<string, string>  
                        {  
                            {"MAC", macAddress},
                            {"Product", "BatchProcessor"},  
                            {"Version", "1.0.0"},  
                        })
                        .LicensedTo("Mohan", "Mastermohan292@gmail.com")  
                        .CreateAndSignWithPrivateKey(privateKey, macAddress);

            //File.WriteAllText("license.json", licenseJson);
            //File.WriteAllText("License.lic", license.ToString(), Encoding.UTF8);        
            // Save license to XML file
            string licensePath = "license.xml";
            File.WriteAllText(licensePath, license.ToString());
 

            
            Console.WriteLine("License generated successfully!");
            //Console.WriteLine(license.ToString());
        }

        // Helper method to get MAC address
        static string GetMacAddress()
        {
            var nic = NetworkInterface.GetAllNetworkInterfaces()
                .FirstOrDefault(n => n.OperationalStatus == OperationalStatus.Up &&
                                     n.NetworkInterfaceType != NetworkInterfaceType.Loopback);

            return nic?.GetPhysicalAddress().ToString() ?? "UNKNOWN";
           
        }


        public static string GenerateLicense(LicenseData license)
        {
            using RSA rsa = RSA.Create();
            rsa.ImportFromPem("C:\\Temp\\License\\private.pem");

            var unsignedData = new
            {
                license.MachineId,
                license.Product,
                license.Version,
                license.Expiry
            };

            string dataJson = JsonConvert.SerializeObject(unsignedData);
            byte[] dataBytes = Encoding.UTF8.GetBytes(dataJson);
            byte[] sigBytes = rsa.SignData(dataBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            license.Signature = Convert.ToBase64String(sigBytes);

           return JsonConvert.SerializeObject(license, Newtonsoft.Json.Formatting.Indented);           
        }

    }
}