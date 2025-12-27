
using DeviceId;
using LicenseKeyPairGenerator;

var keyGenerator = Standard.Licensing.Security.Cryptography.KeyGenerator.Create(); 
var keyPair = keyGenerator.GenerateKeyPair();
string passPhrase = "BatchProcessor";string customerEmail="mastermohan292@gmail.com";
var privateKey = keyPair.ToEncryptedPrivateKeyString(passPhrase);  
var publicKey = keyPair.ToPublicKeyString();
File.WriteAllText($"{customerEmail}_privateKey.txt", privateKey);
File.WriteAllText($"{customerEmail}_publicKey.txt", publicKey);
Console.WriteLine("Private and public keys generated successfully!");

//KeyCreation keyCreation = new KeyCreation();
//keyCreation.KeyPairGenerator();

//LicenseCreation licenseCreation = new LicenseCreation();
//licenseCreation.CreateLicense(GetDeviceId());

//VerifyLicense verifyLicense = new VerifyLicense();
//verifyLicense.VerifyLicenseFile(GetDeviceId());

 /*  static string GetDeviceId()
        {
            string deviceId = new DeviceIdBuilder()
                                .AddMachineName()
                                .AddOsVersion()
                                .OnWindows(windows => windows
                                    .AddProcessorId()
                                    .AddMotherboardSerialNumber()
                                    .AddSystemDriveSerialNumber())
                                .ToString();

            return deviceId;           
        } */