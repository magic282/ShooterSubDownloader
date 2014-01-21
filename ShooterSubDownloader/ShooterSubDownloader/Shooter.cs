using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.Serialization;
using System.Text;
using System.Diagnostics;
using System.Threading;

namespace ShooterSubDownloader
{

    /// <summary>
    /// https://docs.google.com/document/d/1ufdzy6jbornkXxsD-OGl3kgWa4P9WO5NZb6_QYZiGI0/preview
    /// </summary>
    class Shooter
    {
        [DataContract]
        class Fileinfo
        {
            [DataMember]
            public string Ext { get; set; }
            [DataMember]
            public string Link { get; set; }
        }

        [DataContract]
        class Subinfo
        {
            [DataMember]
            public string Desc { get; set; }
            [DataMember]
            public int Delay { get; set; }
            [DataMember]
            public Fileinfo[] Files { get; set; }
        }

        private const string url = "http://shooter.cn/api/subapi.php";
        internal enum language
        {
            Chn,
            eng
        };

        internal enum returnStatus
        {
            Success,
            NoSubtitle,
            DownloadFailed,
            Unknown
        };

        private FileInfo videoFile;
        private bool enableEngSub;
        private bool enbaleOnlyOneSub;
        private int taskIndex;
        private returnStatus status;
        private string hashValue;
        private Subinfo[] subinfoChn;
        private Subinfo[] subinfoEng;

        public string FileName
        {
            get { return videoFile.FullName; }
        }

        public int TaskIndex
        {
            get { return this.taskIndex; }
        }
        public Shooter(FileInfo fileInfo, bool enableEngSub, bool enableOnlyOneSub, int taskIndex)
        {
            this.videoFile = fileInfo;
            this.enableEngSub = enableEngSub;
            this.enbaleOnlyOneSub = enableOnlyOneSub;
            this.taskIndex = taskIndex;
            this.status = returnStatus.Unknown;
            this.hashValue = SVPlayerHash.ComputeFileHash(fileInfo);

            //Thread t = new Thread(action);
            //t.Start();
        }

        public void startDownload()
        {
            Logger.Log(string.Format("Shooter working for {0}", Path.GetFileNameWithoutExtension(videoFile.FullName)));
            try
            {
                getSubInfoFromShooter();
            }
            catch (Exception e)
            {
                status = returnStatus.DownloadFailed;
                Logger.Log("Exception when getting sub info");
                Logger.Log(e.GetType().ToString());
                Logger.Log(e.Message);
                Logger.Log(e.StackTrace);
                return;
            }

            try
            {
                if (enbaleOnlyOneSub)
                {
                    DownFirstSub();
                }
                else
                {
                    Down();
                }

            }
            catch (Exception e)
            {
                status = returnStatus.DownloadFailed;
                Logger.Log("Exception when downloading");
                Logger.Log(e.GetType().ToString());
                Logger.Log(e.Message);
                Logger.Log(e.StackTrace);
                return;
            }

            Logger.Log(string.Format("Shooter finsihed for {0}", Path.GetFileNameWithoutExtension(videoFile.FullName)));
        }

        internal returnStatus Status
        {
            get { return status; }
            set { status = value; }
        }

