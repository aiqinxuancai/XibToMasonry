using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace XibToMasonry.Utils
{
    public class XibParser
    {

        private JObject _rootJson;
        private XmlDocument _rootXml;
        private XmlElement _controlNameXml;
        private JArray _controlNameJson;
        private int _fakeNameIndex;

        private ConnectionsParser _connectionsParser;

        public XibParser(string filePath) {
            //读入
            _fakeNameIndex = 0;
            string xml = File.ReadAllText(filePath);
           

            //解析 connections
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            _rootXml = doc;


           string jsonText = JsonConvert.SerializeXmlNode(doc);
           Console.WriteLine(jsonText);


            //实际已经全解析，只需将对应的数据写出
            _rootJson = JObject.Parse(jsonText);


            //生成类声明结构


            //生成类内容结构

            //生成bodyView的
            //将名称添加至subviews

            //将所有控件命名

            WriteInterFace();

        }

        //生成声明
        private void WriteInterFace()
        {
            //@property(nonatomic, strong) UIView *outsideView;
            //从关联的.m文件中提取操作代码

            JObject mainView = (JObject)_rootJson["document"]["objects"]["view"];
            //JObject mainView = (JObject)_rootJson["document"]["objects"]["view"];
            XmlElement mainViewXml = _rootXml["document"]["objects"]["view"];

            //寻找主View


            RunOneView(mainViewXml, true);



        }


        //将view的id属性转换成实际名字
        private string IdToName(string id)
        {
            string name = @$"fakeIName{_fakeNameIndex}"; //FakeIName
            if (_controlNameXml != null)
            {
                foreach (XmlElement item in _controlNameXml.ChildNodes)
                {
                    if (item.GetAttribute("destination") == id)
                    {
                        name = item.GetAttribute("property");
                        break;
                    }
                }
            }
            _fakeNameIndex++;
            return name;
        }

        private string GetOCClassName(string nodeName, string customClassName)
        {
            if (string.IsNullOrWhiteSpace(customClassName) == false)
            {
                return customClassName;
            } 
            else
            {
                //string name = nodeName switch
                //{
                //    "button" => "UIButton",
                //    _ => ""
                //};
                string name = "UI" + System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(nodeName);
                return name;
            }
        }

        private void RunOneView(XmlElement xml, bool isMain)
        {
            //取当前的角色

            string name = xml.Name;
            string id = xml.GetAttribute("id");
            string propertyName = IdToName(id);
            string customClass = xml.GetAttribute("customClass");
            string className = GetOCClassName(name, customClass);

            Console.WriteLine(@$"{className} {propertyName}");

            if (isMain)
            {
                //主View是self.view
                _controlNameXml = xml["connections"];
            }

            

            xml.SetAttribute("xibToMasonryNamePropertyName", propertyName);
            //处理子view

            XmlElement subView = xml["subviews"];
            if (subView != null)
            {
                foreach (XmlElement item in subView.ChildNodes)
                {
                    RunOneView(item, false);
                }
            }
            //Console.WriteLine(subView);
        }



        //private List<JObject> GetAllSubView()
        //{
        //    //无视层级，将所有取出
        //}



        //生成懒加载
        //生成AddSubview
        //生成mas布局




    }
}
