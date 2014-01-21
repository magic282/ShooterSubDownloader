using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ShooterSubDownloader
{
    public partial class MainWindows : Form
    {
        public MainWindows()
        {
            InitializeComponent();

            listView1.DragEnter += new DragEventHandler(listBox1_DragEnter);
            listView1.DragDrop += new DragEventHandler(listBox1_DragDrop);

            fileNames = new List<string>();
            Logger.clear();
        }

        private void MainWindows_Load(object sender, EventArgs e)
        {

        }

        private void listBox1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Link;
            else e.Effect = DragDropEffects.None;
        }

        private List<string> fileNames;

        private bool isVideoByExtension(string file)
        {
            foreach (string s in suffix)
            {
                if (s == Path.GetExtension(file).ToLower())
                {
                    return true;
                }
            }
            return false;
        }

        private void storeFileName(string[] FNs)
        {

            foreach (string s in FNs)
            {
                FileInfo f = new FileInfo(s);
                if (f.Attributes == FileAttributes.Directory)
                {
                    foreach (string item in Directory.GetFiles(s))
                    {
                        if (!fileNames.Contains(item) && isVideoByExtension(item))
                        {
                            fileNames.Add(item);
                        }
                    }
                }
                else
                {
                    if (!fileNames.Contains(s) && isVideoByExtension(s))
                    {
                        fileNames.Add(s);
                    }

                }
            }
        }
        private void listBox1_DragDrop(object sender, DragEventArgs e)
        {
            //定义一个string用于存储路径名
            string[] s = (string[])e.Data.GetData(DataFormats.FileDrop);
            storeFileName(s);
            showFileNames();
        }

        #region suffix

        private static string[] suffix = {
                                           ".mp4",
                                           ".3gp",
                                           ".3g2",
                                           ".asf",
                                           ".avi",
                                           ".vob",
                                           ".flv",
                                           ".mov",
                                           ".mkv",
                                           ".rm",
                                           ".rmvb",
                                           ".divx",
                                           ".mpg",
                                           ".mpeg",
                                           ".mpe",
                                           ".wmv"
                                        };
        #endregion

        private void showFileNames()
        {
            listView1.Items.Clear();
            ColumnHeader header = new ColumnHeader();
            header.Text = "文件名";
            header.Name = "col1";
            header.Width = 700;
            listView1.Columns.Add(header);
            foreach (string s in fileNames)
            {
                ListViewItem lvi = new ListViewItem(Path.GetFileName(s));
                lvi.BackColor = Color.White;
                listView1.Items.Add(lvi);
            }

            // scroll to bottom
            //int visibleItems = listBox1.ClientSize.Height / listBox1.ItemHeight;
            //listBox1.TopIndex = Math.Max(listBox1.Items.Count - visibleItems + 1, 0);
        }

        private ManualResetEvent[] doneEvents;
        private void startSingleTask(Object threadContext)
        {
            Shooter shooter = (Shooter)threadContext;
            shooter.startDownload();

            Logger.Log(string.Format("Subs for video {0} is finished.", shooter.FileName));
            Logger.Log(string.Format("returnStatus is {0}", shooter.Status.ToString()));
            Color c;
            if (shooter.Status == Shooter.returnStatus.DownloadFailed)
            {
                c = label7.BackColor;
            }
            else if (shooter.Status == Shooter.returnStatus.NoSubtitle)
            {
                c = label6.BackColor;
            }
            else if (shooter.Status == Shooter.returnStatus.Success)
            {
                c = label5.BackColor;
            }
            else
            {
                c = label4.BackColor;
            }
            if (listView1.InvokeRequired)
            {
                listView1.Invoke(new MethodInvoker(delegate
                {
                    listView1.Items[shooter.TaskIndex].BackColor = c;
                }));
            }

            doneEvents[shooter.TaskIndex % taskThreadNum].Set();

        }

        private int taskThreadNum = 1;
        private void Run()
        {
            Logger.Log("Start Running...");
            Logger.Log(string.Format("taskThreadNum is {0}", taskThreadNum));
            for (int f = 0; f < fileNames.Count; )
            {
                int todoTaskNum = (fileNames.Count - f > taskThreadNum) ? taskThreadNum : (fileNames.Count - f);
                doneEvents = new ManualResetEvent[todoTaskNum];
                for (int i = 0; i < doneEvents.Length; ++i)
                    doneEvents[i] = new ManualResetEvent(false);

                for (int i = 0; i < todoTaskNum; ++i)
                {
                    Shooter shooter = new Shooter(new FileInfo(fileNames[f + i]),
                        checkBox1.Checked, checkBox2.Checked,
                        f + i);
                    ThreadPool.QueueUserWorkItem(startSingleTask, shooter);
                }
                WaitHandle.WaitAll(doneEvents);
                f += todoTaskNum;
            }

            if (button1.InvokeRequired)
            {
                button1.Invoke(new MethodInvoker(delegate
                {
                    button1.Enabled = true;
                    button1.Text = "下载！";
                }));
            }
            if (button2.InvokeRequired)
            {
                button2.Invoke(new MethodInvoker(delegate
                {
                    button2.Enabled = true;
                }));
            }
            if (numericUpDown1.InvokeRequired)
            {
                numericUpDown1.Invoke(new MethodInvoker(delegate
                {
                    numericUpDown1.Enabled = true;
                }));
            }
            if (checkBox1.InvokeRequired)
            {
                checkBox1.Invoke(new MethodInvoker(delegate
                {
                    checkBox1.Enabled = true;
                }));
            }
            if (checkBox2.InvokeRequired)
            {
                checkBox2.Invoke(new MethodInvoker(delegate
                {
                    checkBox2.Enabled = true;
                }));
            }
            Logger.Log("Run finished.");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (!Shooter.canConnect())
            {
                Logger.Log("Cannot connect to shooter.cn");
                MessageBox.Show("无法连接到射手网，请检查网络连接。");
                return;
            }

            foreach (ListViewItem l in listView1.Items)
            {
                l.BackColor = label4.BackColor;
            }

            button1.Enabled = false;
            button1.Text = "下载中...";
            button2.Enabled = false;
            checkBox1.Enabled = false;
            checkBox2.Enabled = false;
            numericUpDown1.Enabled = false;
            this.taskThreadNum = Convert.ToInt32(numericUpDown1.Value);

            Thread t = new Thread(Run);
            t.Start();

            ManualResetEvent finish = new ManualResetEvent(false);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            listView1.Items.Clear();
            fileNames = new List<string>();
        }

    }
}
