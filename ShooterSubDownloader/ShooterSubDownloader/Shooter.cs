using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

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
            Eng
        };

        private FileInfo videoFile;
        private string hashValue;
        public Shooter(FileInfo fileInfo)
        {
            this.videoFile = fileInfo;
            string hashValue = SVPlayerHash.ComputeFileHash(fileInfo);
            action(hashValue, fileInfo, language.Chn);
        }

        public void Down()
        {

        }


        private void action(string hashValue, FileInfo fileInfo, language lang)
        {
            using (var wb = new WebClient())
            {
                var data = new NameValueCollection();
                data["filehash"] = hashValue;
                data["pathinfo"] = fileInfo.FullName;
                data["format"] = "json";
                data["lang"] = lang.ToString();

                var response = wb.UploadValues(url, "POST", data);
                string retString = Encoding.UTF8.GetString(response);
                Console.WriteLine(retString);
                Subinfo[] subinfo = JsonHelper.FromJson<Subinfo[]>(retString);
                foreach (Subinfo sub in subinfo)
                {
                    Console.WriteLine(sub.Desc);
                    foreach (Fileinfo file in sub.Files)
                    {
                        Console.WriteLine(file.Ext);
                        Console.WriteLine(file.Link);
                    }
                }
            }
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
