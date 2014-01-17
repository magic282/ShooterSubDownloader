﻿using System;
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

            listBox1.AllowDrop = true;
            listBox1.DragEnter += new DragEventHandler(listBox1_DragEnter);
            listBox1.DragDrop += new DragEventHandler(listBox1_DragDrop);

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

        private void showFileNames()
        {
            listBox1.Items.Clear();
            foreach (string s in fileNames)
            {
                listBox1.Items.Add(Path.GetFileName(s));
            }

            // scroll to bottom
            int visibleItems = listBox1.ClientSize.Height / listBox1.ItemHeight;
            listBox1.TopIndex = Math.Max(listBox1.Items.Count - visibleItems + 1, 0);
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (!Shooter.canConnect())
            {
                MessageBox.Show("无法连接到射手网，请检查网络连接。");
                return;
            }
            foreach (string s in fileNames)
            {
                Shooter shooter = new Shooter(new FileInfo(s),checkBox1.Checked);

                #region temp solution
                Thread t = new Thread(shooter.startDownload);
                t.Start();
                #endregion
            }
            listBox1.Items[0].ToString();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            listBox1.Items.Clear();
            fileNames = new List<string>();
        }

    }
}
