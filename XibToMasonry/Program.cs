using System;
using System.IO;
using XibToMasonry.Utils;

namespace XibToMasonry
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            if (args.Length > 0)
            {
                string path = args[0];
                if (Directory.Exists(path))
                {
                    var files = Directory.GetFiles(path, "*.xib", SearchOption.AllDirectories);
                    foreach (string file in files)
                    {
                        XibParser xibParser = new XibParser(file);
                        xibParser.SaveParser();
                    }
                } 
                else if (File.Exists(path))
                {
                    XibParser xibParser = new XibParser(path);
                    xibParser.SaveParser();
                }

            }
        }
    }
}
