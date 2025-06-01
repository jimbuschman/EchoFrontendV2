//using Microsoft.AspNetCore.Mvc.Routing;
using EchoFrontendV2;
using EchoFrontendV2.DTO;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using File = System.IO.File;


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

        //public static string URL = @"http://localhost:11434/";
        public static void Setup(RealtimeLogger logger)
        {
            _logger = logger;
            //var budget = 32000;

            memoryManager = new MemoryManager(_logger, globalBudget: budget);
            memoryManager.ConfigurePool("Core", percentage: 0.10, 0, hardCap: 2048); //static system
            memoryManager.ConfigurePool("ActiveSession", percentage: 0.35, 3); // current session
            memoryManager.ConfigurePool("RecentHistory", percentage: 0.15, 2); // older summarized session entries.
            memoryManager.ConfigurePool("Recall", percentage: 0.30, 1, hardCap: 8192); // memories
            memoryManager.ConfigurePool("Buffer", percentage: 0.10, 1);
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
                _logger.LogTrack("Previous Sessions:");
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
                    _logger.LogTrack("-" + s.Summary);
                }
            }
        }

        public static readonly HttpClient httpClient = new HttpClient()
        {
            BaseAddress = new Uri("http://localhost:11434/"),
            Timeout = TimeSpan.FromSeconds(60)
        };
        public static string EncodeImageToBase64(string imagePath)
        {
            try
            {
                byte[] imageBytes = File.ReadAllBytes(imagePath);
                return Convert.ToBase64String(imageBytes);
            }
            catch (Exception ex)
            {
                _logger.LogException($"OllamaChat:EncodeImageToBase64(): Error reading or encoding image: {ex.Message}");
                return null;
            }
        }



        // Simple test functions
        public static string GetCurrentTime()
        {
            return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        public static string AddNumbers(int a, int b)
        {
            return (a + b).ToString();
        }

        public static string GetWeather(string city)
        {
            // Mock weather function
            return $"The weather in {city} is sunny with 72°F";
        }

        public static string HandleToolCall(ToolCall tool)
        {
            try
            {
                switch (tool.Name)
                {
                    case "get_current_time":
                        return GetCurrentTime();
                    case "get_framework_summaries"://(filter: str = '', limit: int = 10)
                        var items = Form1.Database.GetFrameworkInfo();
                        var json = JsonSerializer.Serialize(items.Select(f => new
                        {
                            f.Id,
                            f.Name,
                            f.Description
                        }).ToList());
                        return json;
                    case "get_full_framework(id: int)":
                        var data = Form1.Database.GetFrameworkById((int)tool.Arguments["id"]);
                        return data;
                    default:
                        return $"Unknown function: {tool.Name}";
                }
            }
            catch (Exception ex)
            {
                return $"Error executing function {tool.Name}: {ex.Message}";
            }
        }
        public static async Task<string> GetMessage(string message)
        {
            int userTokens = TokenEstimator.EstimateTokens(message);
            int maxContext = budget; // or whatever your model supports
            int availableForMemory = maxContext - userTokens - MemoryManager.OVERHEAD_TOKENS;

            // Step 1: Gather all context for injection
            var memoryContext = memoryManager.GatherMemory(tokenBudget: availableForMemory);
            _logger.LogTrack("available tokens: " + availableForMemory);

            // Step 2: Build the full message list
            var sessionMessages = new List<SessionMessage>();

            // Add memory messages
            StringBuilder mems = new StringBuilder();
            List<string> names = new List<string>();
            //memoryManager.ConfigurePool("Core", percentage: 0.10, 0, hardCap: 2048); //static system
            //memoryManager.ConfigurePool("ActiveSession", percentage: 0.35, 3); // current session
            //memoryManager.ConfigurePool("RecentHistory", percentage: 0.15, 2); // older summarized session entries.
            //memoryManager.ConfigurePool("Recall", percentage: 0.30, 1, hardCap: 8192); // memories
            //memoryManager.ConfigurePool("Buffer", percentage: 0.10, 1);
            foreach (var mem in memoryContext)
            {
                if (!names.Contains(mem.PoolName))
                {
                    names.Add(mem.PoolName);
                    if (mem.PoolName == "Lessons")
                    {
                        mems.AppendLine("RelaventLessons:");
                    }
                    else if (mem.PoolName == "Recall")
                    {
                        mems.AppendLine("RelaventMemory:");
                    }
                    else if (mem.PoolName == "RecentHistory" || mem.PoolName == "Buffer")
                    {
                        mems.AppendLine("RecentHisory:");
                    }
                    else if (mem.PoolName == "Core")
                    {
                        mems.AppendLine("CoreFoundation:");
                    }
                    else if (mem.PoolName == "ActiveSession")
                    {
                        mems.AppendLine("ActiveSession:");
                    }
                }
                mems.AppendLine("   - " + mem.Text);
            }
            sessionMessages.Add(new SessionMessage("system", mems.ToString()));

            sessionMessages.Add(new SessionMessage("system", @"
                        You can use the following tools by generating a JSON block named `tool_call`. Specify the tool name and arguments clearly.

                        The system will respond with a `tool_output` block containing the result. Use that result to reason or take further steps.

                        Available tools:
                        - get_current_time()
                        - get_framework_summaries()
                            → Returns a list of framework summaries including id, name, and description. Use this to browse framework entries.

                        - get_full_framework(id: int)
                            → Returns the full text a specific framework. Use this after selecting a relevant framework from the summaries.
                       
                        Only call defined tools. If no tool is needed, respond normally."));


            //if (!string.IsNullOrWhiteSpace(toolResponse))
            //{
            //    string rawJson = toolResponse.Trim(); // Removes \r\n or extra whitespace

            //    //string wrappedToolResponse = $"```json\n{rawJson}\n```";
            //    var toolMessage = new SessionMessage("assistant", "Here is the tool output: " + rawJson);
            //    sessionMessages.Add(toolMessage);
            //}
            ////else
            //{

            //    var userMessage = new SessionMessage("user", message);

            //    // Add image if imagePath is provided and not null/empty
            //    if (!string.IsNullOrEmpty(imagePath))
            //    {
            //        string base64Image = EncodeImageToBase64(imagePath);
            //        if (base64Image != null)
            //        {
            //            // Ollama expects 'images' as an array of base64 strings
            //            userMessage.Images = new List<string> { base64Image };
            //        }
            //        else
            //        {
            //            // Log an error if image encoding failed
            //            _logger.LogWarning($"OllamaChat:SendMessageToOllama(): Failed to encode image at path: {imagePath}. Sending text message only.");
            //        }
            //    }
            //    sessionMessages.Add(userMessage);
            //}


            return mems.ToString();// JsonSerializer.Serialize(sessionMessages, new JsonSerializerOptions { WriteIndented = true });
        }
        public static async Task<string> SendMessageToOllama(string message, string toolResponse, bool fromChatGPT, Action<string> updateUI, string imagePath = null)
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
            StringBuilder mems = new StringBuilder();
            List<string> names = new List<string>();
            //memoryManager.ConfigurePool("Core", percentage: 0.10, 0, hardCap: 2048); //static system
            //memoryManager.ConfigurePool("ActiveSession", percentage: 0.35, 3); // current session
            //memoryManager.ConfigurePool("RecentHistory", percentage: 0.15, 2); // older summarized session entries.
            //memoryManager.ConfigurePool("Recall", percentage: 0.30, 1, hardCap: 8192); // memories
            //memoryManager.ConfigurePool("Buffer", percentage: 0.10, 1);
            foreach (var mem in memoryContext)
            {
                if (!names.Contains(mem.PoolName))
                {
                    names.Add(mem.PoolName);
                    if (mem.PoolName == "Lessons")
                    {
                        mems.AppendLine("RelaventLessons:");
                    }
                    else if (mem.PoolName == "Recall")
                    {
                        mems.AppendLine("RelaventMemory:");
                    }
                    else if (mem.PoolName == "RecentHistory" || mem.PoolName == "Buffer")
                    {
                        mems.AppendLine("RecentHisory:");
                    }
                    else if (mem.PoolName == "Core")
                    {
                        mems.AppendLine("CoreFoundation:");
                    }
                    else if (mem.PoolName == "ActiveSession")
                    {
                        mems.AppendLine("ActiveSession:");
                    }
                }
                mems.AppendLine("   - " + mem.Text);
            }
            sessionMessages.Add(new SessionMessage("system", mems.ToString()));

            sessionMessages.Add(new SessionMessage("system", @"
                        You can use the following tools by generating a JSON block named `tool_call`. Specify the tool name and arguments clearly.

                        The system will respond with a `tool_output` block containing the result. Use that result to reason or take further steps.

                        Available tools:
                        - get_current_time()
                        - get_framework_summaries()
                            → Returns a list of framework summaries including id, name, and description. Use this to browse framework entries.

                        - get_full_framework(id: int)
                            → Returns the full text a specific framework. Use this after selecting a relevant framework from the summaries.
                       
                        Only call defined tools. If no tool is needed, respond normally."));


            if (!string.IsNullOrWhiteSpace(toolResponse))
            {
                string rawJson = toolResponse.Trim(); // Removes \r\n or extra whitespace

                //string wrappedToolResponse = $"```json\n{rawJson}\n```";
                var toolMessage = new SessionMessage("assistant", "Here is the tool output: " + rawJson);
                sessionMessages.Add(toolMessage);
            }
            //else
            {

                var userMessage = new SessionMessage("user", message);

                // Add image if imagePath is provided and not null/empty
                if (!string.IsNullOrEmpty(imagePath))
                {
                    string base64Image = EncodeImageToBase64(imagePath);
                    if (base64Image != null)
                    {
                        // Ollama expects 'images' as an array of base64 strings
                        userMessage.Images = new List<string> { base64Image };
                    }
                    else
                    {
                        // Log an error if image encoding failed
                        _logger.LogWarning($"OllamaChat:SendMessageToOllama(): Failed to encode image at path: {imagePath}. Sending text message only.");
                    }
                }
                sessionMessages.Add(userMessage);
            }

            var payload = new
            {
                messages = sessionMessages,
                model = "gemma3:latest",
                stream = true,
                options = new
                {
                    num_ctx = 32768,
                    num_gpu = 1 // Forces Ollama to use GPU
                }
            };
            var payloadJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
            _logger.LogTrack($"🟨 Payload to Ollama:\n{payloadJson}");
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

                bool inCodeBlock = false;
                var codeBlockBuilder = new StringBuilder();
                List<SessionMessage> injectedMessages = new();
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
                                    if (textChunk.Trim() == "```")
                                    {
                                        if (!inCodeBlock)
                                        {
                                            inCodeBlock = true;
                                            codeBlockBuilder.Clear();
                                            continue;
                                        }
                                        else
                                        {
                                            inCodeBlock = false;

                                            var rawJson = codeBlockBuilder.ToString().Trim();
                                            if (rawJson.StartsWith("json", StringComparison.OrdinalIgnoreCase))
                                                rawJson = rawJson.Substring(4).TrimStart();
                                            _logger.LogTrack($"[Parsed Code Block]:\n{rawJson}");

                                            if (rawJson.Contains("tool_call"))
                                            {
                                                try
                                                {
                                                    if (rawJson.StartsWith("tool_call"))
                                                    {
                                                        int start = rawJson.IndexOf('{');
                                                        if (start != -1)
                                                        {
                                                            var objectPart = rawJson.Substring(start).Trim();
                                                            rawJson = $"{{\"tool_call\": {objectPart}}}";
                                                        }
                                                    }
                                                    using var doc3 = JsonDocument.Parse(rawJson);
                                                    if (doc3.RootElement.TryGetProperty("tool_call", out var toolCallProp))
                                                    {
                                                        var toolName = toolCallProp.GetProperty("tool_name").GetString();
                                                        Dictionary<string, object> args = new Dictionary<string, object>();
                                                        if (toolCallProp.TryGetProperty("arguments", out var tempargs) && tempargs.ValueKind == JsonValueKind.Object)
                                                        {
                                                            foreach (var p in tempargs.EnumerateObject())
                                                            {
                                                                try
                                                                {
                                                                    args[p.Name] = JsonSerializer.Deserialize<object>(p.Value.GetRawText());
                                                                }
                                                                catch (Exception ex)
                                                                {
                                                                    _logger.LogWarning($"Failed to deserialize argument '{p.Name}': {ex.Message}");
                                                                    args[p.Name] = null;
                                                                }
                                                            }
                                                        }

                                                        var toolCall = new ToolCall { Name = toolName, Arguments = args };
                                                        var result = HandleToolCall(toolCall);
                                                        _logger.LogTrack($"✅ Parsed tool_call: {toolCall.Name}");
                                                        var output = new ToolOutput
                                                        {
                                                            Payload = new ToolOutputPayload
                                                            {
                                                                Name = toolName,
                                                                Result = result
                                                            }
                                                        };
                                                        var json = JsonSerializer.Serialize(output);
                                                        injectedMessages.Add(new SessionMessage("assistant", json));
                                                        continue;
                                                    }
                                                }
                                                catch (JsonException ex)
                                                {
                                                    injectedMessages.Add(new SessionMessage("system", @"Your tool_call output must be a valid JSON object with the structure: { ""tool_call"": { ... } }"));
                                                    _logger.LogException($"❌ Tool JSON parse failed: {ex.Message}");
                                                }

                                                continue;
                                            }

                                            // Skip printing code block to user
                                            continue;
                                        }
                                    }
                                    else if (inCodeBlock)
                                    {
                                        codeBlockBuilder.Append(textChunk.Trim());
                                    }

                                    if (!inCodeBlock)
                                    {
                                        fullResponse.Append(textChunk);
                                        // Update the UI using the provided action
                                        updateUI(textChunk);
                                    }

                                }
                            }

                            if (doc.RootElement.TryGetProperty("done", out var doneProp) &&
                                doneProp.GetBoolean())
                            {
                                if (fullResponse == null)
                                    fullResponse = new StringBuilder();
                                foreach (var injected in injectedMessages)
                                {
                                    fullResponse.AppendLine(); // optional visual separation
                                    fullResponse.Append(injected.Content);
                                    //updateUI(injected.Content);
                                }
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
