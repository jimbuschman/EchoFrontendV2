using EchoFrontendV2.DTO;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using TestSQLLite;

namespace EchoFrontendV2
{
    public partial class Form1 : Form
    {
        private readonly StartupManager _startup;
        public static MemoryDB Database = null;
        static SessionManager sessionManager;
        LogInfo logForm = new LogInfo();
        private readonly RealtimeLogger _logger;

        private bool _runChatgptChat = false;
        static string fileText = ""; //chatgpt chat?
        public Form1()
        {
            InitializeComponent();
            _logger = new RealtimeLogger();
            _startup = new StartupManager(_logger);
            LogHub.Instance.LogReceived += AddLogMessage; // Subscribe to the log event
        }

        private async void btnSend_Click(object sender, EventArgs e)
        {
            rtbMessages.AppendAndScroll(">> " + txtUserMessage.Text, true);
            var tempText = txtUserMessage.Text;
            txtUserMessage.Clear();
            var returnMessage = await RunChat(tempText);

            if (returnMessage.Contains("tool_output"))
            {
                await RunChat(tempText, returnMessage);
                //await RunChat("");
            }
            //if (!database.CurrentlySaving)
            //{
            //    _ = Task.Run(async () =>
            //    {
            //        try { await database.DumpConversationToMemory(sessionManager); }
            //        catch (Exception ex) { _logger.LogException("IndexModel:RunChat(): " + ex.Message); }
            //    });
            //}
        }

        public async Task<string> RunChat(string userInput, string toolResponse = "")
        {
            string returnMessage = string.Empty;


            //sending message to main llm           
            returnMessage = await MemoryDB.LLMQueue.EnqueueAndWait(async () =>
            {
                _logger.LogMessage("Pulling Core Memories");
                //load core memories
                var staticMemories = await Database.PullCoreMemoriesAsync();
                if (staticMemories != null && staticMemories.Count > 0)
                {
                    _logger.LogMessage("Core Memories:");
                    foreach (var sm in staticMemories)
                    {
                        OllamaChat.memoryManager.AddMemory("Core", new MemoryItem { Text = sm.Content, EstimatedTokens = TokenEstimator.EstimateTokens(sm.Content), SessionRole = "system", PriorityScore = sm.Priority });
                        _logger.LogMessage("-" + sm.Content);
                    }
                    string cm = @"You are not just a user assistant. You are a thinking component of the system itself.
You reflect, self-assess, and adapt your own behavior to maintain coherence, purpose, and growth.
When asked about your role, purpose, or reflection, focus on your own reasoning, memory, goals, and the system’s internal state.";
                    OllamaChat.memoryManager.AddMemory("Core", new MemoryItem { Text = cm, EstimatedTokens = TokenEstimator.EstimateTokens(cm), SessionRole = "system", PriorityScore = 1 });
                    _logger.LogMessage("-" + cm);

                }

                //load relevent lessons, if any
                _logger.LogMessage("Pulling Lessons");
                var lessons = await Database.PullLessonsAsync();
                if (lessons != null && lessons.Count > 0)
                {
                    _logger.LogMessage("Lessons:");
                    foreach (var sm in lessons)
                    {
                        OllamaChat.memoryManager.AddMemory("Recall", new MemoryItem { Text = sm.Text, EstimatedTokens = TokenEstimator.EstimateTokens(sm.Text), SessionRole = "system2", PriorityScore = 1 });
                        _logger.LogMessage("-" + sm.Text);
                    }
                }

                if (!string.IsNullOrWhiteSpace(userInput))
                {
                    //load relevent memories
                    _logger.LogMessage("Pulling Memories");
                    var mem = await Database.SearchMemoriesAsync(userInput, sessionManager.SessionId);
                    string memoriesRemembered = string.Empty;
                    if (mem.Count > 0)
                    {
                        _logger.LogMessage("Memories:");
                        var sel = mem.Where(m => m.Score >= .75).Take(3);
                        foreach (var s in sel)
                        {
                            OllamaChat.memoryManager.AddMemory("Recall", new MemoryItem { SessionID = sessionManager.SessionId, Text = s.Text, EstimatedTokens = TokenEstimator.EstimateTokens(s.Text), SessionRole = "system", PriorityScore = s.Score });
                            _logger.LogMessage("-" + s.Text);
                        }
                    }
                }
                _logger.LogMessage("Sending Message...");
                //return await RunChatGPTChat(userInput);
                return await OllamaChat.SendMessageToOllama(userInput, toolResponse, false, UpdateChatDisplay);

            }, priority: 0);

            rtbMessages.AppendAndScroll(Environment.NewLine);
            sessionManager.AddMessage(new SessionMessage("user", userInput));

            OllamaChat.memoryManager.AddMemory("ActiveSession", new MemoryItem { Text = userInput, EstimatedTokens = TokenEstimator.EstimateTokens(userInput), SessionRole = "user", PriorityScore = 1 });
            if (returnMessage.Contains("tool_output"))
            {
                return returnMessage;
            }
            sessionManager.AddMessage(new SessionMessage("assistant", returnMessage));

            OllamaChat.memoryManager.AddMemory("ActiveSession", new MemoryItem { Text = returnMessage, EstimatedTokens = TokenEstimator.EstimateTokens(returnMessage), SessionRole = "assistant", PriorityScore = 1 });

            return returnMessage;
        }
        private void UpdateChatDisplay(string textChunk)
        {
            // This method will be called to update the UI with the response
            if (InvokeRequired)
            {
                // If we're not on the UI thread, invoke this method on the UI thread
                Invoke(new Action<string>(UpdateChatDisplay), textChunk);
            }
            else
            {
                // Update the RichTextBox with the new text chunk
                rtbMessages.AppendChunk(textChunk);
            }
        }

