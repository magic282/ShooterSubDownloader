using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DataPreprocessing
{
    class Program
    {
        public static bool IsFileInUse(string fileName)
        {
            bool inUse = true;

            FileStream fs = null;
            try
            {
                fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.None);
                inUse = false;
            }
            catch
            {
            }
            finally
            {
                if (fs != null)
                    fs.Close();
            }
            return inUse;
        }

        static string processTuple(List<string[]> tuples)
        {
            Regex regex = new Regex("[-+]?[0-9]*\\.?[0-9]+([eE][-+]?[0-9]+)?");
            int index = 0;
            double max = 0;
            for (int i = 0; i < tuples.Count; ++i)
            {
                string[] tmp = tuples[i][2].Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                Match match = regex.Match(tmp[tmp.Length - 3]);
                double confd = double.Parse(match.Value);
                match = regex.Match(tmp[tmp.Length - 2]);
                double overall = double.Parse(match.Value); ;
                if (max < confd + overall)
                {
                    index = i;
                    max = confd + overall;
                }
                else if (Math.Abs(max - confd - overall) < 1e-8 &&
                        tuples[i][1].Length < tuples[index][1].Length)
                {
                    index = i;
                }
            }
            return (tuples[index][0] + "\t" + tuples[index][1] + "\t" + tuples[index][2]);
        }

        static void Main(string[] args)
        {
            string inDir = @"\\graph013\EntityDescription\Scored\";
            //string outDir = @"D:\EntityDescription\Scored\";
            string outDir = inDir;
            string[] files = Directory.GetFileSystemEntries(inDir, "satori_*.desc", SearchOption.TopDirectoryOnly);

            foreach (var file in files)
            {
                string[] existFiles = Directory.GetFileSystemEntries(outDir, "satori_*.top1", SearchOption.TopDirectoryOnly);
                if (existFiles.Contains<string>(Path.GetFullPath(outDir + Path.GetFileNameWithoutExtension(file) + ".top1")))
                {
                    Console.WriteLine("Output file {0} already exists, skip.", Path.GetFullPath(outDir + Path.GetFileNameWithoutExtension(file) + ".top1"));
                    continue;
                }

                if (IsFileInUse(file))
                {
                    Console.WriteLine("Input file {0} is being used by anohter program, skip.", file);
                    continue;
                }


                Console.WriteLine("Processing {0}", Path.GetFileName(file));
                //output to: file+".top1"
                // 1) take the description with the highest score
                // 2) if two entities have the same score, take the shorter one


                using (StreamReader sr = new StreamReader(file))
                using (StreamWriter sw = new StreamWriter(outDir + Path.GetFileNameWithoutExtension(file) + ".top1"))
                {
                    if (sr.EndOfStream)
                        break;

                    int count = 0;
                    //int testMaxLine = 1000000;

                    List<string[]> tuples = new List<string[]>();
                    string line = sr.ReadLine();
                    string[] data = line.Split(new char[] { ' ', '\t' }, 3, StringSplitOptions.RemoveEmptyEntries);
                    tuples.Add(data);

                    while (!sr.EndOfStream)
                    {
                        line = sr.ReadLine();
                        count++;
                        data = line.Split(new char[] { ' ', '\t' }, 3, StringSplitOptions.RemoveEmptyEntries);

                        if (data[0] != tuples[0][0])
                        {
                            // find best
                            string ret = processTuple(tuples);
                            sw.WriteLine(ret);
                            // next
                            tuples = new List<string[]>();
                            tuples.Add(data);
                            continue;
                        }

                        tuples.Add(data);
                    }

                    if (tuples.Count != 0)
                    {
                        string ret = processTuple(tuples);
                        sw.WriteLine(ret);
                    }

                }

            }

        }
    }
}
