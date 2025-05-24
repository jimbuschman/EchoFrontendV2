using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace EchoFrontendV2
{
    public class ChatSession
    {
        public string SessionId { get; set; }
        public string Title { get; set; }
        public string InitialTag { get; set; }
        public int MessageCount { get; set; }
        public List<Message> Messages { get; set; }
        public DateTime CreatedDate { get; set; }

        public ChatSession()
        {
            Messages = new List<Message>();
        }
    }

    public class Message
    {
        public string Role { get; set; }
        public string Content { get; set; }
        public DateTime Timestamp { get; set; }
    }
}

namespace importorig
{
    public class ChatFile
    {
        public string Title { get; set; }
        public double Create_Time { get; set; }
        public double Update_Time { get; set; }
        public Dictionary<string, MessageNode> Mapping { get; set; }
    }

    public class MessageNode
    {
        public string Id { get; set; }
        public ChatMessage Message { get; set; }
        public string Parent { get; set; }
        public List<string> Children { get; set; }
    }

    public class ChatMessage
    {
        public string Id { get; set; }
        public Author Author { get; set; }
        public double? Create_Time { get; set; }
        public double? Update_Time { get; set; }
        public MessageContent Content { get; set; }
        public string Status { get; set; }
        public bool? End_Turn { get; set; }
        public double Weight { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
        public string Recipient { get; set; }
        public string Channel { get; set; }
    }

    public class Author
    {
        public string Role { get; set; }  // e.g., "user", "assistant", "system"
        public string Name { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
    }

    public class MessageContent
    {
        [JsonProperty("content_type")]
        public string Content_Type { get; set; }

        [JsonProperty("parts")]
        public JToken Parts { get; set; }
    }


    public static class FileAutomation
    {
        [DllImport("user32.dll")]
        static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

        private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;

        /// <summary>
        /// Copies a specified file to the clipboard and then attempts to paste it using simulated input.
        /// Be aware that this method uses simulated mouse and keyboard input, which can be
        /// sensitive to screen resolution, active windows, and other system states.
        /// </summary>
        /// <param name="filePath">The full path to the file to be copied and pasted.</param>
        public static void CopyAndPasteFile(string filePath)
        {
            if (File.Exists(filePath))
            {
                StringCollection paths = new StringCollection();
                paths.Add(filePath);

                // Set the file to the clipboard
                Clipboard.SetFileDropList(paths);

                // Give the system some time to process the clipboard operation
                Thread.Sleep(5000);

                // Simulate mouse click (coordinates might need adjustment for different screens)
                // These coordinates (-427, 1635) seem specific to your original setup.
                // You might want to make these configurable or remove them if not universally applicable.
                SetCursorPos(586, 1644);
                
                mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, -447, 1625, 0, 0);

                // Simulate Ctrl+V to paste
                SendKeys.SendWait("^v");
                Thread.Sleep(5000);

                // Simulate Enter key press
                SendKeys.SendWait("{ENTER}");
            }
            else
            {
                Console.WriteLine($"Error: The file '{filePath}' does not exist.");
            }
        }
    }
}
