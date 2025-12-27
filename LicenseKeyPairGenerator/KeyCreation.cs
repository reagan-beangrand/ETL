using System;
using Standard.Licensing.Security.Cryptography;

namespace LicenseKeyPairGenerator;

public class KeyCreation
{
 public void KeyPairGenerator()
 {
    Console.WriteLine("LicenseKeyPairGenerator started!");
   
 
    KeyGenerator keyGenerator = KeyGenerator.Create();
    KeyPair keyPair = keyGenerator.GenerateKeyPair();
    string passPhrase = "BatchProcessor";
    string privateKey = keyPair.ToEncryptedPrivateKeyString(passPhrase);
    string publicKey = keyPair.ToPublicKeyString();
    
    Console.WriteLine("Private key: {0}", privateKey);
    Console.WriteLine("Public key : {0}", publicKey);
    File.WriteAllText("privateKey.txt", privateKey);
    File.WriteAllText("publicKey.txt", publicKey);
    Console.WriteLine("LicenseKeyPairGenerator completed!");
 }
}
