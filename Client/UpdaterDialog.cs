using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Updater
{
    public partial class UpdaterDialog : Form
    {
        public ProgressBar progress;
        public UpdaterDialog()
        {
            InitializeComponent();
            FormBorderStyle = FormBorderStyle.None;
            ClientSize = new Size(250, 70);
            progress = new ProgressBar();
            progress.Location = new Point(20, 40);
            progress.Size = new Size(210, 18);
            progress.Minimum = 0;
            progress.Maximum = 100;
            progress.Value = 0;
            Controls.Add(progress);
        }
    }
}