        private void Down()
        {
            int count = 0;
            Logger.Log(string.Format("starting download for {0}",
                Path.GetFileNameWithoutExtension(videoFile.FullName)));

            int expectCnt = 0;
            if (subinfoChn != null)
            {
                foreach (Subinfo sub in subinfoChn)
                {
                    expectCnt += sub.Files.Length;
                }
            }
            if (enableEngSub && subinfoEng != null)
            {
                foreach (Subinfo sub in subinfoEng)
                {
                    expectCnt += sub.Files.Length;
                }
            }

            if (expectCnt <= 0)
            {
                status = returnStatus.NoSubtitle;
                return;
            }

            #region Download Chinese subtitles
            if (subinfoChn != null)
            {

                WebClient client = new WebClient();
                string dir = videoFile.DirectoryName;
                string subFileNameBase = Path.GetFileNameWithoutExtension(videoFile.FullName);
                Logger.Log(string.Format("Get {0} subs returned.", subinfoChn.Length));
                for (int i = 0; i < subinfoChn.Length; ++i)
                {
                    Logger.Log(string.Format("Downloading {0} file", i));
                    for (int j = 0; j < subinfoChn[i].Files.Length; ++j)
                    {
                        Console.WriteLine(subinfoChn[i].Files[j].Ext);
                        Logger.Log(subinfoChn[i].Files[j].Ext);
                        Console.WriteLine(subinfoChn[i].Files[j].Link);
                        Logger.Log(subinfoChn[i].Files[j].Link);
                        string subFileName = subFileNameBase +
                            ".chn" + (i == 0 ? "" : string.Format("{0}", i)) +
                            "." + subinfoChn[i].Files[j].Ext;

                        try
                        {
                            client.DownloadFile(new Uri(subinfoChn[i].Files[j].Link),
                                                dir + Path.DirectorySeparatorChar + subFileName);
                            if (subinfoChn[i].Delay != 0)
                            {
                                string delayFileName = subFileName + ".delay";
                                FileStream delayFile = new FileStream(
                                    dir + Path.DirectorySeparatorChar + delayFileName, FileMode.OpenOrCreate);
                                StreamWriter sw = new StreamWriter(delayFile);
                                sw.Write(subinfoChn[i].Delay);
                                sw.Flush();
                                sw.Close();
                                delayFile.Close();
                            }
                            ++count;
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Caught exception while downloading.");
                            Console.WriteLine(e.GetType().ToString());
                            Console.WriteLine(e.Message);
                            Console.WriteLine(e.StackTrace);

                            Logger.Log("Caught exception while downloading.");
                            Logger.Log(e.GetType().ToString());
                            Logger.Log(e.Message);
                            Logger.Log(e.StackTrace);
                            //status = returnStatus.DownloadFailed;
                            //return;
                        }
                    }
                }
            }
            #endregion

            #region Download English subtitles
            if (enableEngSub && subinfoEng != null)
            {
                WebClient client = new WebClient();
                string dir = videoFile.DirectoryName;
                string subFileNameBase = Path.GetFileNameWithoutExtension(videoFile.FullName);
                Logger.Log(string.Format("Get {0} subs returned.", subinfoEng.Length));
                for (int i = 0; i < subinfoEng.Length; ++i)
                {
                    Logger.Log(string.Format("Downloading {0} file", i));
                    for (int j = 0; j < subinfoEng[i].Files.Length; ++j)
                    {
                        Console.WriteLine(subinfoEng[i].Files[j].Ext);
                        Logger.Log(subinfoEng[i].Files[j].Ext);
                        Console.WriteLine(subinfoEng[i].Files[j].Link);
                        Logger.Log(subinfoEng[i].Files[j].Link);
                        string subFileName = subFileNameBase +
                            ".eng" + (i == 0 ? "" : string.Format("{0}", i)) +
                            "." + subinfoEng[i].Files[j].Ext;

                        try
                        {
                            client.DownloadFile(new Uri(subinfoEng[i].Files[j].Link),
                                              dir + Path.DirectorySeparatorChar + subFileName);
                            ++count;
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Caught exception while downloading.");
                            Console.WriteLine(e.GetType().ToString());
                            Console.WriteLine(e.Message);
                            Console.WriteLine(e.StackTrace);

                            Logger.Log("Caught exception while downloading.");
                            Logger.Log(e.GetType().ToString());
                            Logger.Log(e.Message);
                            Logger.Log(e.StackTrace);
                            //status = returnStatus.DownloadFailed;
                            //return;
                        }
                    }
                }
            }
            #endregion



            if (count > 0)
            {
                status = returnStatus.Success;
            }
            else
            {
                status = returnStatus.DownloadFailed;
            }
            Logger.Log(string.Format("Download {0} files for {1} in total.", count,
                Path.GetFileNameWithoutExtension(videoFile.FullName)));
            Logger.Log("download finished.");

        }

