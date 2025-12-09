using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Application_A
{
    public partial class Form1 : Form
    {

        string statusApp = "close";
        string baseURL = @"http://localhost:0000";
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void ChangeDoorByStatus(string status)
        {
            if (status == "Open")
            {
                pictureBox1.Image = Properties.Resources.door_open;
                statusApp = "open";
            }
            else
            {
                pictureBox1.Image = Properties.Resources.door_close;
                statusApp = "close";
            }
            
        }
    }
}
