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

namespace WindowsFormsApplication1
{
    public partial class MainWindows : Form
    {
        public MainWindows()
        {
            InitializeComponent();

            listView1.DragEnter += new DragEventHandler(listBox1_DragEnter);
            listView1.DragDrop += new DragEventHandler(listBox1_DragDrop);

            fileNames = new List<string>();
        }

        private void MainWindows_Load(object sender, EventArgs e)
        {

        }

        private void listBox1_DragEnter(object sender, DragEventArgs e)
        {
            Console.WriteLine("in DragEnter");
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
                                           ".mkv"
                                        };
        #endregion

        public class ColorListBoxItem
        {
            public Color ItemColor { get; set; }
            public string Message { get; set; }
            public ColorListBoxItem(Color c, string m)
            {
                ItemColor = c;
                Message = m;
            }
        }
        private void showFileNames()
        {
            listView1.Items.Clear();
            ColumnHeader header = new ColumnHeader();
            header.Text = "文件名";
            header.Name = "col1";
            //header.AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
            header.Width = 700;
            listView1.Columns.Add(header);
            foreach (string s in fileNames)
            {
                ListViewItem lvi = new ListViewItem(Path.GetFileName(s));
                lvi.BackColor = Color.White;
                //lvi.SubItems.Add(Path.GetFileName(s));

                //ListViewItem.ListViewSubItem llvi = new ListViewItem.ListViewSubItem();
                //llvi.Text = Path.GetFileName(s);
                //llvi.BackColor = Color.Red;
                //llvi.ForeColor = Color.Blue;
                //lvi.SubItems.Add(llvi);
                //lvi.Text = Path.GetFileName(s);
                //lvi.UseItemStyleForSubItems = false;
                listView1.Items.Add(lvi);
            }

            // scroll to bottom
            //int visibleItems = listBox1.ClientSize.Height / listBox1.ItemHeight;
            //listBox1.TopIndex = Math.Max(listBox1.Items.Count - visibleItems + 1, 0);
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }



        private ManualResetEvent[] doneEvents;
        private void startSingleTask(Object threadContext)
        {
            Shooter shooter = (Shooter)threadContext;
            shooter.startDownload();
            //MessageBox.Show(shooter.Status.ToString());
            //Console.WriteLine("shooterFileName: {0}", shooter.FileName);
            for (int i = 0; i < fileNames.Count; ++i)
            {
                //Console.WriteLine("fileNames[{0}] is {1}", i, fileNames[i]);
                if (shooter.FileName == fileNames[i])
                {
                    Console.WriteLine("Subs for video {0} is finished.", shooter.FileName);
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
                            listView1.Items[i].BackColor = c;
                        }));
                    }
                    
                    doneEvents[i].Set();
                }
            }
        }

        private void Run()
        {
            Console.WriteLine("Pushing all download task to ThreadPool...");
            doneEvents = new ManualResetEvent[fileNames.Count];
            for (int i = 0; i < doneEvents.Length; ++i)
                doneEvents[i] = new ManualResetEvent(false);
            for (int i = 0; i < fileNames.Count; ++i)
            {
                Shooter shooter = new Shooter(new FileInfo(fileNames[i]), checkBox1.Checked);
                ThreadPool.QueueUserWorkItem(startSingleTask, shooter);
                #region temp solution
                //Thread t = new Thread(shooter.startDownload);
                //t.Start();
                //t.Join();
                #endregion
            }
            WaitHandle.WaitAll(doneEvents);
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
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (!Shooter.canConnect())
            {
                MessageBox.Show("无法连接到射手网，请检查网络连接。");
                return;
            }
            Thread t = new Thread(Run);
            t.Start();
            button1.Enabled = false;
            button1.Text = "下载中...";
            button2.Enabled = false;
            ManualResetEvent finish = new ManualResetEvent(false);

        }

        private void button2_Click(object sender, EventArgs e)
        {
            listView1.Items.Clear();
            fileNames = new List<string>();
        }

        #region A not elegant way to use personal backgroud for items in listbox
        //private void listBox1_DrawItem(object sender, DrawItemEventArgs e)
        //{
        //    Console.WriteLine("debug point 1");
        //    if (listBox1.Items.Count <= 0)
        //    {
        //        Console.WriteLine("debug point 2");
        //        return;
        //    }
        //    ColorListBoxItem item = listBox1.Items[e.Index] as ColorListBoxItem; // Get the current item and cast it to MyListBoxItem

        //    Console.WriteLine("debug point 3");
        //    if (item != null)
        //    {

        //        e.DrawBackground();
        //        Graphics g = e.Graphics;

        //        g.FillRectangle(new SolidBrush(item.ItemColor), e.Bounds);

        //        // Print text

        //        e.DrawFocusRectangle();

        //        e.Graphics.DrawString( // Draw the appropriate text in the ListBox
        //            item.Message, // The message linked to the item
        //            listBox1.Font, // Take the font from the listbox
        //            new SolidBrush(Color.Black), // Set the color 
        //            e.Bounds, // X pixel coordinate
        //            StringFormat.GenericDefault // Y pixel coordinate.  Multiply the index by the ItemHeight defined in the listbox.
        //        );
        //    }
        //    else
        //    {
        //        // The item isn't a MyListBoxItem, do something about it
        //        Console.WriteLine("debug point 5");
        //    }
        //    Console.WriteLine("debug point 4");
        //} 
        #endregion

    }
}
