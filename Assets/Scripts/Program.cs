using IronPython.Hosting;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace CSHttpClientSample
{
    static class Program
    {
        // Add your Computer Vision subscription key and endpoint to your environment variables.
        static string subscriptionKey = "99fb915c97b549ca8ccf1ae20b29940e";

        // 
        static string endpoint = "https://holoboard.cognitiveservices.azure.com/";

        static string imageFilePath = @"C:\Users\aearl\Documents\GitHub\WIN_20191012_15_08_04_Pro.jpg";
        static void Main()
        {
            var pythonRes = RunPythonCodeString("print(5)");
            var res = ExtractTextLocal(imageFilePath).Result;
        }

        public static string RunPythonCodeString(string input)
        {
            // Save the string as a file
            var fileLocation = savePythonCode(input);

            // Run the code and get the output
            var result = runPythonFile(fileLocation);
            return result;
        }

        private static string savePythonCode(string input)
        {
            var fileLocation = AppDomain.CurrentDomain.BaseDirectory + @"\pythonCode.py";
            File.WriteAllText(fileLocation, input);
            return fileLocation;
        }

        private static string runPythonFile(string fileLocation)
        {
            var txtWriter = new StringWriter();
            var stream = new MemoryStream();
            var engine = Python.CreateEngine();
            engine.Runtime.IO.SetOutput(stream, txtWriter);
            engine.Runtime.IO.SetErrorOutput(stream, txtWriter);
            engine.ExecuteFile(fileLocation);
            return txtWriter.ToString();
        }

        public static async Task<string> ExtractTextLocal(string localImage)
        {
            var creds = new ApiKeyServiceClientCredentials(subscriptionKey);
            var client = new ComputerVisionClient(creds)
            {
                Endpoint = endpoint
            };

            // Helps calucalte starting index to retrieve operation ID
            const int numberOfCharsInOperationId = 36;
            using (Stream imageStream = File.OpenRead(localImage))
            {
                // Read the text from the local image
                BatchReadFileInStreamHeaders localFileTextHeaders = await client.BatchReadFileInStreamAsync(imageStream);
                // Get the operation location (operation ID)
                string operationLocation = localFileTextHeaders.OperationLocation;

                // Retrieve the URI where the recognized text will be stored from the Operation-Location header.
                string operationId = operationLocation.Substring(operationLocation.Length - numberOfCharsInOperationId);

                // Extract text, wait for it to complete.
                int i = 0;
                int maxRetries = 10;
                ReadOperationResult results;
                do
                {
                    results = await client.GetReadOperationResultAsync(operationId);
                    await Task.Delay(1000);
                    if (maxRetries == 9)
                    {
                        Console.WriteLine("Server timed out.");
                    }
                }
                while ((results.Status == TextOperationStatusCodes.Running ||
                        results.Status == TextOperationStatusCodes.NotStarted) && i++ < maxRetries);

                // Display the found text.
                Console.WriteLine();
                var textRecognitionLocalFileResults = results.RecognitionResults;
                var result = "";
                foreach (TextRecognitionResult recResult in textRecognitionLocalFileResults)
                {
                    foreach (Line line in recResult.Lines)
                    {
                        result += line.Text + "\n";
                    }
                }
                return result;
            }
        }
    }
}
