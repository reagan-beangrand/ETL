// See https://aka.ms/new-console-template for more information
//Console.WriteLine("Hello, World!");
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Text.Json;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using HtmlAgilityPack;
using System.Text.RegularExpressions;
using CarHive.Parser;
using CarHive.Entity;
using Standard.Licensing;
using Standard.Licensing.Validation;

class Program
{
    private static readonly string logFileName = "process.log";

    // Config values
    private static string licenseKeyFolderPath;
    private static string processedFolder;
    private static string errorFolder;
    private static string folderPath;
    private static string formUrl;
    private static string formPostUrl;
    private static string summaryFile;
    private static string logFileFolderPath;
    private static string userName;
    private static string email;
    private static List<(string FileName, string Status, string Destination, string Timestamp)> summaryRecords 
    = new List<(string, string, string, string)>(); 
    static async Task Main(string[] args)
    {
        LoadConfig("config.json");

        if (!ValidateLicense(licenseKeyFolderPath))
        {
            Log("[ERROR] Invalid license key or machine binding mismatch. Unauthorized installation.");
            return;
        }

        Log("[INFO] License key and machine binding validated. Proceeding...");

        var stopwatch = Stopwatch.StartNew();
        int totalFiles = 0, successCount = 0, failureCount = 0;

        try
        {
            Log($"[INFO] Scanning folder: {folderPath}");
            var pdfFiles = Directory.GetFiles(folderPath, "*.pdf");

            if(pdfFiles.Length ==0)
            {
                Log("[INFO] No PDF files found to process.");
                return;
            }
            Log($"[INFO] Found {pdfFiles.Length} PDF files.");

            var fieldMap = await GetGoogleFormFields(formUrl);
            Log($"[INFO] Found {fieldMap.Count} form fields.");          

         
            foreach (var pdfPath in pdfFiles)
            {
                totalFiles++;
                Log($"[INFO] Processing file: {Path.GetFileName(pdfPath)}");

                var extractedData = ExtractKeyValuePairsFromText(pdfPath);
                Log($"[INFO] Extracted {extractedData.Count} key-value pairs.");

                var success = true;//await SubmitToGoogleForm(formPostUrl, fieldMap, extractedData);
                string destinationFolder = success ? processedFolder : errorFolder;
                string status = success ? "Success" : "Failed";
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                try
                {
                    string destPath = Path.Combine(destinationFolder, Path.GetFileName(pdfPath));
                    File.Move(pdfPath, destPath, true);
                    Log(success
                        ? $"[SUCCESS] Submitted and moved to Processed: {Path.GetFileName(pdfPath)}"
                        : $"[ERROR] Submission failed. Moved to Error: {Path.GetFileName(pdfPath)}");

                    summaryRecords.Add((Path.GetFileName(pdfPath), status, destPath, timestamp));
                }
                catch (Exception ex)
                {
                    Log($"[ERROR] Failed to move file {Path.GetFileName(pdfPath)}: {ex.Message}");
                    summaryRecords.Add((Path.GetFileName(pdfPath), "MoveError", pdfPath, timestamp));
                }

                if (success) successCount++; else failureCount++;

            }
        }
        catch (Exception ex)
        {
            Log($"[EXCEPTION] {ex.Message}");
            Log("[STACKTRACE] " + ex.StackTrace);
        }
        finally
        {
            stopwatch.Stop();
            Log("=== Summary Report ===");
            Log($"Total Files Processed: {totalFiles}");
            Log($"Successful Submissions: {successCount}");
            Log($"Failed Submissions: {failureCount}");
            Log($"=== Process Completed in {stopwatch.Elapsed.TotalSeconds:F2} seconds ===");
            try
            {
                using (var writer = new StreamWriter(summaryFile))
                {
                    writer.WriteLine("FileName,Status,Destination,Timestamp");
                    foreach (var record in summaryRecords)
                    {
                        writer.WriteLine($"{record.FileName},{record.Status},{record.Destination},{record.Timestamp}");
                    }
                }
                Log($"[INFO] Summary CSV written to {summaryFile}");
            }
            catch (Exception ex)
            {
                Log($"[ERROR] Failed to write summary CSV: {ex.Message}");
            }

        }
    }

