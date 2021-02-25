using System;
using System.IO;
using XibToMasonry.Utils;

namespace XibToMasonry
{
    class Program
    {
        static void Main(string[] args)
        {
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
                        Console.WriteLine("clear!");
                    }
                } 
                else if (File.Exists(path))
                {
                    XibParser xibParser = new XibParser(path);
                    xibParser.SaveParser();
                    Console.WriteLine("clear!");
                }
                Console.WriteLine("conversion complete.");
            } 
            else
            {
                Console.WriteLine("pls input xib file path.");
            }
        }
    }
}
