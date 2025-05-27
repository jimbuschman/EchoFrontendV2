
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
            txtCurrentLLM = new TextBox();
            button1 = new Button();
            button2 = new Button();
            button3 = new Button();
            checkBox1 = new CheckBox();
            rtbRunningContext = new SmoothScrollRichTextBox();
            button4 = new Button();
            txtInfo = new TextBox();
            txtSearch = new TextBox();
            btnSearch = new Button();
            rbMemory = new RadioButton();
            rbLesson = new RadioButton();
            btnImportFile = new Button();
            btnFrameworkImport = new Button();
            btnBook = new Button();
            btnImage = new Button();
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
            btnShowLog.Location = new Point(12, 78);
            btnShowLog.Name = "btnShowLog";
            btnShowLog.Size = new Size(240, 23);
            btnShowLog.TabIndex = 6;
            btnShowLog.Text = "Show Logs";
            btnShowLog.UseVisualStyleBackColor = true;
            btnShowLog.Click += btnShowLog_Click;
            // 
            // btnLauchChat
            // 
            btnLauchChat.Location = new Point(12, 107);
            btnLauchChat.Name = "btnLauchChat";
            btnLauchChat.Size = new Size(240, 23);
            btnLauchChat.TabIndex = 7;
            btnLauchChat.Text = "Run ChatGPT";
            btnLauchChat.UseVisualStyleBackColor = true;
            btnLauchChat.Click += btnLauchChat_Click;
            // 
            // txtCurrentLLM
            // 
            txtCurrentLLM.Location = new Point(12, 24);
            txtCurrentLLM.Name = "txtCurrentLLM";
            txtCurrentLLM.Size = new Size(159, 23);
            txtCurrentLLM.TabIndex = 8;
            // 
            // button1
            // 
            button1.Location = new Point(177, 24);
            button1.Name = "button1";
            button1.Size = new Size(75, 23);
            button1.TabIndex = 9;
            button1.Text = "Update";
            button1.UseVisualStyleBackColor = true;
            // 
            // button2
            // 
            button2.Location = new Point(12, 209);
            button2.Name = "button2";
            button2.Size = new Size(240, 23);
            button2.TabIndex = 10;
            button2.Text = "Backup DB";
            button2.UseVisualStyleBackColor = true;
            button2.Click += button2_Click;
            // 
            // button3
            // 
            button3.Location = new Point(12, 180);
            button3.Name = "button3";
            button3.Size = new Size(240, 23);
            button3.TabIndex = 11;
            button3.Text = "Dump Memory";
            button3.UseVisualStyleBackColor = true;
            // 
            // checkBox1
            // 
            checkBox1.AutoSize = true;
            checkBox1.Location = new Point(12, 53);
            checkBox1.Name = "checkBox1";
            checkBox1.Size = new Size(130, 19);
            checkBox1.TabIndex = 12;
            checkBox1.Text = "Larger Model Mode";
            checkBox1.UseVisualStyleBackColor = true;
            // 
            // rtbRunningContext
            // 
            rtbRunningContext.BackColor = Color.Black;
            rtbRunningContext.BorderStyle = BorderStyle.None;
            rtbRunningContext.Font = new Font("Consolas", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            rtbRunningContext.ForeColor = Color.FromArgb(255, 128, 0);
            rtbRunningContext.Location = new Point(1066, 24);
            rtbRunningContext.Name = "rtbRunningContext";
            rtbRunningContext.ReadOnly = true;
            rtbRunningContext.Size = new Size(323, 647);
            rtbRunningContext.TabIndex = 13;
            rtbRunningContext.Text = "";
            // 
            // button4
            // 
            button4.Location = new Point(12, 151);
            button4.Name = "button4";
            button4.Size = new Size(240, 23);
            button4.TabIndex = 14;
            button4.Text = "Database Audit";
            button4.UseVisualStyleBackColor = true;
            button4.Click += button4_Click;
            // 
            // txtInfo
            // 
            txtInfo.Location = new Point(4, 486);
            txtInfo.Multiline = true;
            txtInfo.Name = "txtInfo";
            txtInfo.Size = new Size(248, 185);
            txtInfo.TabIndex = 15;
            // 
            // txtSearch
            // 
            txtSearch.Location = new Point(4, 427);
            txtSearch.Name = "txtSearch";
            txtSearch.Size = new Size(159, 23);
            txtSearch.TabIndex = 16;
            // 
            // btnSearch
            // 
            btnSearch.Location = new Point(177, 427);
            btnSearch.Name = "btnSearch";
            btnSearch.Size = new Size(75, 23);
            btnSearch.TabIndex = 17;
            btnSearch.Text = "Search";
            btnSearch.UseVisualStyleBackColor = true;
            btnSearch.Click += btnSearch_Click;
            // 
            // rbMemory
            // 
            rbMemory.AutoSize = true;
            rbMemory.Checked = true;
            rbMemory.Location = new Point(7, 458);
            rbMemory.Name = "rbMemory";
            rbMemory.Size = new Size(78, 19);
            rbMemory.TabIndex = 18;
            rbMemory.TabStop = true;
            rbMemory.Text = "Memories";
            rbMemory.UseVisualStyleBackColor = true;
            // 
            // rbLesson
            // 
            rbLesson.AutoSize = true;
            rbLesson.Location = new Point(91, 458);
            rbLesson.Name = "rbLesson";
            rbLesson.Size = new Size(66, 19);
            rbLesson.TabIndex = 19;
            rbLesson.Text = "Lessons";
            rbLesson.UseVisualStyleBackColor = true;
            // 
            // btnImportFile
            // 
            btnImportFile.Location = new Point(12, 238);
            btnImportFile.Name = "btnImportFile";
            btnImportFile.Size = new Size(240, 23);
            btnImportFile.TabIndex = 20;
            btnImportFile.Text = "File Import";
            btnImportFile.UseVisualStyleBackColor = true;
            btnImportFile.Click += btnImportFile_Click;
            // 
            // btnFrameworkImport
            // 
            btnFrameworkImport.Location = new Point(12, 267);
            btnFrameworkImport.Name = "btnFrameworkImport";
            btnFrameworkImport.Size = new Size(240, 23);
            btnFrameworkImport.TabIndex = 21;
            btnFrameworkImport.Text = "Framework File Import";
            btnFrameworkImport.UseVisualStyleBackColor = true;
            btnFrameworkImport.Click += btnFrameworkImport_Click;
            // 
            // btnBook
            // 
            btnBook.Location = new Point(12, 296);
            btnBook.Name = "btnBook";
            btnBook.Size = new Size(240, 23);
            btnBook.TabIndex = 22;
            btnBook.Text = "Book Import";
            btnBook.UseVisualStyleBackColor = true;
            btnBook.Click += btnBook_Click;
            // 
            // btnImage
            // 
            btnImage.Location = new Point(12, 325);
            btnImage.Name = "btnImage";
            btnImage.Size = new Size(240, 23);
            btnImage.TabIndex = 23;
            btnImage.Text = "Image Import";
            btnImage.UseVisualStyleBackColor = true;
            btnImage.Click += btnImage_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1401, 685);
            Controls.Add(btnImage);
            Controls.Add(btnBook);
            Controls.Add(btnFrameworkImport);
            Controls.Add(btnImportFile);
            Controls.Add(rbLesson);
            Controls.Add(rbMemory);
            Controls.Add(btnSearch);
            Controls.Add(txtSearch);
            Controls.Add(txtInfo);
            Controls.Add(button4);
            Controls.Add(rtbRunningContext);
            Controls.Add(checkBox1);
            Controls.Add(button3);
            Controls.Add(button2);
            Controls.Add(button1);
            Controls.Add(txtCurrentLLM);
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
        private TextBox txtCurrentLLM;
        private Button button1;
        private Button button2;
        private Button button3;
        private CheckBox checkBox1;
        private SmoothScrollRichTextBox rtbRunningContext;
        private Button button4;
        private TextBox txtInfo;
        private TextBox txtSearch;
        private Button btnSearch;
        private RadioButton rbMemory;
        private RadioButton rbLesson;
        private Button btnImportFile;
        private Button btnFrameworkImport;
        private Button btnBook;
        private Button btnImage;
    }
}
