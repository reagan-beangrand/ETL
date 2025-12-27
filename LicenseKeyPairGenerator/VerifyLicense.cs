using System;
using System.Xml;
using Microsoft.IdentityModel.Tokens;
using Standard.Licensing;
using Standard.Licensing.Validation;

namespace LicenseKeyPairGenerator;

public class VerifyLicense
{
    public void VerifyLicenseFile(string deviceId)
    {
        Console.WriteLine("License file verification started!");
        //License license;
        string licenseKey = File.ReadAllText(@"C:\Personal\Reagan\Work\Projects\Mohan\ETL\LicenseKeyPairGenerator\bin\Debug\net9.0\license.txt");
        /* using (var xmlReader = new XmlTextReader(@"C:\Personal\Reagan\Work\Projects\Mohan\ETL\LicenseKeyPairGenerator\bin\Debug\net9.0\license.xml"))
        {
            license = License.Load(xmlReader);
        } */
       
        License license = License.Load(Base64UrlEncoder.Decode(licenseKey));

        string currentDeviceIdentifier = deviceId;//Environment.MachineName;
        string publicKey = "MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEnE9MbXcPOYeVNBoI0ZjSoNK4l1gsIUQ84jZbklF4ZxzGO1fMREetdTmzfRxBqyFiNyeMkK8DJm22zPbojgwpEw==";
        var validationFailures = 
            license.Validate()
           .ExpirationDate()
           .And()
           .Signature(publicKey)
           .And()
           .AssertThat(lic => // Check Device Identifier matches.
               lic.AdditionalAttributes.Get("DeviceIdentifier") == currentDeviceIdentifier,
               new GeneralValidationFailure()
               {
                   Message      = "Invalid Device.",
                   HowToResolve = "Contact the supplier to obtain a new license key."
               })
           .AssertValidLicense()
           .ToList();
 
        if (validationFailures.Any())
        {
            throw new UnauthorizedAccessException(validationFailures.First().Message);
        }
        Console.WriteLine("License file verified successfully!");
    }
}
