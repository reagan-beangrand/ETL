using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Net.NetworkInformation;
using Newtonsoft.Json;

namespace LicenseSystem
{
  
    public class LicenseData
    {
        public string MachineId { get; set; }
        public string Product { get; set; }
        public string Version { get; set; }
        public DateTime Expiry { get; set; }
        public string Signature { get; set; }
    }

    public class LicenseManager
    {
        private readonly string _licenseFilePath;
        private readonly string _publicKey;
        private readonly string _publicKeyPem;


        public LicenseManager(string licenseFilePath, string publicKeyPem)
        {
            _licenseFilePath = licenseFilePath;
            _publicKeyPem = publicKeyPem;
        }

        // Generate machine fingerprint
        private string GetMachineId()
        {
            var sb = new StringBuilder();

            // Local Machine ID
            // Get all network interfaces
            /*NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (NetworkInterface adapter in interfaces)
            {
                // Get the physical address for the current adapter
                PhysicalAddress address = adapter.GetPhysicalAddress();
                byte[] bytes = address.GetAddressBytes();

                // Format the address into a readable string (e.g., 00-00-00-00-00-00)
                string macAddress = string.Empty;
                for (int i = 0; i < bytes.Length; i++)
                {
                    macAddress += bytes[i].ToString("X2"); // Format as two uppercase hexadecimal digits
                    if (i != bytes.Length - 1)
                    {
                        macAddress += "-"; // Add hyphens between bytes
                    }
                }

                Console.WriteLine($"Interface: {adapter.Name}, MAC Address: {macAddress}");
            }*/
            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.OperationalStatus == OperationalStatus.Up &&
                    nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                {
                    // Get the physical address for the current adapter
                    PhysicalAddress address = nic.GetPhysicalAddress();
                    byte[] bytes = address.GetAddressBytes();

                    // Format the address into a readable string (e.g., 00-00-00-00-00-00)
                    string macAddress = string.Empty;
                    for (int i = 0; i < bytes.Length; i++)
                    {
                        macAddress += bytes[i].ToString("X2"); // Format as two uppercase hexadecimal digits
                        if (i != bytes.Length - 1)
                        {
                            macAddress += "-"; // Add hyphens between bytes
                        }
                    }
                    sb.Append(macAddress);
                    break;
                }
            }
            return sb.ToString();
            // Hash fingerprint
            // using (var sha = SHA256.Create())
            // {
            //     var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            //     return Convert.ToBase64String(sha.ComputeHash(bytes));
            // }
        }

        // Validate license file
        public bool ValidateLicense()
        {
            if (!File.Exists(_licenseFilePath))
            {
                Console.WriteLine("License file not found.");
                return false;
            }

            try
            {
                string json = File.ReadAllText(_licenseFilePath);
                var license = JsonConvert.DeserializeObject<LicenseData>(json);

                if (license == null)
                {
                    Console.WriteLine("Invalid license format.");
                    return false;
                }

                // Check machine binding
                if (license.MachineId != "ABC123")//!= GetMachineId())
                {
                    Console.WriteLine("License does not match this machine.");
                    return false;
                }

                // Check expiry
                if (DateTime.UtcNow > license.Expiry)
                {
                    Console.WriteLine("License expired.");
                    return false;
                }

                // Verify signature
                var isValid = VerifySignature(license);
                return isValid;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"License validation error: {ex.Message}");
                return false;
            }
        }

         // Verify RSA signature using PEM public key
        private bool VerifySignature(LicenseData license)
        {
            try
            {
                using RSA rsa = RSA.Create();
                rsa.ImportFromPem(_publicKeyPem);

                var unsignedData = new
                {
                    license.MachineId,
                    license.Product,
                    license.Version,
                    license.Expiry
                };

                string dataJson = JsonConvert.SerializeObject(unsignedData);
                byte[] dataBytes = Encoding.UTF8.GetBytes(dataJson);
                byte[] sigBytes = Convert.FromBase64String(license.Signature);

                return rsa.VerifyData(dataBytes, sigBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            }
            catch
            {
                return false;
            }
        }

        // Verify RSA signature
        private bool VerifySignature1(LicenseData license)
        {
            try
            {
                using (var rsa = new RSACryptoServiceProvider())
                {
                    rsa.FromXmlString(_publicKey);

                    // Serialize license without signature
                    var unsignedData = new LicenseData
                    {
                        MachineId = license.MachineId,
                        Product = license.Product,
                        Version = license.Version,
                        Expiry = license.Expiry
                    };

                    string dataJson = JsonConvert.SerializeObject(unsignedData);
                    byte[] dataBytes = Encoding.UTF8.GetBytes(dataJson);
                    byte[] sigBytes = Convert.FromBase64String(license.Signature);

                    return rsa.VerifyData(dataBytes, new SHA256CryptoServiceProvider(), sigBytes);
                }
            }
            catch
            {
                return false;
            }
        }        
    }
}