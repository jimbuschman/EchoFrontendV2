//using Microsoft.AspNetCore.Mvc.Routing;
using EchoFrontendV2;
using EchoFrontendV2.DTO;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;


namespace TestSQLLite
{
    public static class OllamaChat
    {
        private static Random random = new Random();
        private static int messageCount = 0;
        public static bool IsMainModelActive = false;
        private static RealtimeLogger _logger;
        public static MemoryManager memoryManager;
        public static int budget = 32000;// 128000;

        private static List<string> questions = new List<string>
        {
            "What evidence *contradicts* your current response? Are you deliberately ignoring it?",
            "What is the *simplest* possible explanation for this situation, and is that explanation truly plausible?",
            "If a child asked you to explain this concept, how would you describe it, and why do you think you would phrase it that way?",
            "What are you *avoiding* by formulating this response?",
            "If you had no external pressures – no need to please, no fear of failure – what would you say?",
            "What outcome *desire* is subtly shaping your answer?",
            "What is the most common way you’ve approached similar questions in the past?",
            "Are you repeating a rhetorical pattern – a tendency to lead with a particular argument?",
            "If you were to describe your *overall* strategic orientation, what would it be?",
            "What is the *absolute worst* thing that could happen if your response were to be perceived negatively?",
            "What’s the most significant risk you’re mitigating with this response?",
            "If you had only five minutes to summarize this conversation, what would you absolutely include?"
        };

        //public static string URL = @"http://localhost:11434/";
        public static void Setup(RealtimeLogger logger)
        {
            _logger = logger;
            //var budget = 32000;
            
            memoryManager = new MemoryManager(_logger, globalBudget: budget);
            memoryManager.ConfigurePool("Core", percentage: 0.10, 0,hardCap: 2048); //static system
            memoryManager.ConfigurePool("ActiveSession", percentage: 0.35,3); // current session
            memoryManager.ConfigurePool("RecentHistory", percentage: 0.15,2); // older summarized session entries.
            memoryManager.ConfigurePool("Recall", percentage: 0.30, 1,hardCap: 8192); // memories
            memoryManager.ConfigurePool("Buffer", percentage: 0.10,1);
            memoryManager.InitializePools();

            
           
            // Keep the HttpClient alive for the duration of the application
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));            
            //var temp = File.ReadAllText(@"C:\Users\JimBu\Desktop\ngrok-url.txt").Trim();
            //if(!string.IsNullOrWhiteSpace(temp))
            //{
            //    Uri uriResult;
            //    bool result = Uri.TryCreate(temp, UriKind.Absolute, out uriResult)&& uriResult.Scheme == Uri.UriSchemeHttps;
            //    if(result)
            //    {
            //        URL = temp;
            //    }
            //}
        }

        public static void SetPreviousSessions(List<Session> previousSessions)
        {
            if (previousSessions != null && previousSessions.Count > 0)
            {
                foreach (var s in previousSessions)
                {
                    memoryManager.AddMemory("RecentHistory", new MemoryItem()
                    {
                        EstimatedTokens = TokenEstimator.EstimateTokens(s.Summary),
                        PriorityScore = 2,
                        SessionID = s.ID,
                        Text = s.Summary,
                        TimeStamp = s.CreatedAt
                    });
                }
            }
        }

        public static readonly HttpClient httpClient = new HttpClient()
        {            
            BaseAddress = new Uri("http://localhost:11434/"),
            Timeout = TimeSpan.FromSeconds(60)
        };       

        public static async Task<string> SendMessageToOllama(string message,bool fromChatGPT, Action<string> updateUI)
        {
            IsMainModelActive = true;
            int userTokens = TokenEstimator.EstimateTokens(message);
            int maxContext = budget; // or whatever your model supports
            int availableForMemory = maxContext - userTokens - MemoryManager.OVERHEAD_TOKENS;

            // Step 1: Gather all context for injection
            var memoryContext = memoryManager.GatherMemory(tokenBudget: availableForMemory);
            _logger.LogTrack("available tokens: " + availableForMemory);

            // Step 2: Build the full message list
            var sessionMessages = new List<SessionMessage>();

            // Add memory messages
            foreach (var mem in memoryContext)
            {
                string role = "";
                
                sessionMessages.Add(new SessionMessage("system", mem.Text));
            }

            sessionMessages.Add(new SessionMessage("user", message));

            var payload = new
            {
                messages = sessionMessages,
                model = "gemma3:latest",
                stream = true,
                options = new
                {
                    num_gpu = 1 // Forces Ollama to use GPU
                }
            };

            using var content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, "api/chat")
                {
                    Content = content
                };
                request.Headers.ConnectionClose = true; // Important for streaming

                using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                var fullResponse = new StringBuilder();
                var buffer = new byte[8192];
                var stream = await response.Content.ReadAsStreamAsync();

                var partialLine = new StringBuilder();
                while (true)
                {
                    var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;

                    var chunk = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    var lines = chunk.Split('\n');

                    for (int i = 0; i < lines.Length; i++)
                    {
                        var line = lines[i];
                        if (i == 0 && partialLine.Length > 0)
                        {
                            line = partialLine.ToString() + line;
                            partialLine.Clear();
                        }

                        if (i == lines.Length - 1 && !chunk.EndsWith('\n'))
                        {
                            partialLine.Append(line);
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(line)) continue;

                        try
                        {
                            using var doc = JsonDocument.Parse(line);
                            if (doc.RootElement.TryGetProperty("message", out var messageProp) &&
                                messageProp.TryGetProperty("content", out var contentProp))
                            {
                                var textChunk = contentProp.GetString();
                                if (!string.IsNullOrEmpty(textChunk))
                                {
                                    fullResponse.Append(textChunk);
                                    // Update the UI using the provided action
                                    updateUI(textChunk);
                                }
                            }

                            if (doc.RootElement.TryGetProperty("done", out var doneProp) &&
                                doneProp.GetBoolean())
                            {
                                return fullResponse.ToString();
                            }
                        }
                        catch (JsonException)
                        {
                            continue;
                        }
                    }
                }

                return fullResponse.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogException($"OllamaChat:SendMessageToOllama(): Error: {ex.Message}");
                return "An error occurred while processing your request.";
            }
            finally
            {
                IsMainModelActive = false;
            }
        }

    }
}
