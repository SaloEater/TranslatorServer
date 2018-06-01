using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TranslatorServer
{
    public partial class Form1 : Form
    {

        Server server;
        Thread myThread;
        public Form1()
        {
            InitializeComponent();
            server = new Server(80);
            myThread = new Thread(server.Start);
            this.FormClosed += new FormClosedEventHandler(Form1_FormClosed);
            Console.WriteLine(DateTime.Now);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            
        }

        private void button1_Click(object sender, EventArgs e)
        {
            openFileDialog1.Multiselect = true;
            openFileDialog1.Filter = "TSV files (*.tsv)|*.tsv|All files (*.*)|*.*";
            openFileDialog1.ShowDialog();
            foreach (string filename in openFileDialog1.FileNames)
                listBox1.Items.Add(Path.GetFileName(filename));
        }

        private void button2_Click(object sender, EventArgs e)
        {
            InitializeServerVariables();
            //server.Start();
            myThread.Start();
        }

        private void InitializeServerVariables()
        {
            foreach (String file in listBox1.Items)
            {
                server.AddFile(file);
            }
            server.LoadFiles();
        }

        void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            server.SaveFiles();
            server.SendFilesToGoogleSheets();
            myThread.Abort();
        }
    }
}
