using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace EchoFrontendV2
{
    public class SmoothScrollRichTextBox : RichTextBox
    {
        [DllImport("user32.dll")]
        static extern int GetScrollPos(IntPtr hWnd, int nBar);

        [DllImport("user32.dll")]
        static extern int SetScrollPos(IntPtr hWnd, int nBar, int nPos, bool bRedraw);

        [DllImport("user32.dll")]
        static extern int SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

        private const int WM_VSCROLL = 0x0115;
        private const int SB_VERT = 0x1;
        private const int SB_THUMBPOSITION = 4;
        private const int SB_THUMBTRACK = 5;

        private Timer scrollTimer;
        private int targetScrollPos;
        private StringBuilder lineBuffer = new StringBuilder();
        public SmoothScrollRichTextBox()
        {
            scrollTimer = new Timer();
            scrollTimer.Interval = 15; // smoother with smaller interval
            scrollTimer.Tick += ScrollTimer_Tick;
        }

        private void ScrollTimer_Tick(object sender, EventArgs e)
        {
            int currentPos = GetScrollPos(this.Handle, SB_VERT);
            if (currentPos < targetScrollPos)
            {
                int newPos = currentPos + 1;
                SetScrollPos(this.Handle, SB_VERT, newPos, true);
                SendMessage(this.Handle, WM_VSCROLL, SB_THUMBPOSITION + 0x10000 * newPos, 0);
            }
            else
            {
                scrollTimer.Stop();
            }
        }

        public void SmoothScrollToBottom()
        {
            this.SelectionStart = this.Text.Length;
            this.SelectionLength = 0;
            this.ScrollToCaret(); // optional jump to bottom immediately
            targetScrollPos = GetScrollPos(this.Handle, SB_VERT);
            scrollTimer.Start();
        }
        private int previousLineCount = 0;

        public void AppendChunk(string textChunk)
        {
            // Append the text first
            this.AppendText(textChunk);

            // Check if a new line was added
            int currentLineCount = this.GetLineFromCharIndex(this.TextLength) + 1;

            if (currentLineCount > previousLineCount)
            {
                previousLineCount = currentLineCount;
                SmoothScrollToBottom(); // only scroll on visible growth
            }
        }
        public void AppendAndScroll(string text,bool user = false)
        {
            if(user)
                this.SelectionColor = Color.Green;
            else
                this.SelectionColor = Color.Teal;
            this.AppendText(text + Environment.NewLine);
            SmoothScrollToBottom();
        }
    }
}