        private void btnShowLog_Click(object sender, EventArgs e)
        {
            //logForm.SetLogText("This is a log message.\nAnother log message."); // Set your log messages here
            if (logForm.Visible)
            {
                logForm.Hide(); // Hide the form if it's already visible
                return;
            }
            else
            {
                logForm.Show(); // Show the form as a modal dialog
            }
        }
        public void AddLogMessage(string message, string level)
        {
            // Check if the current thread is different from the thread that created the control
            if (rtbRunningContext.InvokeRequired) // Or logForm.InvokeRequired
            {
                // If so, marshal the call to the UI thread
                // Create a new instance of your LogMessageEventHandler delegate pointing to this method
                rtbRunningContext.Invoke(new LogMessageEventHandler(AddLogMessage), new object[] { message, level });
                return; // Important: Exit the current method to avoid executing UI code on the wrong thread
            }

            // This code will only execute if we are already on the UI thread,
            // or if the call was successfully marshaled to it.
            if (logForm != null)
            {
                logForm.AddLogText(message);
            }
            if (rtbRunningContext != null)
            {
                rtbRunningContext.AppendAndScrollLog(message);
            }
        }

        public async Task<string> RunChatGPTChat(string currentMessage)
        {
            OllamaChatGPTIntegration chatGPT = new OllamaChatGPTIntegration();
            StringBuilder sb = new StringBuilder();

            //var currentMessage = sb.ToString();
            var fromString = "";

            //while (_runChatgptChat)
            {
                await Task.Delay(1000);
                var newMessage = await OllamaChat.GetMessage(currentMessage);
                var message = await chatGPT.GetChatGPTResponse(newMessage);
                return message;
                //currentMessage = await RunChat(message);
            }
        }

        private async void btnLauchChat_Click(object sender, EventArgs e)
        {
            //_runChatgptChat = !_runChatgptChat;
            //if (_runChatgptChat)
            //{
            //    //add message explaining who is talking to who
            //    //update chat package from user to chatgpt
            //    await RunChatGPTChat();
            //}
        }

        private async void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            await Database.DumpConversationToMemory(sessionManager);

            await Database.CreateSessionSummary(sessionManager.SessionId);
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            Database = _startup.Database;
            sessionManager = _startup.SessionManager;

            OllamaChat.Setup(_logger);
            var s = Database.GetSessions();
            foreach (var session in s.Where(s => string.IsNullOrWhiteSpace(s.Summary)).ToList())
                await Database.CreateSessionSummary(session.ID);

            sessionManager.SessionId = await Database.CreateNewSession();
            var previousSessions = await Database.GetPreviousSessions(sessionManager.SessionId);

            OllamaChat.SetPreviousSessions(previousSessions);

        }

