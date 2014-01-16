using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApplication1
{

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
        public Shooter(FileInfo fileInfo)
        {
            string hashValue = SVPlayerHash.ComputeFileHash(fileInfo);
            action(hashValue, fileInfo, language.Chn);
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
    }
}
