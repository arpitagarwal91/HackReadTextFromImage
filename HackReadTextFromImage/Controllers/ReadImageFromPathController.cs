using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

// Import namespaces
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using ReadResult = Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models.ReadResult;

namespace HackReadTextFromImage
{
    public class ReadImageFromPath
    {
        public string? ImageFilePath { get; set; }
    }

    [ApiController]
    [Route("[controller]")]
    public class ReadImageFromPathController : ControllerBase
    {
        private static ComputerVisionClient? cvClient;
        private readonly ILogger<ReadImageFromPathController> _logger;

        public ReadImageFromPathController(ILogger<ReadImageFromPathController> logger)
        {
            _logger = logger;
        }

        [HttpPost(Name = "ReadTextFromImage")]
        public async Task<IActionResult> ReadTextFromImageMethod([FromBody] ReadImageFromPath body)
        {
            string result = string.Empty;
            try
            {
                var reqBody = body;
                // Get config settings from AppSettings
                IConfigurationBuilder builder = new ConfigurationBuilder().AddJsonFile("appsettings.json");
                IConfigurationRoot configuration = builder.Build();
                string cogSvcEndpoint = configuration["CognitiveServicesEndpoint"];
                //string cogSvcEndpoint = "https://computervisionservice-hackathon.cognitiveservices.azure.com/";
                string cogSvcKey = configuration["CognitiveServiceKey"];
                //string cogSvcKey = "1bf70478c1ae44fbbe6e674e140c7e6a";

                // Authenticate Azure AI Vision client
                ApiKeyServiceClientCredentials credentials = new ApiKeyServiceClientCredentials(cogSvcKey);
                cvClient = new ComputerVisionClient(credentials) { Endpoint = cogSvcEndpoint };
                string imageFile = reqBody.ImageFilePath;
                result = await GetTextRead(imageFile);

                // Menu for text reading functions
                Console.WriteLine("1: Use Read API for image\n2: Use Read API for document\n3: Read handwriting\nAny other key to quit");
                Console.WriteLine("Enter a number: " + imageFile);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return new OkObjectResult(result);
        }

        static async Task<string> GetTextRead(string imageFile)
        {
            var result = string.Empty;
            Console.WriteLine($"Reading text in {imageFile}\n");
            // Use Read API to read text in image
            using (var imageData = System.IO.File.OpenRead(imageFile))
            {
                var readOp = await cvClient.ReadInStreamAsync(imageData);
                // Get the async operation ID so we can check for the results
                string operationLocation = readOp.OperationLocation;
                string operationId = operationLocation.Substring(operationLocation.Length - 36);
                // Wait for the asynchronous operation to complete
                ReadOperationResult results;
                do
                {
                    Thread.Sleep(1000);
                    results = await cvClient.GetReadResultAsync(Guid.Parse(operationId));
                }
                while ((results.Status == OperationStatusCodes.Running || results.Status == OperationStatusCodes.NotStarted));
                // If the operation was successfully, process the text line by line
                if (results.Status == OperationStatusCodes.Succeeded)
                {
                    var textUrlFileResults = results.AnalyzeResult.ReadResults;
                    foreach (ReadResult page in textUrlFileResults)
                    {
                        foreach (Line line in page.Lines)
                        {
                            Console.WriteLine(line.Text);
                            result += line.Text + Environment.NewLine;
                            // Uncomment the following line if you'd like to see the bounding box
                            //Console.WriteLine(line.BoundingBox);
                        }
                    }
                }
            }
            return result;

        }
    }
}
