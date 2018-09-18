using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace TestSO
{

    public partial class Main : Form
    {


        private void SetText(string text, Label L)
        {
            if (L.InvokeRequired)
            {
                StringArgReturningVoidDelegate d = new StringArgReturningVoidDelegate(SetText);
                this.Invoke(d, new object[] { text, L });
            }
            else
            {
                L.Text = text;
            }
        }

        int count = 0;
        private readonly object _lock = new object();
        private readonly Queue<string> _queue = new Queue<string>();
        private readonly AutoResetEvent _signal = new AutoResetEvent(false);
        delegate void StringArgReturningVoidDelegate(string text, Label L);
        private static NamedPipeServerStream server;
        private BinaryReader br;
        private BinaryWriter bw;
        public Main()
        {
            InitializeComponent();
            server = new NamedPipeServerStream("testing");
            br = new BinaryReader(server);
            bw = new BinaryWriter(server);
            new Thread(new ThreadStart(ProducerThread)).Start();
        }
        private void Main_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (server != null)
            {
                server.Close();
                server.Dispose();
                server = null;
            }
            Application.Exit();
        }

        void ProducerThread()
        {
            while (true)
            {
                _signal.WaitOne();
                string item = string.Empty;
                do
                {
                    item = string.Empty;
                    lock (_lock)
                    {
                        if (_queue.Count > 0)
                        {
                            item = _queue.Dequeue();
                            this.SetText("Started " + item.ToString() + " and left " + string.Join(",", _queue.ToArray()), this.label1);
                        }
                    }

                    if (item != string.Empty)
                    {

                        try
                        {

                            if (server != null && !server.IsConnected)
                                server.WaitForConnection();

                            if (server != null && server.IsConnected)
                            {
                                var str = new string(item.ToString().ToArray());

                                var buf = Encoding.ASCII.GetBytes(str);
                                bw.Write((uint)buf.Length);
                                bw.Write(buf);
                            }
                            if (server != null && server.IsConnected)
                            {
                                var len = (int)br.ReadUInt32();
                                var str = new string(br.ReadChars(len));
                                this.SetText(str, this.label2);
                            }
                        }
                        catch (Exception EX)
                        {
                            MessageBox.Show(EX.Message.ToString());
                        }
                    }
                }
                while (item != string.Empty);
            }
        }

        private void add_Click(object sender, EventArgs e)
        {
            count++;
            lock (_lock)
            {
                _queue.Enqueue(count.ToString());
                this.SetText("Added "+count.ToString()+" and have " + string.Join(",", _queue.ToArray()), this.label1);
            }
            _signal.Set();
        }


    }
}