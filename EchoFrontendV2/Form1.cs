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
        static MemoryDB database = null;
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

        private void btnSend_Click(object sender, EventArgs e)
        {
            rtbMessages.AppendAndScroll(">> " + txtUserMessage.Text, true);
            _ = RunChat(txtUserMessage.Text);
            txtUserMessage.Clear();
        }

        public async Task<string> RunChat(string userInput, bool fromChatGPT = false)
        {
            string returnMessage = string.Empty;
            if (!database.CurrentlySaving)
            {
                _ = Task.Run(async () =>
                {
                    try { await database.DumpConversationToMemory(sessionManager); }
                    catch (Exception ex) { _logger.LogException("IndexModel:RunChat(): " + ex.Message); }
                });
            }

            //sending message to main llm           
            returnMessage = await MemoryDB.LLMQueue.EnqueueAndWait(async () =>
             {
                 _logger.LogTrack("Pulling Core Memories");
                 //load core memories
                 var staticMemories = await database.PullCoreMemoriesAsync();
                 if (staticMemories != null && staticMemories.Count > 0)
                 {
                     _logger.LogTrack("Core Memories:");
                     foreach (var sm in staticMemories)
                     {
                         OllamaChat.memoryManager.AddMemory("Core", new MemoryItem { Text = sm.Content, EstimatedTokens = TokenEstimator.EstimateTokens(sm.Content), SessionRole = "system", PriorityScore = sm.Priority });
                         _logger.LogTrack("-" + sm.Content);
                     }
                 }

                 //load relevent lessons, if any
                 _logger.LogTrack("Pulling Lessons");
                 var lessons = await database.PullLessonsAsync();
                 if (lessons != null && lessons.Count > 0)
                 {
                     _logger.LogTrack("Lessons:");
                     foreach (var sm in lessons)
                     {
                         OllamaChat.memoryManager.AddMemory("Recall", new MemoryItem { Text = sm.Text, EstimatedTokens = TokenEstimator.EstimateTokens(sm.Text), SessionRole = "system", PriorityScore = 1 });
                         _logger.LogTrack("-" + sm.Text);
                     }
                 }

                 //load relevent memories
                 _logger.LogTrack("Pulling Memories");
                 var mem = await database.SearchMemoriesAsync(userInput, sessionManager.SessionId);
                 string memoriesRemembered = string.Empty;
                 if (mem.Count > 0)
                 {
                     _logger.LogTrack("Memories:");
                     var sel = mem.Where(m => m.Score >= .75).Take(3);
                     foreach (var s in sel)
                     {
                         OllamaChat.memoryManager.AddMemory("Recall", new MemoryItem { SessionID = sessionManager.SessionId, Text = s.Text, EstimatedTokens = TokenEstimator.EstimateTokens(s.Text), SessionRole = "system", PriorityScore = s.Score });
                         _logger.LogTrack("-" + s.Text);
                     }
                 }

                 _logger.LogTrack("Sending Message...");
                 return await OllamaChat.SendMessageToOllama(userInput, fromChatGPT, UpdateChatDisplay);

             }, priority: 0);

            rtbMessages.AppendAndScroll(Environment.NewLine);
            sessionManager.AddMessage(new SessionMessage("user", userInput));

            OllamaChat.memoryManager.AddMemory("ActiveSession", new MemoryItem { Text = userInput, EstimatedTokens = TokenEstimator.EstimateTokens(userInput), SessionRole = "user", PriorityScore = 1 });

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

        public async Task RunChatGPTChat()
        {
            OllamaChatGPTIntegration chatGPT = new OllamaChatGPTIntegration();
            StringBuilder sb = new StringBuilder();

            var currentMessage = sb.ToString();
            var fromString = "";

            while (_runChatgptChat)
            {
                await Task.Delay(1000);
                var message = await chatGPT.GetChatGPTResponse(fromString + currentMessage);
                fromString = "From Gemma3: ";
                rtbMessages.AppendAndScroll("ChatGPT: " + message + Environment.NewLine);
                currentMessage = await RunChat(message, true);
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
            await database.DumpConversationToMemory(sessionManager);

            await database.CreateSessionSummary(sessionManager.SessionId);
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            database = _startup.Database;
            sessionManager = _startup.SessionManager;

            OllamaChat.Setup(_logger);
            var s = database.GetSessions();
            foreach (var session in s.Where(s => string.IsNullOrWhiteSpace(s.Summary)).ToList())
                await database.CreateSessionSummary(session.ID);

            sessionManager.SessionId = await database.CreateNewSession();
            var previousSessions = await database.GetPreviousSessions(sessionManager.SessionId);

            OllamaChat.SetPreviousSessions(previousSessions);

        }

        private void button4_Click(object sender, EventArgs e)
        {
            StringBuilder sb = new StringBuilder();
            var sessions = database.GetSessions();
            foreach (var s in sessions)
            {
                if (string.IsNullOrWhiteSpace(s.Summary))
                {
                    sb.AppendLine($"Session ID: {s.ID} Missing Summary");
                }
                var memories = database.GetMemoriesBySession(s.ID);
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
                var mem = await database.SearchMemoriesAsync(txtSearch.Text, sessionManager.SessionId);
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
                        await database.CreateFile(filePath);
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
                        var f = await database.GetFile(filePath);
                        var id = await database.CreateFrameworkFile(f.Id, f.Source);
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
                        var id = await database.CreateFile(filePath);

                        var text = System.IO.File.ReadAllText(filePath);
                        await database.CreateBookFile(id, text);
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
