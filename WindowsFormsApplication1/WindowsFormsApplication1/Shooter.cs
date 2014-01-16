using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApplication1
{
    class Shooter
    {
        public Shooter(FileInfo fileInfo)
        {
            string hashValue = SVPlayerHash.ComputeFileHash(fileInfo);
            Console.WriteLine(hashValue);
        }
    }
}
