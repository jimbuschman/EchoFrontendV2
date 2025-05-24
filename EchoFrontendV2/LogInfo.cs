using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EchoFrontendV2
{
    public partial class LogInfo : Form
    {
        public LogInfo()
        {
            InitializeComponent();
        }
        public void AddLogText(string message)
        {
            rtbLogs.AppendAndScroll(message);
        }
    }
}
