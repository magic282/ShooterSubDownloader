using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace WindowsFormsApplication1
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

        private const string url = "https://www.shooter.cn/api/subapi.php";
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
        private returnStatus status;
        private string hashValue;
        private Subinfo[] subinfoChn;
        private Subinfo[] subinfoEng;
        public Shooter(FileInfo fileInfo, bool enableEngSub)
        {
            this.videoFile = fileInfo;
            this.enableEngSub = enableEngSub;
            this.status = returnStatus.Unknown;
            this.hashValue = SVPlayerHash.ComputeFileHash(fileInfo);

            //Thread t = new Thread(action);
            //t.Start();
        }

        public void startDownload()
        {
            Down();
        }

        internal returnStatus Status
        {
            get { return status; }
            set { status = value; }
        }

        private void Down()
        {
            int count = 0;
            try
            {
                #region Download Chinese subtitles
                if (subinfoChn != null)
                {

                    WebClient client = new WebClient();
                    string dir = videoFile.DirectoryName;
                    string subFileNameBase = Path.GetFileNameWithoutExtension(videoFile.FullName);
                    Console.WriteLine("Get {0} subs returned.", subinfoChn.Length);
                    for (int i = 0; i < subinfoChn.Length; ++i)
                    {
                        Console.WriteLine("Downloading {0} file", i);
                        for (int j = 0; j < subinfoChn[i].Files.Length; ++j)
                        {
                            Console.WriteLine(subinfoChn[i].Files[j].Ext);
                            Console.WriteLine(subinfoChn[i].Files[j].Link);
                            string subFileName = subFileNameBase +
                                ".chn" + (i == 0 ? "" : string.Format("{0}", i)) +
                                "." + subinfoChn[i].Files[j].Ext;
                            client.DownloadFile(new Uri(subinfoChn[i].Files[j].Link),
                                dir + Path.DirectorySeparatorChar + subFileName);
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
                    Console.WriteLine("Get {0} subs returned.", subinfoEng.Length);
                    for (int i = 0; i < subinfoEng.Length; ++i)
                    {
                        Console.WriteLine("Downloading {0} file", i);
                        for (int j = 0; j < subinfoEng[i].Files.Length; ++j)
                        {
                            Console.WriteLine(subinfoEng[i].Files[j].Ext);
                            Console.WriteLine(subinfoEng[i].Files[j].Link);
                            string subFileName = subFileNameBase +
                                ".eng" + (i == 0 ? "" : string.Format("{0}", i)) +
                                "." + subinfoEng[i].Files[j].Ext;
                            client.DownloadFile(new Uri(subinfoEng[i].Files[j].Link),
                                dir + Path.DirectorySeparatorChar + subFileName);
                        }
                    }
                } 
                #endregion
            }
            catch
            {
                status = returnStatus.DownloadFailed;
                return;
            }

            if (count > 0)
            {
                status = returnStatus.Success;
            }
            else
            {
                status = returnStatus.NoSubtitle;
            }

        }


        /// <summary>
        /// waiting for refactoring
        /// </summary>
        /// <param name="hashValue"></param>
        /// <param name="fileInfo"></param>
        /// <param name="downEngSub"></param>
        private void action()
        {
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
                subinfoChn = JsonHelper.FromJson<Subinfo[]>(retString);

                #region result debug output
                //foreach (Subinfo sub in subinfoChn)
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

        }

        public static bool canConnect()
        {
            Uri url = new Uri("https://www.shooter.cn/");
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
