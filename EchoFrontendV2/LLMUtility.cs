using EchoFrontendV2;
using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace TestSQLLite
{
    public static class  LLMUtilityCalls
    {
        private static string _llmName = "llama3.2:latest"; //hf.co/prithivMLmods/Llama-Chat-Summary-3.2-3B-GGUF:Q4_K_M 
        private static async Task<string> GetLLMResponse(string text, RealtimeLogger _logger, string llmName)
        {

            if(string.IsNullOrWhiteSpace(llmName))
                llmName = _llmName;

            string returnVal = string.Empty;
            try
            {
                var llm = new LLMUtility(_logger, llmName);
                returnVal = (await llm.SendMessage(text));                
            }
            catch (Exception ex)
            {
                _logger.LogException("LLMUtilityCalls:GetLLMResponse(): " + ex.Message);
            }
            return returnVal;
        }
        public static async Task<string> RankMemory(string text, RealtimeLogger _logger, string llmName = "")
        {    
            string phrase = "You are evaluating a message and its summary to determine how informative or meaningful it is.\r\n\r\nBased on the content of the message, " +
                "summary, and your judgment of how useful it would be for memory recall, system onboarding, or future reference, assign it a rank from 1 to 5, " +
                "using the following scale:\r\n\r\n📊 Suggested Meaning Scale:\r\n1 — Noise / Fluff\r\nThe message is boilerplate, repetitive, off-topic, or " +
                "lacking meaningful content.\r\n→ Ignore for memory or search.\r\n\r\n2 — Minor\r\nThe message contains light emotional context or a vague thought, " +
                "but lacks depth or specificity.\r\n→ Low search priority.\r\n\r\n3 — Useful\r\nThe message contains at least one clear idea, insight, or point worth " +
                "keeping.\r\n→ Include in search, not critical.\r\n\r\n4 — Important\r\nThe message has clear relevance, contributes meaningful insight, or reveals a " +
                "decision, realization, or reflective moment.\r\n→ Keep for memory, onboarding, and reflection.\r\n\r\n5 — Critical\r\nThe message is core to system identity, " +
                "evolution, autonomy, or decision-making. This includes key turning points or foundational reflections.\r\n→ Must be retained for context and version " +
                "continuity.\r\n\r\n⚙️ Output Format:\r\nPlease respond with only the rank (1–5) Message:" + text;
            return await GetLLMResponse(phrase, _logger, llmName);
        }
        public static async Task<string> RephraseAsMemoryStyle(string text, RealtimeLogger _logger, string llmName = "")
        {
            var phrase = $@"
                Rephrase the question as a direct, factual sentence someone might have said in a conversation.
                Avoid emotional or poetic language. Be concise and declarative.

                Question: {text}
                Declarative:";
            return await GetLLMResponse(phrase, _logger, llmName);
        }

        public static async Task<string> SummarizeMemory(string text, RealtimeLogger _logger, string llmName = "")
        {
            if (string.IsNullOrWhiteSpace(llmName))
                llmName = "hf.co/prithivMLmods/Llama-Chat-Summary-3.2-3B-GGUF:Q4_K_M";

            var phrase = $@"
                Summarize the following memory in 1 concise, factual sentence.
                Avoid lists or multiple versions. Focus on core details.
                Memory: {text}";
            return await GetLLMResponse(phrase, _logger, llmName);
           
        }
        public static async Task<string> SummarizeFile(string text, RealtimeLogger _logger, string llmName = "")
        {
            var phrase = $@"
                Summarize the following File in 2-3 concise, factual sentence.
                Avoid lists or multiple versions. Focus on core details.
                File: {text}";
            return await GetLLMResponse(phrase, _logger, llmName);

        }
        public static async Task<string> SummarizeBook(string text, RealtimeLogger _logger, string llmName = "")
        {
            var phrase = $@"
                Please write a 2-3 concise summary of this book.                
                Book: {text}";
            return await GetLLMResponse(phrase, _logger, llmName);

        }
        public static async Task<string> AuthorBook(string text, RealtimeLogger _logger, string llmName = "")
        {
            var phrase = $@"
                 Can you respond with ONLY the author of this book, no other text.                
                Book: {text}";
            return await GetLLMResponse(phrase, _logger, llmName);

        }
        public static async Task<string> TitleBook(string text, RealtimeLogger _logger, string llmName = "")
        {
            var phrase = $@"
                 Can you respond with ONLY the title of this book, no other text.                
                Book: {text}";
            return await GetLLMResponse(phrase, _logger, llmName);

        }

        public static async Task<string> GetLessionFromConversation(string text, RealtimeLogger _logger, string llmName = "")
        {
            var phrase = $@"
            Looking at the following conversation:
            1. Did the assistant understand the user's intention?
            2. What did it miss?
            3. What 3-5 lessons should it internalize to improve in the future?
            4. Growth Trajectory?

            Please separate your response into:
            Evaluation Summary
            Lessons
            Conversation:  {text}";
            return await GetLLMResponse(phrase, _logger, llmName);
        }
        public static async Task<string> SummarizeSessionChunk(string text, RealtimeLogger _logger, string llmName = "")
        {
            if (string.IsNullOrWhiteSpace(llmName))
                llmName = "hf.co/prithivMLmods/Llama-Chat-Summary-3.2-3B-GGUF:Q4_K_M";

            var phrase = $@"
            Summarize the following conversation in 1–2 concise sentences. 
            Focus only on what was discussed, decided, or explored. Avoid filler, repetition, 
            or quoting directly — rephrase in your own words.
            Conversation {text}";
            return await GetLLMResponse(phrase, _logger, llmName);         
        }

        public static async Task<string> SummarizeSessionConversation(string text, RealtimeLogger _logger, string llmName = "")
        {
            if (string.IsNullOrWhiteSpace(llmName))
                llmName = "hf.co/prithivMLmods/Llama-Chat-Summary-3.2-3B-GGUF:Q4_K_M";

            var phrase = $@"
            Summarize the following conversation in 2-3 concise sentences. Focus only on what was discussed, decided, or explored. 
            Avoid filler, repetition, or quoting directly \u2014 rephrase in your own words. 
            **Ensure the summary is in third-person, objective voice, without any 'I', 'we', or 'you' pronouns.**\n\n[" + text + "]";
            return await GetLLMResponse(phrase, _logger, llmName);
          

        }
        public static async Task<string> SummarizeSessionConversationSummaries(string text, RealtimeLogger _logger, string llmName = "")
        {
            if (string.IsNullOrWhiteSpace(llmName))
                llmName = "hf.co/prithivMLmods/Llama-Chat-Summary-3.2-3B-GGUF:Q4_K_M";

            var phrase = $@"
            Please summarize these summaries into 3-5 sentences that reflect the overall conversation. \n\n[" + text + "]";
            return await GetLLMResponse(phrase, _logger, llmName);
            

        }
        public static async Task<string> GenerateSessionConversationTitle(string text, RealtimeLogger _logger, string llmName = "")
        {
            var phrase = $@"
            Generate a concise title for this conversation 1 sentence or less. Please keep it short and concise and respond with only the title. Conversation: {text}";
            return await GetLLMResponse(phrase, _logger, llmName);
           
        }

        public static async Task<string> GenerateSessionMemoryName(string text, RealtimeLogger _logger, string llmName = "")
        {
            var phrase = $@"
            Generate a concise name for this memory using 2-3 words. Please keep it short and concise and respond with only the name. Memory: {text}";
            return await GetLLMResponse(phrase, _logger, llmName);
            
        }

        public static async Task<double> AskImportance(string text, RealtimeLogger _logger, string llmName = "")
        {
            var phrase = $@"
            Rate the importance of the following memory on a scale from 0.0 to 1.0.
            Use the following guidelines:
            - 1.0 = Deeply personal, emotionally significant, critical fact, or core belief
            - 0.7 = Important context or recurring theme
            - 0.4 = Useful but minor detail
            - 0.1 = Casual, generic, or low-impact
            
            Respond ONLY with a single numeric value.
            
            Memory: {text}";
            var response = await GetLLMResponse(phrase, _logger, llmName);
            if(double.TryParse(response, out double importance))
            {
                return importance;
            }
            else
            {
                _logger.LogException("LLMUtilityCalls:AskImportance(): Failed to parse importance value.");
                return 0.0; // Default value if parsing fails
            }
        }

    }
    public class LLMUtility
    {
        private static readonly HttpClient httpClient;
        private readonly string _modelName;
        private static readonly ConcurrentDictionary<string, string> responseCache = new();
        private readonly RealtimeLogger _logger;
        private bool _useCPU;
        public static bool UserOldPC = false;
        private static string _baseAddress = "https://0272-2604-2d80-a602-400-9d22-1ee2-d29b-28ef.ngrok-free.app";
        public LLMUtility(RealtimeLogger logger, string name = "llama3.2:latest", bool useCPU = true)
        {
            _logger = logger;
            _modelName = name;
            _useCPU = useCPU;
        }

        // Define response model classes
        private class OllamaResponse
        {
            public OllamaMessage? message { get; set; }
            public bool done { get; set; }
        }

        private class OllamaMessage
        {
            public string? content { get; set; }
        }

        static LLMUtility()
        {
            var socketsHandler = new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(5),
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
                MaxConnectionsPerServer = 100,  // Increased from 20
                EnableMultipleHttp2Connections = true  // Better for HTTP/2
            };

            httpClient = new HttpClient(socketsHandler)
            {
                BaseAddress = new Uri( UserOldPC ? _baseAddress :  "http://localhost:11434/"),
                Timeout = TimeSpan.FromSeconds(180)
            };

            // Add these headers for better performance
            httpClient.DefaultRequestHeaders.ConnectionClose = false;
            httpClient.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
            httpClient.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
            //httpClient.DefaultRequestHeaders.Accept.Clear();
            //httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }



        // Add these static readonly fields at class level
        private static readonly JsonSerializerOptions jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultBufferSize = 1024
        };


        public async Task<string> SendMessage(string message, bool skipReply = false)
        {
            if (responseCache.TryGetValue(message, out var cachedResponse))
            {
                return cachedResponse;
            }
            if (UserOldPC)
                _useCPU = true;
            var messages = new List<object>
            {               
                new {
                    role = "system",
                    content = "You are a highly efficient, single-output summarization module. Your ONLY purpose is to produce a summary. You will NEVER engage in conversation, offer greetings, ask questions, or add any introductory or concluding remarks. Respond with nothing but the requested summary. The summary MUST be written from a neutral, objective, third-person perspective. Do NOT use first-person (e.g., 'I', 'we') or second-person (e.g., 'you') pronouns. Do not output anything that sounds like a human speaking or an AI assisting."
                },
                new {
                    role = "user",
                    content = message // The original message parameter is now the user's content
                }
            };
            var payload = new
            {
                messages = messages, // Use the constructed list of messages
                //messages = new[] { new { role = "user", content = message } },
                //max_tokens = 150,
                model = _modelName,               
                options = new  // Add Ollama-specific options
                {
                    //temperature = 0.1,
                    num_gpu = 1// _useCPU ? 0 : 1  // 0 = CPU-only, 1 = use GPU
                }
            };

            string jsonPayload = JsonSerializer.Serialize(payload, jsonOptions);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            try
            {
                HttpResponseMessage response = await httpClient.PostAsync("api/chat", content);

                if (response.IsSuccessStatusCode)
                {
                    if (skipReply)
                    {
                        return string.Empty;
                    }

                    using var responseStream = await response.Content.ReadAsStreamAsync();
                    using var reader = new StreamReader(responseStream);
                    var fullResponse = new StringBuilder();

                    while (!reader.EndOfStream)
                    {
                        var line = await reader.ReadLineAsync();
                        if (!string.IsNullOrEmpty(line))
                        {
                            try
                            {
                                var result = JsonSerializer.Deserialize<OllamaResponse>(line, jsonOptions);
                                if (result?.message?.content != null)
                                {
                                    fullResponse.Append(result.message.content);
                                }
                                if (result?.done == true)
                                {
                                    break;
                                }
                            }
                            catch (JsonException)
                            {
                                // Skip malformed JSON lines
                                continue;
                            }
                        }
                    }

                    var responseText = fullResponse.ToString();
                    responseCache.TryAdd(message, responseText);
                    return responseText;
                }
                else
                {
                    string errorResponse = await response.Content.ReadAsStringAsync();
                    _logger.LogException($"LLMUtility:SendMessage(): Error: {response.StatusCode}, Response: {errorResponse}");
                    return "Sorry, I couldn't process your request.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogException("LLMUtility:SendMessage(): " + ex.Message);
                return "An error occurred while processing your request.";
            }
        }
    }
}