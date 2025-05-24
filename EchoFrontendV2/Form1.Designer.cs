
namespace EchoFrontendV2
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            txtUserMessage = new TextBox();
            rtbMessages = new SmoothScrollRichTextBox();
            btnSend = new Button();
            btnShowLog = new Button();
            btnLauchChat = new Button();
            SuspendLayout();
            // 
            // txtUserMessage
            // 
            txtUserMessage.BackColor = Color.FromArgb(64, 64, 64);
            txtUserMessage.BorderStyle = BorderStyle.FixedSingle;
            txtUserMessage.Font = new Font("Consolas", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            txtUserMessage.ForeColor = Color.White;
            txtUserMessage.Location = new Point(258, 508);
            txtUserMessage.Multiline = true;
            txtUserMessage.Name = "txtUserMessage";
            txtUserMessage.Size = new Size(718, 163);
            txtUserMessage.TabIndex = 2;
            txtUserMessage.KeyPress += txtUserMessage_KeyPress;
            // 
            // rtbMessages
            // 
            rtbMessages.BackColor = Color.Black;
            rtbMessages.BorderStyle = BorderStyle.None;
            rtbMessages.Font = new Font("Consolas", 15F, FontStyle.Regular, GraphicsUnit.Point, 0);
            rtbMessages.ForeColor = Color.FromArgb(0, 192, 192);
            rtbMessages.Location = new Point(258, 24);
            rtbMessages.Name = "rtbMessages";
            rtbMessages.ReadOnly = true;
            rtbMessages.Size = new Size(783, 478);
            rtbMessages.TabIndex = 4;
            rtbMessages.Text = "";
            // 
            // btnSend
            // 
            btnSend.Location = new Point(982, 648);
            btnSend.Name = "btnSend";
            btnSend.Size = new Size(75, 23);
            btnSend.TabIndex = 5;
            btnSend.Text = "Send";
            btnSend.UseVisualStyleBackColor = true;
            btnSend.Click += btnSend_Click;
            // 
            // btnShowLog
            // 
            btnShowLog.Location = new Point(982, 508);
            btnShowLog.Name = "btnShowLog";
            btnShowLog.Size = new Size(75, 23);
            btnShowLog.TabIndex = 6;
            btnShowLog.Text = "Show Logs";
            btnShowLog.UseVisualStyleBackColor = true;
            btnShowLog.Click += btnShowLog_Click;
            // 
            // btnLauchChat
            // 
            btnLauchChat.Location = new Point(982, 532);
            btnLauchChat.Name = "btnLauchChat";
            btnLauchChat.Size = new Size(87, 23);
            btnLauchChat.TabIndex = 7;
            btnLauchChat.Text = "Run ChatGPT";
            btnLauchChat.UseVisualStyleBackColor = true;
            btnLauchChat.Click += btnLauchChat_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1401, 685);
            Controls.Add(btnLauchChat);
            Controls.Add(btnShowLog);
            Controls.Add(btnSend);
            Controls.Add(rtbMessages);
            Controls.Add(txtUserMessage);
            Name = "Form1";
            Text = "Form1";
            FormClosing += Form1_FormClosing;
            Load += Form1_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        private void txtUserMessage_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                e.Handled = true; // Prevent the ding sound
                btnSend_Click(sender, e);
            }
        }

        #endregion

        private TextBox txtUserMessage;
        private SmoothScrollRichTextBox rtbMessages;
        private Button btnSend;
        private Button btnShowLog;
        private Button btnLauchChat;
    }
}
