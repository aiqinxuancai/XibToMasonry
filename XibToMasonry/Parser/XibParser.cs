using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            //
            //从关联的.m文件中提取操作代码

            JObject mainView = (JObject)_rootJson["document"]["objects"]["view"];
            //JObject mainView = (JObject)_rootJson["document"]["objects"]["view"];
            XmlElement mainViewXml = _rootXml["document"]["objects"]["view"];

            //寻找主View
            //green="0.4823529411764706" blue="0.62352941176470589" 
            CalculationHelper.ColorToHex("0.4823529411764706", "0.4823529411764706", "0.62352941176470589");

            RunOneView(mainViewXml, true);



        }


        /// <summary>
        /// 仅支持自定义View 还不支持tableviewcell等xib
        /// </summary>
        /// <param name="xml"></param>
        /// <param name="isMain"></param>
        /// <param name="parentViewPropertyName"></param>
        private void RunOneView(XmlElement xml, bool isMain, string parentViewPropertyName = "")
        {
            //取当前的角色

            string name = xml.Name;
            string id = xml.GetAttribute("id");
            string propertyName = IdToName(id); //属性名
            string customClass = xml.GetAttribute("customClass");
            string className = CalculationHelper.GetOCClassName(name, customClass); // UIView UIButton
            xml.SetAttribute("xibToMasonryNamePropertyName", propertyName);

            if (isMain)
            {
                //获取当前View的名字
                //主View是self.view
                //存储propertyName的列表
                propertyName = "view";
                _controlNameXml = xml["connections"];
            } 
            else
            {
                //TODO 写出
                Console.WriteLine(@$"@property(nonatomic, strong) {className} *{propertyName}");

                //写出数据

                string parentViewName = parentViewPropertyName == "" ? "self" : parentViewPropertyName;

                Console.WriteLine(@$"[self.{parentViewName} addSubview:self.{propertyName}];");

            }

            if (className == "UIButton")
            {
                GetUIButton(xml, propertyName);
            }

            //分析约束，如果是带有secondItem，则视为全局依赖项目，后续读取
            //如果约束没有带有secondItem，则为自身属性的显示，如高度等
            //创建懒加载
            //TODO 优先处理不带依赖的布局
            //处理子view


            XmlElement subView = xml["subviews"];
            if (subView != null)
            {
                foreach (XmlElement item in subView.ChildNodes)
                {
                    RunOneView(item, false, propertyName);
                }
            }
            //Console.WriteLine(subView);
        }

        private string GetUIButton(XmlElement xml, string propertyName)
        {
            string lazyCode = string.Empty;

            lazyCode += @$"- (UIButton *){propertyName} {{ {"\r\n"}";
            lazyCode += @$"    if (_{propertyName}) {{  {"\r\n"}";
            lazyCode += @$"        UIButton *button = [UIButton new]; {"\r\n"}";
            //lazyCode += @$"        button.***;";

            //TODO属性、文字、图片、颜色
            foreach (XmlElement item in xml.ChildNodes)
            {
                if (item.Name == "state")
                {
                    string stateName = $"UIControlState{CalculationHelper.UpperFirstChar(item.GetAttribute("key"))}";
                    if (string.IsNullOrWhiteSpace(item.GetAttribute("title")) == false) //设置标题
                    {
                        lazyCode += $"        [button setTitle:@\"{item.GetAttribute("title")}\" forState:{stateName}]; \r\n";
                    }

                    if (string.IsNullOrWhiteSpace(item.GetAttribute("image")) == false) //设置头像
                    {
                        lazyCode += $"        [button setImage:[UIImage imageNamed:@\"{item.GetAttribute("image")}\"] forState:{stateName}]; \r\n";
                    }

                    //检查子节点设置标题颜色
                    foreach (XmlElement subItem in item.ChildNodes)
                    {
                        if (subItem.Name == "color")
                        {
                            //检查子节点设置标题颜色
                            lazyCode += NodeColor(subItem, stateName);
                        }
                    }
                } 
                else if (item.Name == "color")
                {
                    lazyCode += NodeColor(item);
                }
            }

            //方法
            if (xml["connections"] != null)
            {
                foreach (XmlElement item in xml["connections"].ChildNodes)
                {
                    if (item.Name == "action")
                    {
                        Console.WriteLine("实现方法{0}", item.GetAttribute("eventType"));

                        string eventName = $"UIControlEvent{CalculationHelper.UpperFirstChar(item.GetAttribute("eventType"))}";
                        string selector = item.GetAttribute("selector");

                        lazyCode += $"        [button addTarget:self action:@selector({selector}) forControlEvents:{eventName}]; \r\n";
                    }
                }
            }

            lazyCode += $"        _{propertyName} = button; \r\n";
            lazyCode += "        } \r\n";
            lazyCode += "    } \r\n";
            lazyCode += $"    return _{propertyName}; \r\n";
            lazyCode += "} \r\n";

            Console.WriteLine(lazyCode);
            return lazyCode;
        }

        
        private string NodeColor(XmlElement xml, string state = "")
        {

            if (string.IsNullOrWhiteSpace(xml.GetAttribute("key")) == false)
            {
                string hex = "FFFFFF";
                string alpha = "1";
                string lazyCode = "";
                if (string.IsNullOrWhiteSpace(xml.GetAttribute("white")) == false)
                {
                    if (double.Parse(xml.GetAttribute("white")) == 0)
                    {
                        alpha = "0.0";
                    }
                    else
                    {
                        alpha = "1";
                    }
                }
                else
                {
                    var red = xml.GetAttribute("red");
                    var green = xml.GetAttribute("green");
                    var blue = xml.GetAttribute("blue");

                    hex = CalculationHelper.ColorToHex(red, green, blue);
                    alpha = xml.GetAttribute("alpha");
                }

                if (string.IsNullOrWhiteSpace(state))
                {
                    lazyCode += $"        [button set{CalculationHelper.UpperFirstChar(xml.GetAttribute("key"))}:[UIColor hx_colorWithHexString:@\"#{hex}\" alpha:{alpha}]]; \r\n";
                } 
                else
                {
                    lazyCode += $"        [button set{CalculationHelper.UpperFirstChar(xml.GetAttribute("key"))}:[UIColor hx_colorWithHexString:@\"#{hex}\" alpha:{alpha}] " +
                        $"forState:{state}]; \r\n";
                }
                return lazyCode;
            }
            return "";
        }



        //将view的id属性转换成实际名字
        private string IdToName(string id)
        {
            string name = @$"fakeIName{_fakeNameIndex}"; //FakeIName
            if (_controlNameXml != null)
            {
                foreach (XmlElement item in _controlNameXml.ChildNodes)
                {
                    if (item.Name == "outlet" && item.GetAttribute("destination") == id)
                    {
                        name = item.GetAttribute("property");
                        break;
                    }
                }
            }
            _fakeNameIndex++;
            return name;
        }





    }
}