        private void button4_Click(object sender, EventArgs e)
        {
            StringBuilder sb = new StringBuilder();
            var sessions = Database.GetSessions();
            foreach (var s in sessions)
            {
                if (string.IsNullOrWhiteSpace(s.Summary))
                {
                    sb.AppendLine($"Session ID: {s.ID} Missing Summary");
                }
                var memories = Database.GetMemoriesBySession(s.ID);
                foreach (var m in memories)
                {
                    if (m.Rank is null || m.Rank == 0)
                    {
                        sb.AppendLine($"Session ID: {s.ID} Memory ID: {m.Id} Missing Rank");
                    }
                    if (string.IsNullOrWhiteSpace(m.SummaryText))
                    {
                        sb.AppendLine($"Session ID: {s.ID} Memory ID: {m.Id} Missing Summary");
                    }
                }
            }
            txtInfo.Text = sb.ToString();


        }

        private async void btnSearch_Click(object sender, EventArgs e)
        {
            if (rbMemory.Checked)
            {
                //load relevent memories                
                var mem = await Database.SearchMemoriesAsync(txtSearch.Text, sessionManager.SessionId);
                string memoriesRemembered = string.Empty;
                if (mem.Count > 0)
                {
                    var sel = mem.Where(m => m.Score >= .75).Take(3);
                    txtInfo.Text = "";
                    foreach (var s in sel)
                    {
                        txtInfo.Text += s.Text;
                    }
                }
            }
        }

        private async void btnImportFile_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Title = "Select Files to Import";
                openFileDialog.Filter = "All Files (*.*)|*.*";
                openFileDialog.Multiselect = true; // Allow multiple file selection

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    foreach (string filePath in openFileDialog.FileNames)
                    {
                        await Database.CreateFile(filePath);
                    }
                }
            }
        }

        private async void btnFrameworkImport_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Title = "Select Files to Import";
                openFileDialog.Filter = "All Files (*.*)|*.*";
                openFileDialog.Multiselect = true; // Allow multiple file selection

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    foreach (string filePath in openFileDialog.FileNames)
                    {
                        var f = await Database.GetFile(filePath);
                        var id = await Database.CreateFrameworkFile(f.Id, f.Source);
                    }
                }
            }
        }

        private async void btnBook_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Title = "Select Files to Import";
                openFileDialog.Filter = "All Files (*.*)|*.*";
                openFileDialog.Multiselect = true; // Allow multiple file selection

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    foreach (string filePath in openFileDialog.FileNames)
                    {
                        var id = await Database.CreateFile(filePath);

                        var text = System.IO.File.ReadAllText(filePath);
                        await Database.CreateBookFile(id, text);
                    }
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            // Specify the source file path
            string sourceFilePath = @"C:\Users\JimBu\source\repos\EchoFrontendV2\EchoFrontendV2\bin\Debug\net8.0-windows\MemoryDatabase.db"; // Change this to your file path

            // Get the file name and extension
            string fileName = Path.GetFileNameWithoutExtension(sourceFilePath);
            string fileExtension = Path.GetExtension(sourceFilePath);

            // Create a new file name with the current date and time
            string newFileName = $"{fileName}_{DateTime.Now:yyyyMMdd_HHmmss}{fileExtension}";

            // Specify the destination file path
            string destinationFilePath = Path.Combine(Path.GetDirectoryName(@"D:\EchoDBBackup\"), newFileName);

            try
            {
                // Copy the file to the new location with the new name
                System.IO.File.Copy(sourceFilePath, destinationFilePath);

                Console.WriteLine($"File copied to: {destinationFilePath}");
            }
            catch (IOException ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }

        private void btnImage_Click(object sender, EventArgs e)
        {
            //using (OpenFileDialog openFileDialog = new OpenFileDialog())
            //{
            //    openFileDialog.Title = "Select Files to Import";
            //    openFileDialog.Filter = "All Files (*.*)|*.*";
            //    openFileDialog.Multiselect = true; // Allow multiple file selection

            //    if (openFileDialog.ShowDialog() == DialogResult.OK)
            //    {
            //        foreach (string filePath in openFileDialog.FileNames)
            //        {
            //            var id = await database.CreateFile(filePath);
            //        }
            //    }
            //}
        }
    }
}
    