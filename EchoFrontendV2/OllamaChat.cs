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
                _logger.LogMessage("Previous Sessions:");
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
                    _logger.LogMessage("-" + s.Summary);
                }
            }
        }

        public static readonly HttpClient httpClient = new HttpClient()
        {
            BaseAddress = new Uri("http://localhost:11434/"),
            Timeout = TimeSpan.FromSeconds(60)
        };
        public static List<ToolMetadata> ToolRegistry = new()
        {
            new ToolMetadata
            {
                Name = "get_current_time",
                Description = "Returns the current server time.",
                Arguments = new()
            },
            new ToolMetadata
            {
                Name = "get_framework_summaries",
                Description = "Returns a list of framework summaries.",
                Arguments = new()
                {
                    { "filter", ("string", "Filter by name or keyword (optional)", false) },
                    { "limit", ("int", "Max number of results (optional)", false) }
                }
            },
            new ToolMetadata
            {
                Name = "get_full_framework",
                Description = "Returns the full text of a specific framework.",
                Arguments = new()
                {
                    { "id", ("int", "The framework ID (required)", true) }
                }
            }
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
                    case "get_full_framework":// (id: int)":
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
            _logger.LogMessage("available tokens: " + availableForMemory);

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
        public static async Task<string> SendMessageToOllama2(string message, string toolResponse, bool fromChatGPT, Action<string> updateUI, string imagePath = null)
        {
            IsMainModelActive = true;
            int userTokens = TokenEstimator.EstimateTokens(message);
            int maxContext = budget; // or whatever your model supports
            int availableForMemory = maxContext - userTokens - MemoryManager.OVERHEAD_TOKENS;

            // Step 1: Gather all context for injection
            var memoryContext = memoryManager.GatherMemory(tokenBudget: availableForMemory);
            _logger.LogMessage("available tokens: " + availableForMemory);

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
            _logger.LogMessage($"🟨 Payload to Ollama:\n{payloadJson}");
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
                                            _logger.LogMessage($"[Parsed Code Block]:\n{rawJson}");

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
                                                        _logger.LogMessage($"✅ Parsed tool_call: {toolCall.Name}");
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

        // Helper: Read LLM stream and accumulate output
        private static async Task<string> ReadllmStream(
            Stream stream,
            byte[] buffer,
            StringBuilder partialLine,
            Action<string> updateUI,
            StringBuilder fullResponse)
        {
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
                                updateUI(textChunk);
                                return textChunk;
                            }
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

        // Helper: Try to parse a tool call from LLM output
        private static bool TryParseToolCall(string output, out ToolCall toolCall)
        {
            toolCall = null;
            try
            {
                using var doc = JsonDocument.Parse(output);
                if (doc.RootElement.TryGetProperty("tool_call", out var toolCallProp))
                {
                    var toolName = toolCallProp.GetProperty("tool_name").GetString();
                    var args = new Dictionary<string, object>();
                    if (toolCallProp.TryGetProperty("arguments", out var tempargs) && tempargs.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var p in tempargs.EnumerateObject())
                            args[p.Name] = JsonSerializer.Deserialize<object>(p.Value.GetRawText());
                    }
                    toolCall = new ToolCall { Name = toolName, Arguments = args };
                    return true;
                }
            }
            catch { }
            return false;
        }
        public static string GenerateToolPrompt()
        {
            var sb = new StringBuilder();
            sb.AppendLine("You can use the following tools by responding with a JSON object as shown.");
            foreach (var tool in ToolRegistry)
            {
                sb.AppendLine($"- {tool.Name}");
                sb.AppendLine($"  - Description: {tool.Description}");
                if (tool.Arguments.Any())
                {
                    sb.AppendLine("  - Arguments:");
                    foreach (var arg in tool.Arguments)
                        sb.AppendLine($"      - {arg.Key} ({arg.Value.Type}){(arg.Value.Required ? " [required]" : "")}: {arg.Value.Description}");
                }
                else
                {
                    sb.AppendLine("  - Arguments: none");
                }
                sb.AppendLine();
            }
            sb.AppendLine("Example tool call:");
            sb.AppendLine("{ \"tool_call\": { \"tool_name\": \"get_full_framework\", \"arguments\": { \"id\": 42 } } }");
            sb.AppendLine("If you need to use a tool, respond ONLY with the JSON object. If not, respond normally.");
            return sb.ToString();
        }

        public static async Task<string> SendMessageToOllama(
            string message,
            string toolResponse,
            bool fromChatGPT,
            Action<string> updateUI,
            string imagePath = null)
        {
            IsMainModelActive = true;
            try
            {
                int userTokens = TokenEstimator.EstimateTokens(message);
                int maxContext = budget;
                int availableForMemory = maxContext - userTokens - MemoryManager.OVERHEAD_TOKENS;

                // Gather context
                var memoryContext = memoryManager.GatherMemory(tokenBudget: availableForMemory);

                // Build dynamic system prompt
                var systemPrompt = GenerateToolPrompt();

                // Build message list
                var sessionMessages = new List<SessionMessage>
                {
                    new SessionMessage("system", systemPrompt)
                };

                // Add memory context
                var mems = new StringBuilder();
                foreach (var mem in memoryContext)
                    mems.AppendLine($"[{mem.PoolName}] {mem.Text}");
                sessionMessages.Add(new SessionMessage("system", mems.ToString()));

                // Add user message
                var userMessage = new SessionMessage("user", message);
                if (!string.IsNullOrEmpty(imagePath))
                {
                    string base64Image = EncodeImageToBase64(imagePath);
                    if (base64Image != null)
                        userMessage.Images = new List<string> { base64Image };
                }
                sessionMessages.Add(userMessage);

                // Add tool response if present
                if (!string.IsNullOrWhiteSpace(toolResponse))
                    sessionMessages.Add(new SessionMessage("assistant", toolResponse));

                // Prepare payload
                var payload = new
                {
                    messages = sessionMessages,
                    model = "gemma3:latest",
                    stream = true,
                    options = new { num_ctx = 32768, num_gpu = 1 }
                };

                using var content = new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json");

                var request = new HttpRequestMessage(HttpMethod.Post, "api/chat") { Content = content };
                request.Headers.ConnectionClose = true;

                using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                var stream = await response.Content.ReadAsStreamAsync();
                var buffer = new byte[8192];
                var fullResponse = new StringBuilder();
                var partialLine = new StringBuilder();

                // Tool call loop: keep processing tool calls until LLM stops requesting them
                while (true)
                {
                    string llmOutput = await ReadllmStream(stream, buffer, partialLine, updateUI, fullResponse);

                    // Try to detect one or more tool calls in the output
                    if (TryParseToolCalls(llmOutput, out List<ToolCall> toolCalls) && toolCalls.Count > 0)
                    {
                        foreach (var toolCall in toolCalls)
                        {
                            var (isValid, error) = ValidateToolCall(toolCall);
                            if (!isValid)
                            {
                                // Inject error feedback and let LLM retry
                                var errorMsg = $"Tool call error: {error}. Please check your tool call JSON and try again.";
                                _logger.LogWarning(errorMsg);
                                sessionMessages.Add(new SessionMessage("system", errorMsg));
                                break; // Only process one error at a time
                            }
                            // Notify UI about tool execution
                            updateUI($"[Executing tool: {toolCall.Name}]");

                            // Execute tool and inject result
                            string toolResult = HandleToolCall(toolCall);
                            _logger.LogMessage($"Tool call: {toolCall.Name} | Args: {JsonSerializer.Serialize(toolCall.Arguments)} | Result: {toolResult}");
                            var toolOutput = new ToolOutput
                            {
                                Payload = new ToolOutputPayload
                                {
                                    Name = toolCall.Name,
                                    Result = toolResult
                                }
                            };
                            string toolJson = JsonSerializer.Serialize(toolOutput);

                            // Add tool output as assistant message
                            sessionMessages.Add(new SessionMessage("tool_output", toolJson));
                        }

                        // Rebuild payload and resend
                        payload = new
                        {
                            messages = sessionMessages,
                            model = "gemma3:latest",
                            stream = true,
                            options = new { num_ctx = 32768, num_gpu = 1 }
                        };
                        using var newContent = new StringContent(
                            JsonSerializer.Serialize(payload),
                            Encoding.UTF8,
                            "application/json");
                        request.Content = newContent;
                        using var newResponse = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                        newResponse.EnsureSuccessStatusCode();
                        stream = await newResponse.Content.ReadAsStreamAsync();
                        continue;
                    }
                    else
                    {
                        // Proactive tool suggestion: If the LLM's response is vague, suggest tool use
                        if (IsVagueOrIncomplete(llmOutput))
                        {
                            var suggestion = "If you need more information, consider using one of the available tools by responding with a tool_call JSON as described above.";
                            _logger.LogMessage("Proactive tool suggestion injected.");
                            sessionMessages.Add(new SessionMessage("system", suggestion));
                            payload = new
                            {
                                messages = sessionMessages,
                                model = "gemma3:latest",
                                stream = true,
                                options = new { num_ctx = 32768, num_gpu = 1 }
                            };
                            using var suggestionContent = new StringContent(
                                JsonSerializer.Serialize(payload),
                                Encoding.UTF8,
                                "application/json");
                            request.Content = suggestionContent;
                            using var suggestionResponse = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                            suggestionResponse.EnsureSuccessStatusCode();
                            stream = await suggestionResponse.Content.ReadAsStreamAsync();
                            continue;
                        }

                        // No tool call detected, return final response
                        return fullResponse.ToString();
                    }
                }
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

        // Helper: Detect if the LLM's output is vague or incomplete (simple heuristic)
        private static bool IsVagueOrIncomplete(string output)
        {
            if (string.IsNullOrWhiteSpace(output)) return true;
            string[] vaguePhrases = new[]
            {
                "I'm not sure", "I don't know", "cannot answer", "need more information", "unsure", "unknown", "no data", "not enough information"
            };
            foreach (var phrase in vaguePhrases)
                if (output.IndexOf(phrase, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            // Optionally: check for very short responses
            if (output.Trim().Length < 20) return true;
            return false;
        }
        public static (bool IsValid, string Error) ValidateToolCall(ToolCall call)
        {
            var tool = ToolRegistry.FirstOrDefault(t => t.Name == call.Name);
            if (tool == null)
                return (false, $"Unknown tool: {call.Name}");

            foreach (var arg in tool.Arguments)
            {
                if (arg.Value.Required && (!call.Arguments?.ContainsKey(arg.Key) ?? true))
                    return (false, $"Missing required argument: {arg.Key}");
                // Optionally: type checking
            }
            return (true, null);
        }
        private static bool TryParseToolCalls(string output, out List<ToolCall> toolCalls)
        {
            toolCalls = new();
            try
            {
                using var doc = JsonDocument.Parse(output);
                if (doc.RootElement.TryGetProperty("tool_call", out var toolCallProp))
                {
                    if (toolCallProp.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in toolCallProp.EnumerateArray())
                        {
                            var call = ParseToolCall(item);
                            if (call != null) toolCalls.Add(call);
                        }
                    }
                    else
                    {
                        var call = ParseToolCall(toolCallProp);
                        if (call != null) toolCalls.Add(call);
                    }
                    return toolCalls.Any();
                }
            }
            catch { }
            return false;
        }

        private static ToolCall ParseToolCall(JsonElement element)
        {
            try
            {
                var toolName = element.GetProperty("tool_name").GetString();
                var args = new Dictionary<string, object>();
                if (element.TryGetProperty("arguments", out var tempargs) && tempargs.ValueKind == JsonValueKind.Object)
                {
                    foreach (var p in tempargs.EnumerateObject())
                        args[p.Name] = JsonSerializer.Deserialize<object>(p.Value.GetRawText());
                }
                return new ToolCall { Name = toolName, Arguments = args };
            }
            catch { return null; }
        }

    }
}
