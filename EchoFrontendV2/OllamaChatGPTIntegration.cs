using System.Diagnostics;
using System.Threading.Tasks;
namespace EchoFrontendV2
{
    public class OllamaChatGPTIntegration
    {
        
        public async Task<string> GetChatGPTResponse(string messageToSend)
        {
            string pythonScriptPath = "C:\\Users\\JimBu\\Downloads\\ollamaChatTest.py"; // Replace with the actual path
            string arguments = $"\"{messageToSend}\""; // Escape quotes if necessary

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "python", // Or the full path to your python executable (e.g., "C:\\Python39\\python.exe")
                Arguments = $"{pythonScriptPath} {arguments}",
                RedirectStandardOutput = true,
                RedirectStandardError = true, // To capture any Python errors
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process process = new Process())
            {
                process.StartInfo = psi;
                process.Start();

                string chatGPTResponse = await process.StandardOutput.ReadToEndAsync();
                string errorOutput = await process.StandardError.ReadToEndAsync(); // Capture errors

                await process.WaitForExitAsync();

                if (!string.IsNullOrEmpty(errorOutput))
                {
                    // Handle any errors that occurred in the Python script
                    Console.WriteLine($"Python Error: {errorOutput}");
                    return null; // Or throw an exception
                }

                return chatGPTResponse.Trim();
            }
        }

        // ... your Ollama interaction logic ...
    }
}