    static void LoadConfig(string configPath)
    {
        try
        {
            string json = File.ReadAllText(configPath);
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("LicenseKeyFolderPath", out var key))
                licenseKeyFolderPath = key.GetString();
            if (doc.RootElement.TryGetProperty("PDFFileFolderPath", out var folder))
                folderPath = folder.GetString();
            if (doc.RootElement.TryGetProperty("FormUrl", out var form))
                formUrl = form.GetString();
            if (doc.RootElement.TryGetProperty("FormPostUrl", out var post))
                formPostUrl = post.GetString();            
             if (doc.RootElement.TryGetProperty("ProcessedFolder", out var processed))
                processedFolder = processed.GetString();
            if (doc.RootElement.TryGetProperty("ErrorFolder", out var error))
                errorFolder = error.GetString();
            if (doc.RootElement.TryGetProperty("SummaryFile", out var summary))
                summaryFile = summary.GetString();
            if (doc.RootElement.TryGetProperty("LogFileFolderPath", out var logFilePath))
                logFileFolderPath = logFilePath.GetString();
            if (doc.RootElement.TryGetProperty("UserName", out var user))
                userName = user.GetString();
            if (doc.RootElement.TryGetProperty("Email", out var mail))
                email = mail.GetString();

            Directory.CreateDirectory(processedFolder);
            Directory.CreateDirectory(errorFolder);
            Directory.CreateDirectory(logFileFolderPath);


            Log("[INFO] Loaded configuration from JSON.");
        }
        catch (Exception ex)
        {
            Log($"[ERROR] Failed to load config: {ex.Message}");
        }
    }

    static bool ValidateLicense(string keyfolderPath)
    { 
        var licenseFile =$"{keyfolderPath}/license.xml";
        var publicKeyFile = $"{keyfolderPath}/publicKey.txt";
        string licenseText = "";string publicKey = "";
        bool isValidLicense = true;       
        if (!File.Exists(licenseFile))
        {
                Console.WriteLine("License file not found.");
                return false;
        }
        else
            licenseText = File.ReadAllText(licenseFile);

        if (!File.Exists(publicKeyFile))
        {
                Console.WriteLine("Public key file not found.");
                return false;
        }
        else
            publicKey = File.ReadAllText(publicKeyFile);
       
        //var license = License.Load($"{keyfolderPath}/License.lic");
        // Read license back from file
        
        var loadedLicense = License.Load(licenseText);

        // Get stored MAC address from license
        string licensedMac = loadedLicense.AdditionalAttributes.Get("MAC");
        Console.WriteLine("Licensed MAC: " + licensedMac);

        // Get current machine MAC
        string currentMac = GetLocalMacAddress();
        Console.WriteLine("Current MAC: " + currentMac);
        // Validation check
        if (licensedMac == currentMac)
        {
            var validationFailures = loadedLicense.Validate()  
                                .ExpirationDate(systemDateTime: DateTime.Now)  
                                .When(lic => lic.Type == LicenseType.Trial)                                
                                .And()  
                                .Signature(publicKey)  
                                .AssertValidLicense();
            if(validationFailures.Count()>0)
                isValidLicense = false;
            foreach (var failure in validationFailures)
            {
                Log(failure.GetType().Name + ": " + failure.Message + " - " + failure.HowToResolve);
                Console.WriteLine(failure.GetType().Name + ": " + failure.Message + " - " + failure.HowToResolve);
            }
            // if (loadedLicense.Expiration > DateTime.Now)
            // {
            //     Console.WriteLine("✅ License is valid and bound to this machine.");
            // }
            // else
            // {
            //     Console.WriteLine("⚠️ License has expired.");
            // }
        }
        else
        {
            isValidLicense = false;
            Log("❌ License is not valid for this machine.");
            Console.WriteLine("❌ License is not valid for this machine.");
        }


        
        return isValidLicense;
        //string publicKey = File.ReadAllText($"{keyfolderPath}/publicKey.xml");
        //string publicKey = File.ReadAllText($"{keyfolderPath}/public.pem");
        //var manager = new LicenseSystem.LicenseManager($"{keyfolderPath}/license.json", publicKey);
        //return manager.ValidateLicense();
        // if (manager.ValidateLicense())
        // {
        //     Console.WriteLine("License valid. Application starting...");
        //     // Run your app logic
        // }
        // else
        // {
        //     Console.WriteLine("License invalid. Exiting...");
        //     Environment.Exit(1);
        // }
        // return true;
        // string localMachineId = GetLocalMacAddress();
        // Log($"[INFO] Local Machine ID: {localMachineId}");

        // return string.Equals(enteredKey, validLicenseKey, StringComparison.OrdinalIgnoreCase)
        //        && string.Equals(localMachineId, validMachineId, StringComparison.OrdinalIgnoreCase);
    }

    static string GetLocalMacAddress()
    {
        // foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
        // {
        //     if (nic.OperationalStatus == OperationalStatus.Up &&
        //         nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
        //     {
        //         return nic.GetPhysicalAddress().ToString();
        //     }
        // }
        // return "UNKNOWN";
         var macAddr = (
                            from nic in NetworkInterface.GetAllNetworkInterfaces()
                            where nic.OperationalStatus == OperationalStatus.Up &&
                                  nic.NetworkInterfaceType != NetworkInterfaceType.Loopback
                            select string.Join("-", nic.GetPhysicalAddress().GetAddressBytes().Select(b => b.ToString("X2")))
                        ).FirstOrDefault();
            return macAddr?.ToString() ?? "UNKNOWN"; 
    }
    static string ExtractTextFromPdf(string pdfPath)
    {
        string result = "";
        using (var document = PdfDocument.Open(pdfPath))
        {
            foreach (Page page in document.GetPages())
            {
                Console.WriteLine($"Page {page.Number}:");
                //Console.WriteLine(page.Text.TrimStart());
                result = page.Text.TrimStart();
            }
        }
        return result;
    }

    static Dictionary<string, string> ExtractKeyValuePairsFromText(string pdfPath)
    {
        // Define the keys we want to extract
        string[] keysToExtract = { "Car Hive Batch Number", "Car Code", "Car Title",
                                    "Seller / Owner Name", "Address", "City", "State", "Pincode",
                                    "Contact Number","Email Address","Fuel Type","Condition",
                                    "Year of Production","Year of Manufacturing","Body Type",
                                    "Mileage (km)","Transmission (Manual/Automatic)","Engine Capacity (cc)",
                                     "Color","Color Code","Number of Owners","Registration City",
                                     "Registration Number","VIN","Chassis Number","Insurance Validity",
                                     "RC Status","Service History","Last Service Date","Service Center History",
                                     "Warranty Status","Feature Highlights","Car Accessories","Fuel Efficiency (km/l)",
                                     "Tyre Condition","Interior Condition","Exterior Condition","Road Tax Paid",
                                     "Loan Status","Asking Price (₹)","Negotiable (Yes/No)","Description"};
        //var data = new Dictionary<string, string>();
        // Split text into lines
        //string[] lines = textContent.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        //Dictionary<string, string> keyValuePairs = new Dictionary<string, string>();
        Dictionary<string, string> extractedValues = new Dictionary<string, string>();
        extractedValues["User Name"]="mohan";
        //extractedValues["Email"]="Mastermohan292@gmail.com";
        try
        {
            string textContent = ExtractTextFromPdf(pdfPath);            
            var parser = new CarInfoParser();
            CarInfo car = parser.Parse(textContent);
            //Console.WriteLine($"Mileage: {car.Mileage}");
            
            string generalRegEx = @":\s*(.+)";
            // string transmissionRegEx = @"Transmission \(Manual/Automatic\):\s*(\w+)";
            // string MileageRegEx = @"Mileage \(km\):\s*(\d+)";
            // string EngineCapRegEx = @"Engine Capacity \(cc\):\s*(\d+)";
            // string NegotiableRegEx = @"Negotiable \(Yes/No\):\s*(\w+)";
            // string fuelEfficiencyRegEx = @"Fuel Efficiency \(km/l\):\s*([\d.]+)";            
            // string askingPriceRegEx = @"Asking Price \(₹\):\s*([\d,]+)";
            //string fuelEfficiencyRegEx = @"Fuel Efficiency \(km/l\):\s*([\d\.]+)\s*km/l";
            int keysCount = keysToExtract.Length;
            for (int i = 0; i < keysCount; i++)
            {
                if(string.Equals(keysToExtract[i],"Mileage (km)", StringComparison.OrdinalIgnoreCase))                
                    extractedValues[keysToExtract[i]] = car.Mileage.ToString();
                if(string.Equals(keysToExtract[i],"Transmission (Manual/Automatic)",StringComparison.OrdinalIgnoreCase))                
                    extractedValues[keysToExtract[i]] = car.Transmission;
                if(string.Equals(keysToExtract[i],"Engine Capacity (cc)",StringComparison.OrdinalIgnoreCase))                
                    extractedValues[keysToExtract[i]] = car.EngineCapacity.ToString();
                if(string.Equals(keysToExtract[i],"Fuel Efficiency (km/l)",StringComparison.OrdinalIgnoreCase))                
                    extractedValues[keysToExtract[i]] = car.FuelEfficiency.ToString() + " km/l";
                if(string.Equals(keysToExtract[i],"Asking Price (₹)",StringComparison.OrdinalIgnoreCase))                
                    extractedValues[keysToExtract[i]] = car.AskingPrice.ToString();
                if(string.Equals(keysToExtract[i],"Negotiable (Yes/No)",StringComparison.OrdinalIgnoreCase))                
                    extractedValues[keysToExtract[i]] = car.Negotiable;    
                
                string pattern = string.Concat(keysToExtract[i], generalRegEx);
                Match match = Regex.Match(textContent, pattern);
                if (match.Success)
                {
                    var extractedValue = match.Groups[1].Value.Trim();
                    if(string.Equals(keysToExtract[i],"Description",StringComparison.OrdinalIgnoreCase))                        
                        extractedValues[keysToExtract[i]] = extractedValue;
                    else                
                        extractedValues[keysToExtract[i]]= GetKeyValue(extractedValue, $"{keysToExtract[i+1]}:");
                }  
            }
            /*foreach (var key in keysToExtract)
            {
                string pattern = key + @":\s*(.+)";
                Match match = Regex.Match(textContent, pattern);
                if (match.Success)
                {
                    var extractedValue = match.Groups[1].Value.Trim();                    
                    if (string.Equals(key, keysToExtract[0],StringComparison.InvariantCultureIgnoreCase))
                        data[key]= GetKeyValue(extractedValue, $"{keysToExtract[1]}:");
                    if (string.Equals(key, keysToExtract[1],StringComparison.InvariantCultureIgnoreCase))
                        data[key]= GetKeyValue(extractedValue, $"{keysToExtract[2]}:");
                    if (string.Equals(key, keysToExtract[2],StringComparison.InvariantCultureIgnoreCase))
                        data[key]= GetKeyValue(extractedValue, $"{keysToExtract[3]}:");
                    if (string.Equals(key, keysToExtract[3],StringComparison.InvariantCultureIgnoreCase))
                        data[key]= GetKeyValue(extractedValue, $"{keysToExtract[4]}:");
                }
            }*/
            //Print results
            foreach (var kvp in extractedValues)
            {
                Log($"[PDF] {kvp.Key} -> {kvp.Value}");
                //Console.WriteLine($"{kvp.Key} -> {kvp.Value}");
            }
        }
        catch (Exception ex)
        {
            Log($"[ERROR] Failed to read PDF {Path.GetFileName(pdfPath)}: {ex.Message}");
        }
        return extractedValues;
    }

    private static string GetKeyValue(string inputText, string marker)
    {
        int index = inputText.IndexOf(marker);
        string beforeText = "";
        if (index != -1)
        {
            // Text before Seller / Owner Name
            beforeText = inputText.Substring(0, index).Trim();

            // Text after Seller / Owner Name
            //string afterText = inputText.Substring(index).Trim();

            //Console.WriteLine("Before Seller / Owner Name:");
            //Console.WriteLine(beforeText);
            //Console.WriteLine();
            //Console.WriteLine("After Seller / Owner Name:");
            //Console.WriteLine(afterText);
        }
        else
        {
            Log($"[ERROR] Marker not found in text: {marker}");
            //Console.WriteLine("Marker not found in text.");
        }
        return beforeText;
    }

    static Dictionary<string, string> ExtractKeyValuePairs(string pdfPath)
    {
        var data = new Dictionary<string, string>();
        try
        {
            using (var document = PdfDocument.Open(pdfPath))
            {
                foreach (Page page in document.GetPages())
                {
                    /*Console.WriteLine($"Page {page.Number}:");
                    Console.WriteLine(page.Text.Trim());
                    
                    // Get individual words with positions
                    foreach (var word in page.GetWords())
                    {
                        Console.WriteLine($"Word: '{word.Text}' at {word.BoundingBox}");
                    }
                    string text = ContentOrderTextExtractor.GetText(page).Trim();
                    IEnumerable<Word> words = page.GetWords(NearestNeighbourWordExtractor.Instance);
                    */
                    var lines = page.Text.Trim().Split('\n');
                    foreach (var line in lines)
                    {
                        if (line.Contains(":"))
                        {
                            var parts = line.Split(new[] { ':' }, 2);
                            if (parts.Length == 2)
                                data[parts[0].Trim()] = parts[1].Trim();
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log($"[ERROR] Failed to read PDF {Path.GetFileName(pdfPath)}: {ex.Message}");
        }
        return data;
    }

    static async Task<Dictionary<string, string>> GetGoogleFormFields(string formUrl)
    {
        var googleFormsToolkitLibrary = new GoogleFormsToolkitLibrary.GoogleFormsToolkitLibrary();
        var results = await googleFormsToolkitLibrary.LoadGoogleFormStructureAsync(formUrl);
        var fieldMap = new Dictionary<string, string>();
        try
        {
            if (results != null && results.QuestionFieldList != null)
            {
                foreach (var questionField in results.QuestionFieldList)
                {
                    var question = questionField.QuestionText.Trim();
                    var input = questionField.AnswerSubmissionId;
                    if (input != null)
                    {
                        //var entryId = input.GetAttributeValue("name", "");
                        fieldMap[question] = input;
                        Log($"[MAP] {question} -> {input}");
                    }
                }
            }
            else
            {
                Log("[WARN] No form fields found.");
            }
            /*var web = new HtmlWeb();
            var doc = await web.LoadFromWebAsync(formUrl);

            var labels = doc.DocumentNode.SelectNodes("//div[contains(@class,'freebirdFormviewerComponentsQuestionBaseTitle')]");
            if (labels != null)
            {
                foreach (var label in labels)
                {
                    var question = label.InnerText.Trim();
                    var input = label.SelectSingleNode(".//following::input[@name]");
                    if (input != null)
                    {
                        var entryId = input.GetAttributeValue("name", "");
                        fieldMap[question] = entryId;
                        Log($"[MAP] {question} -> {entryId}");
                    }
                }
            }
            else
            {
                Log("[WARN] No form fields found.");
            }*/
        }
        catch (Exception ex)
        {
            Log($"[ERROR] Failed to parse Google Form: {ex.Message}");
        }
        return fieldMap;
    }

    static async Task<bool> SubmitToGoogleForm(string postUrl, Dictionary<string, string> fieldMap, Dictionary<string, string> extractedData)
    {
        try
        {
            using (var client = new HttpClient())
            {
                var formData = new Dictionary<string, string>();
                foreach (var kvp in fieldMap)
                {
                    if (extractedData.ContainsKey(kvp.Key))
                    {
                        formData[$"entry.{kvp.Value}"] = extractedData[kvp.Key];
                        Log($"[DATA] {kvp.Key}: {extractedData[kvp.Key]}");
                    }
                }
                formData["emailAddress"] = "Mastermohan292@gmail.com";
                foreach (var kvp in formData)
                {
                    Log($"{kvp.Key} -> {kvp.Value}");
                    Console.WriteLine($"{kvp.Key} -> {kvp.Value}");
                }

                var content = new FormUrlEncodedContent(formData);
                var response = await client.PostAsync(postUrl, content);

                Log($"[INFO] HTTP Response: {response.StatusCode}");
                return response.IsSuccessStatusCode;
            }
        }
        catch (Exception ex)
        {
            Log($"[ERROR] Failed to submit form: {ex.Message}");
            return false;
        }
    }

    static void Log(string message)
    {
        string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}";
        Console.WriteLine(logEntry);
        File.AppendAllText(logFileFolderPath + logFileName, logEntry + Environment.NewLine);
    }
}