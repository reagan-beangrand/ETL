using System;
using Standard.Licensing;
using System.Text;
using System.Xml;
using DeviceId;
using Microsoft.IdentityModel.Tokens;

namespace LicenseKeyPairGenerator;

public class LicenseCreation
{
    public void CreateLicense(string deviceId)
    {
        Console.WriteLine("License file generation started!");
        DateTime expiryDate = DateTime.UtcNow.Date.AddDays(30);
        string deviceIdentifier = deviceId;
        string customerName = "Test Customer";string passPhrase="test-passphrase";
        string privateKey="MIHAMCMGCiqGSIb3DQEMAQMwFQQQfPNeTfWfv7NEyOeTjwJVMwIBCgSBmIpQJ+ZebwVo8liKM5bpdt8a7Z63ZbJroEQFUPbRam3xqRJYEI1ak/dFK2x6QO0yBEAwk4PtsuaZtGbteVeFC9eX0nh6qLp7wKeJwrMWIiCJWXve5hteH11wuJyMcJ/UF1p2qSBcivI5tCVJIp7km4xPk1ZrsBE1N0LVSKgDsmHPcM9HTtlYQxFQ6oMgROhgd+05xCtseZwS";
        License newLicense = License.New()
            .WithUniqueIdentifier(Guid.NewGuid())
            .ExpiresAt(expiryDate)
            .WithAdditionalAttributes(new Dictionary<string, string>
            {
                { "DeviceIdentifier", deviceIdentifier }
            })
            .LicensedTo((c) => c.Name = customerName)            
            .CreateAndSignWithPrivateKey(privateKey, passPhrase);

        string licenseString = newLicense.ToString();
        //File.WriteAllText("license.xml", newLicense.ToString());  
       
 
        /* using (var xmlWriter = new XmlTextWriter("license.xml", Encoding.UTF8))
        {
            newLicense.Save(xmlWriter);
        } */
        string licenseKey = Base64UrlEncoder.Encode(licenseString);

        Console.WriteLine(licenseKey);
        File.WriteAllText($"license.txt", licenseKey);
        Console.WriteLine("License file generated successfully!");
    }

  
}
