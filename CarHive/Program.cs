// See https://aka.ms/new-console-template for more information
//Console.WriteLine("Hello, World!");
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Text.Json;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using System.Xml;
using System.Text.RegularExpressions;
using CarHive.Parser;
using CarHive.Entity;
using Standard.Licensing;
using Standard.Licensing.Validation;
using DeviceId;
using Microsoft.IdentityModel.Tokens;

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
        Console.WriteLine("=== Car Hive PDF Processor Started ===");
        LoadConfig("config.json");

        if (!ValidateLicense(licenseKeyFolderPath))
        {
            //Log("[ERROR] Invalid license key or machine binding mismatch. Unauthorized installation.");
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

                var success = await SubmitToGoogleForm(formPostUrl, fieldMap, extractedData);
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
         Console.WriteLine("=== Car Hive PDF Processor Completed ===");
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
        var licenseKeyPath =$"{keyfolderPath}\\license.txt";        
        string publicKey = "MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAED8bkFa/MaW5NsKlLj0tejVpWdT36x/P90f94Fk4TA4hBcbZYewp9pTm8aSK1wzU73/5fQ/urws/yakYUFIyrkw=="; 
        bool isValidLicense = true; License loadedLicense;      
        if (!File.Exists(licenseKeyPath))
        {
            Log("[ERROR] License file not found.");
            Console.WriteLine("License file not found.");
            return false;
        }
        else
        {
            string licenseKey = File.ReadAllText(licenseKeyPath);        
            loadedLicense = License.Load(Base64UrlEncoder.Decode(licenseKey));            
        }
        // Get stored Device Identifier from license
        string licensedDeviceId = loadedLicense.AdditionalAttributes.Get("DeviceIdentifier");
        // Get current machine Device Identifier
        string currentDeviceId = GetDeviceId();        
        // Validation check
        var validationFailures = loadedLicense.Validate()
                                .ExpirationDate()                           
                                .When(lic => lic.Type == LicenseType.Trial)
                                .And()
                                .Signature(publicKey)
                                .And()
                                .AssertThat(lic => // Check Device Identifier matches.
                                lic.AdditionalAttributes.Get("DeviceIdentifier") == currentDeviceId,
                                new GeneralValidationFailure()
                                {
                                    Message      = "Invalid Device.",
                                    HowToResolve = "Contact the supplier to obtain a new license key."
                                })
                                .AssertValidLicense()
                                .ToList();
                        
        if (validationFailures.Any())
        {
             isValidLicense = false;
            foreach (var failure in validationFailures)
            {
                Log($"[ERROR] {failure.GetType().Name}: {failure.Message} - {failure.HowToResolve}");
                Console.WriteLine(failure.GetType().Name + ": " + failure.Message + " - " + failure.HowToResolve);
            }             
        }
        return isValidLicense;       
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
                //Console.WriteLine($"Page {page.Number}:");
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
        
        Dictionary<string, string> extractedValues = new Dictionary<string, string>();
        extractedValues["User Name"]=userName;        
        try
        {
            string textContent = ExtractTextFromPdf(pdfPath);            
            var parser = new CarInfoParser();
            CarInfo car = parser.Parse(textContent);
            string generalRegEx = @":\s*(.+)";
            
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
            beforeText = inputText.Substring(0, index).Trim();
        else
        {
            Log($"[ERROR] Marker not found in text: {marker}");
            //Console.WriteLine("Marker not found in text.");
        }
        return beforeText;
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
                formData["emailAddress"] = email;
                foreach (var kvp in formData)
                {
                    Log($"{kvp.Key} -> {kvp.Value}");
                    //Console.WriteLine($"{kvp.Key} -> {kvp.Value}");
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
        //Console.WriteLine(logEntry);
        File.AppendAllText($"{logFileFolderPath}\\{logFileName}", logEntry + Environment.NewLine);
    }

    static string GetDeviceId()
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
    }
}