        private void DownFirstSub()
        {
            int count = 0;
            Logger.Log(string.Format("starting download for {0}",
                Path.GetFileNameWithoutExtension(videoFile.FullName)));

            int expectCnt = 0;
            if (subinfoChn != null)
            {
                foreach (Subinfo sub in subinfoChn)
                {
                    expectCnt += sub.Files.Length;
                }
            }
            if (enableEngSub && subinfoEng != null)
            {
                foreach (Subinfo sub in subinfoEng)
                {
                    expectCnt += sub.Files.Length;
                }
            }

            if (expectCnt <= 0)
            {
                status = returnStatus.NoSubtitle;
                return;
            }

            #region Download Chinese subtitles
            if (subinfoChn != null)
            {

                WebClient client = new WebClient();
                string dir = videoFile.DirectoryName;
                string subFileNameBase = Path.GetFileNameWithoutExtension(videoFile.FullName);
                Logger.Log(string.Format("Get {0} subs returned.", subinfoChn.Length));

                Logger.Log(string.Format("Downloading {0} file", 0));

                Console.WriteLine(subinfoChn[0].Files[0].Ext);
                Logger.Log(subinfoChn[0].Files[0].Ext);
                Console.WriteLine(subinfoChn[0].Files[0].Link);
                Logger.Log(subinfoChn[0].Files[0].Link);
                string subFileName = subFileNameBase +
                    "." + subinfoChn[0].Files[0].Ext;

                try
                {
                    client.DownloadFile(new Uri(subinfoChn[0].Files[0].Link),
                                        dir + Path.DirectorySeparatorChar + subFileName);
                    if (subinfoChn[0].Delay != 0)
                    {
                        string delayFileName = subFileName + ".delay";
                        FileStream delayFile = new FileStream(
                            dir + Path.DirectorySeparatorChar + delayFileName, FileMode.OpenOrCreate);
                        StreamWriter sw = new StreamWriter(delayFile);
                        sw.Write(subinfoChn[0].Delay);
                        sw.Flush();
                        sw.Close();
                        delayFile.Close();
                    }
                    ++count;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Caught exception while downloading.");
                    Console.WriteLine(e.GetType().ToString());
                    Console.WriteLine(e.Message);
                    Console.WriteLine(e.StackTrace);

                    Logger.Log("Caught exception while downloading.");
                    Logger.Log(e.GetType().ToString());
                    Logger.Log(e.Message);
                    Logger.Log(e.StackTrace);
                    //status = returnStatus.DownloadFailed;
                    //return;
                }


            }
            #endregion

            if (count > 0)
            {
                status = returnStatus.Success;
            }
            else
            {
                status = returnStatus.DownloadFailed;
            }
            Logger.Log(string.Format("Download {0} files for {1} in total.", count,
                Path.GetFileNameWithoutExtension(videoFile.FullName)));
            Logger.Log("download finished.");

        }


        /// <summary>
        /// waiting for refactoring
        /// </summary>
        /// <param name="hashValue"></param>
        /// <param name="fileInfo"></param>
        /// <param name="downEngSub"></param>
        private void getSubInfoFromShooter()
        {
            Logger.Log("Start getSubInfoFromShooter...");
            #region download Chinese subInfo
            using (var wb = new WebClient())
            {
                var data = new NameValueCollection();
                data["filehash"] = hashValue;
                data["pathinfo"] = videoFile.FullName;
                data["format"] = "json";
                data["lang"] = language.Chn.ToString();

                var response = wb.UploadValues(url, "POST", data);

                if (response.Length == 1 && response[0] == 0xff)
                {
                    subinfoChn = null;
                    goto label1;
                }

                string retString = Encoding.UTF8.GetString(response);
                Console.WriteLine(retString);
                Logger.Log(retString);
                subinfoChn = JsonHelper.FromJson<Subinfo[]>(retString);

                #region result debug output
                //foreach (Subinfo sub in subinfoChn)
                //{
                //    Console.WriteLine(sub.Desc);
                //    Logger.Log(sub.Desc);
                //    foreach (Fileinfo file in sub.Files)
                //    {
                //        Console.WriteLine(file.Ext);
                //        Console.WriteLine(file.Link);
                //        Logger.Log(file.Ext);
                //        Logger.Log(file.Link);
                //    }
                //}
                #endregion
            }
            #endregion

        label1:
            #region download English sub
            if (enableEngSub)
            {
                using (var wb = new WebClient())
                {
                    var data = new NameValueCollection();
                    data["filehash"] = hashValue;
                    data["pathinfo"] = videoFile.FullName;
                    data["format"] = "json";
                    data["lang"] = language.eng.ToString();

                    var response = wb.UploadValues(url, "POST", data);


                    if (response.Length == 1 && response[0] == 0xff)
                    {
                        subinfoEng = null;
                        return;
                    }

                    string retString = Encoding.UTF8.GetString(response);
                    Console.WriteLine(retString);
                    Logger.Log(retString);
                    subinfoEng = JsonHelper.FromJson<Subinfo[]>(retString);

                    #region result debug output
                    //foreach (Subinfo sub in subinfoEng)
                    //{
                    //    Console.WriteLine(sub.Desc);
                    //    foreach (Fileinfo file in sub.Files)
                    //    {
                    //        Console.WriteLine(file.Ext);
                    //        Console.WriteLine(file.Link);
                    //    }
                    //}
                    #endregion
                }
            }
            #endregion
            Console.WriteLine("getSubInfoFromShooter finished.");
            Logger.Log("getSubInfoFromShooter finished.");
        }

        public static bool canConnect()
        {
            Uri url = new Uri("http://shooter.cn/");
            string pingurl = string.Format("{0}", url.Host);
            string host = pingurl;
            bool result = false;
            Ping p = new Ping();
            try
            {
                PingReply reply = p.Send(host, 3000);
                if (reply.Status == IPStatus.Success)
                    return true;
            }
            catch { }
            return result;
        }
    }
}
