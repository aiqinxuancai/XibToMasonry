using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using XibToMasonry.Parser;
 
namespace XibToMasonry.Utils
{
    public class XibParser
    {

        private XmlDocument _rootXml;

        private string _xibFilePath;



        public XibParser(string filePath)
        {
            //读入

            _xibFilePath = filePath;
            string xml = File.ReadAllText(filePath);

            //解析 connections
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            _rootXml = doc;


            Console.WriteLine($"Start converting files:{filePath}");

            try
            {
                StartXibParser();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

        }


        //生成声明
        public void StartXibParser()
        {
            
            XmlElement scenes = _rootXml["document"]["scenes"]; //storyboard
            if (scenes != null)
            {
                Console.WriteLine("");
                //多个objects
                foreach (var item in scenes.ChildNodes)
                {
                    if (item.GetType() == typeof(XmlElement))
                    {
                        ViewParser viewParser = new ViewParser(((XmlElement)item)["objects"], _xibFilePath);
                        viewParser.StartParser();
                        viewParser.SaveParser();
                    }

                }
            }
            else
            {
                XmlElement mainViewXml = _rootXml["document"]["objects"];
                ViewParser viewParser = new ViewParser(mainViewXml, _xibFilePath);
                viewParser.StartParser();
                viewParser.SaveParser();
            }
        }
    }
}
