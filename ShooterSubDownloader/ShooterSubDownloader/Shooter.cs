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
        #region Json DataContracts
        [DataContract]
        class SubFileInfo
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
            public SubFileInfo[] Files { get; set; }
        }
        #endregion

        private const string url = "http://shooter.cn/api/subapi.php";
        internal enum SubLanguage
        {
            Chn, // The C must be capitalized
            eng,
        };

        internal enum ReturnStatus
        {
            Success,
            NoSubtitle,
            DownloadFailed,
            Unknown,
        };

        private FileInfo videoFile;
        private bool enableEngSub;
        private bool enbaleOnlyOneSub;
        private int taskIndex;
        private ReturnStatus status;
        private string hashValue;
        private Subinfo[] subInfoChn;
        private Subinfo[] subInfoEng;

        private const int TryLimit = 3;

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
            this.status = ReturnStatus.Unknown;
            this.hashValue = SVPlayerHash.ComputeFileHash(fileInfo);

            //Thread t = new Thread(action);
            //t.Start();
        }

        public void startDownload()
        {
            Logger.Log(string.Format("Shooter working for {0}", Path.GetFileNameWithoutExtension(videoFile.FullName)));
            try
            {
                getSubInfoFromShooterEntry();
            }
            catch (Exception e)
            {
                status = ReturnStatus.DownloadFailed;
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
                    DownloadFirstSubEntry();
                }
                else
                {
                    DownloadEntry();
                }

            }
            catch (Exception e)
            {
                status = ReturnStatus.DownloadFailed;
                Logger.Log("Exception when downloading");
                Logger.Log(e.GetType().ToString());
                Logger.Log(e.Message);
                Logger.Log(e.StackTrace);
                return;
            }

            Logger.Log(string.Format("Shooter finsihed for {0}", Path.GetFileNameWithoutExtension(videoFile.FullName)));
        }

        internal ReturnStatus Status
        {
            get { return status; }
            set { status = value; }
        }

        private bool DownLoad(Subinfo sub, int subIndex, SubLanguage lan)
        {
            WebClient client = new WebClient();
            string dir = videoFile.DirectoryName;
            string subFileNameBase = Path.GetFileNameWithoutExtension(videoFile.FullName);

            Logger.Log(string.Format("Downloading {0} file", subIndex));
            for (int j = 0; j < sub.Files.Length; ++j)
            {
                Console.WriteLine(sub.Files[j].Ext);
                Logger.Log(sub.Files[j].Ext);
                Console.WriteLine(sub.Files[j].Link);
                Logger.Log(sub.Files[j].Link);
                string subFileName = subFileNameBase +
                    "." + lan.ToString().ToLower() + (subIndex == 0 ? "" : string.Format("{0}", subIndex)) +
                    "." + sub.Files[j].Ext;

                try
                {
                    client.DownloadFile(new Uri(sub.Files[j].Link),
                                        dir + Path.DirectorySeparatorChar + subFileName);
                    if (sub.Delay != 0)
                    {
                        string delayFileName = subFileName + ".delay";
                        FileStream delayFile = new FileStream(
                            dir + Path.DirectorySeparatorChar + delayFileName, FileMode.OpenOrCreate);
                        StreamWriter sw = new StreamWriter(delayFile);
                        sw.Write(sub.Delay);
                        sw.Flush();
                        sw.Close();
                        delayFile.Close();
                    }
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
                    return false;
                }
            }
            return true;
        }

        private int GetExpectDownloadNumber()
        {
            int expectCnt = 0;
            if (subInfoChn != null)
            {
                foreach (Subinfo sub in subInfoChn)
                {
                    expectCnt += sub.Files.Length;
                }
            }
            if (enableEngSub && subInfoEng != null)
            {
                foreach (Subinfo sub in subInfoEng)
                {
                    expectCnt += sub.Files.Length;
                }
            }
            return expectCnt;
        }

        private void DownloadEntry()
        {
            int actualDownloadCount = 0;
            Logger.Log(string.Format("starting download for {0}",
                Path.GetFileNameWithoutExtension(videoFile.FullName)));

            int expectCnt = GetExpectDownloadNumber();

            if (expectCnt <= 0)
            {
                status = ReturnStatus.NoSubtitle;
                return;
            }

            #region Download Chinese subtitles
            if (subInfoChn != null)
            {
                WebClient client = new WebClient();
                string dir = videoFile.DirectoryName;
                string subFileNameBase = Path.GetFileNameWithoutExtension(videoFile.FullName);
                Logger.Log(string.Format("Get {0} subs returned.", subInfoChn.Length));
                for (int i = 0; i < subInfoChn.Length; ++i)
                {
                    bool download_status = false;
                    for (int k = 0; k < TryLimit; ++k)
                    {
                        download_status = DownLoad(subInfoChn[i], i, SubLanguage.Chn);
                        Logger.Log(string.Format("Try download {0} , is successful: [{1}].", subInfoChn[i].Desc, download_status));
                        if (download_status)
                        {
                            break;
                        }
                    }
                    if (!download_status)
                    {
                        Logger.Log(string.Format("Download {0} exceed try limit.", subInfoChn[i].Desc));
                    }
                    else
                    {
                        ++actualDownloadCount;
                    }
                }
            }
            #endregion

            #region Download English subtitles
            if (enableEngSub && subInfoEng != null)
            {
                WebClient client = new WebClient();
                string dir = videoFile.DirectoryName;
                string subFileNameBase = Path.GetFileNameWithoutExtension(videoFile.FullName);
                Logger.Log(string.Format("Get {0} subs returned.", subInfoEng.Length));
                for (int i = 0; i < subInfoEng.Length; ++i)
                {
                    bool download_status = false;
                    for (int k = 0; k < TryLimit; ++k)
                    {
                        download_status = DownLoad(subInfoEng[i], i, SubLanguage.Chn);
                        Logger.Log(string.Format("Try download {0} , is successful: [{1}].", subInfoEng[i].Desc, download_status));
                        if (download_status)
                        {
                            break;
                        }
                    }
                    if (!download_status)
                    {
                        Logger.Log(string.Format("Download {0} exceed try limit.", subInfoEng[i].Desc));
                    }
                    else
                    {
                        ++actualDownloadCount;
                    }
                }
            }
            #endregion

            if (actualDownloadCount > 0)
            {
                status = ReturnStatus.Success;
            }
            else
            {
                status = ReturnStatus.DownloadFailed;
            }

            Logger.Log(string.Format("Download {0} files for {1} in total.", actualDownloadCount,
                Path.GetFileNameWithoutExtension(videoFile.FullName)));
            Logger.Log("download finished.");
        }

        private void DownloadFirstSubEntry()
        {
            int count = 0;
            Logger.Log(string.Format("starting download for {0}",
                Path.GetFileNameWithoutExtension(videoFile.FullName)));

            int expectCnt = GetExpectDownloadNumber();

            if (expectCnt <= 0)
            {
                status = ReturnStatus.NoSubtitle;
                return;
            }

            #region Download Chinese subtitles
            if (subInfoChn != null)
            {

                WebClient client = new WebClient();
                string dir = videoFile.DirectoryName;
                string subFileNameBase = Path.GetFileNameWithoutExtension(videoFile.FullName);
                Logger.Log(string.Format("Get {0} subs returned.", subInfoChn.Length));

                Logger.Log(string.Format("Downloading {0} file", 1));

                Console.WriteLine(subInfoChn[0].Files[0].Ext);
                Logger.Log(subInfoChn[0].Files[0].Ext);
                Console.WriteLine(subInfoChn[0].Files[0].Link);
                Logger.Log(subInfoChn[0].Files[0].Link);
                string subFileName = subFileNameBase +
                    "." + subInfoChn[0].Files[0].Ext;

                try
                {
                    client.DownloadFile(new Uri(subInfoChn[0].Files[0].Link),
                                        dir + Path.DirectorySeparatorChar + subFileName);
                    if (subInfoChn[0].Delay != 0)
                    {
                        string delayFileName = subFileName + ".delay";
                        FileStream delayFile = new FileStream(
                            dir + Path.DirectorySeparatorChar + delayFileName, FileMode.OpenOrCreate);
                        StreamWriter sw = new StreamWriter(delayFile);
                        sw.Write(subInfoChn[0].Delay);
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

            #region Download English subtitles
            if (enableEngSub && subInfoEng != null)
            {
                WebClient client = new WebClient();
                string dir = videoFile.DirectoryName;
                string subFileNameBase = Path.GetFileNameWithoutExtension(videoFile.FullName);
                Logger.Log(string.Format("Get {0} subs returned.", subInfoEng.Length));

                Logger.Log(string.Format("Downloading {0} file", 1));

                Console.WriteLine(subInfoEng[0].Files[0].Ext);
                Logger.Log(subInfoEng[0].Files[0].Ext);
                Console.WriteLine(subInfoEng[0].Files[0].Link);
                Logger.Log(subInfoEng[0].Files[0].Link);
                string subFileName = subFileNameBase +
                    ".eng" + "." + subInfoEng[0].Files[0].Ext;

                try
                {
                    client.DownloadFile(new Uri(subInfoEng[0].Files[0].Link),
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
            #endregion

            if (count > 0)
            {
                status = ReturnStatus.Success;
            }
            else
            {
                status = ReturnStatus.DownloadFailed;
            }
            Logger.Log(string.Format("Download {0} files for {1} in total.", count,
                Path.GetFileNameWithoutExtension(videoFile.FullName)));
            Logger.Log("download finished.");

        }

        private void getSubInfoFromShooterEntry(SubLanguage lan)
        {
            using (WebClient wb = new WebClient())
            {
                var data = new NameValueCollection();
                data["filehash"] = hashValue;
                data["pathinfo"] = videoFile.FullName;
                data["format"] = "json";
                data["lang"] = lan.ToString();

                var response = wb.UploadValues(url, "POST", data);
                string retString = Encoding.UTF8.GetString(response);
                Console.WriteLine(retString);
                Logger.Log(retString);

                if (!(response.Length == 1 && response[0] == 0xff))
                {
                    if (lan == SubLanguage.Chn)
                        subInfoChn = JsonHelper.FromJson<Subinfo[]>(retString);
                    else if (lan == SubLanguage.eng)
                        subInfoEng = JsonHelper.FromJson<Subinfo[]>(retString);
                }
                else
                {
                    if (lan == SubLanguage.Chn)
                        subInfoChn = null;
                    else if (lan == SubLanguage.eng)
                        subInfoEng = null;
                }
            }
        }

        private void getSubInfoFromShooterEntry()
        {
            Logger.Log("Start getSubInfoFromShooter...");

            getSubInfoFromShooterEntry(SubLanguage.Chn);
            if (enableEngSub)
            {
                getSubInfoFromShooterEntry(SubLanguage.eng);
            }

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
