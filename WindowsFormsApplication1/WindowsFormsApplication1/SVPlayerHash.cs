using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApplication1
{
    class SVPlayerHash
    {
        public static string ComputeFileHash(FileInfo fileInfo)
        {
            string ret = "";
            long[] offset = new long[4];
            if (!fileInfo.Exists || fileInfo.Length < 8 * 1024)
            {
                return null;
            }

            offset[3] = fileInfo.Length - 8 * 1024;
            offset[2] = fileInfo.Length / 3;
            offset[1] = fileInfo.Length / 3 * 2;
            offset[0] = 4 * 1024;

            byte[] bBuf = new byte[1024 * 4];
            FileStream fs = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read);
            for (int i = 0; i < 4; ++i)
            {
                fs.Seek(offset[i], 0);
                int readlen = fs.Read(bBuf, 0, 4 * 1024);

                MD5 md5Hash = MD5.Create();
                byte[] data = md5Hash.ComputeHash(bBuf);
                StringBuilder sBuilder = new StringBuilder();

                // Loop through each byte of the hashed data  
                // and format each one as a hexadecimal string. 
                for (int j = 0; j < data.Length; j++)
                {
                    sBuilder.Append(data[j].ToString("x2"));
                }
                                
                if (!string.IsNullOrEmpty(ret))
                {
                    ret += ";";
                }
                ret += sBuilder.ToString();
            }
            
            return ret;
        }
    }
}
