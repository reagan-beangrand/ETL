// See https://aka.ms/new-console-template for more information
//using System.Security.Cryptography;


// using var rsa = new RSACryptoServiceProvider(2048);
// string privateKey = rsa.ToXmlString(true);  // keep secret
// string publicKey = rsa.ToXmlString(false); // embed in app
// File.WriteAllText("privateKey.xml", privateKey);
// File.WriteAllText("publicKey.xml", publicKey);
// Console.WriteLine("Private and public keys generated successfully!");

var keyGenerator = Standard.Licensing.Security.Cryptography.KeyGenerator.Create(); 
var keyPair = keyGenerator.GenerateKeyPair(); 
var privateKey = keyPair.ToEncryptedPrivateKeyString("0A-00-27-00-00-04");  
var publicKey = keyPair.ToPublicKeyString();
File.WriteAllText("privateKey.txt", privateKey);
File.WriteAllText("publicKey.txt", publicKey);
Console.WriteLine("Private and public keys generated successfully!");
