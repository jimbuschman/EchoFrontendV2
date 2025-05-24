namespace EchoFrontendV2
{
    partial class LogInfo
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            rtbLogs = new SmoothScrollRichTextBox();
            SuspendLayout();
            // 
            // rtbLogs
            // 
            rtbLogs.BackColor = Color.Black;
            rtbLogs.BorderStyle = BorderStyle.None;
            rtbLogs.Font = new Font("Consolas", 15F, FontStyle.Regular, GraphicsUnit.Point, 0);
            rtbLogs.ForeColor = Color.FromArgb(0, 192, 192);
            rtbLogs.Location = new Point(9, 12);
            rtbLogs.Name = "rtbLogs";
            rtbLogs.ReadOnly = true;
            rtbLogs.Size = new Size(779, 426);
            rtbLogs.TabIndex = 5;
            rtbLogs.Text = "";
            // 
            // LogInfo
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(rtbLogs);
            Name = "LogInfo";
            Text = "LogInfo";
            ResumeLayout(false);
        }

        #endregion

        private SmoothScrollRichTextBox rtbLogs;
    }
}