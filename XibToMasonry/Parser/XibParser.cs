﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace XibToMasonry.Utils
{
    public class XibParser
    {

        //private JObject _rootJson;
        private XmlDocument _rootXml;
        private XmlElement _controlNameXml;
        //private JArray _controlNameJson;
        private int _fakeNameIndex;

        //private ConnectionsParser _connectionsParser;

        private string _xibFilePath;
        private string _mainClassName;

        //声明
        private string _propertyCode = string.Empty;

        //AddSubview
        private string _propertyAddViewCode = string.Empty;

        //懒加载
        private string _propertyLazyCode = string.Empty;

        //布局
        private string _propertyMasCode = string.Empty;


        public XibParser(string filePath) {
            //读入
            _fakeNameIndex = 0;
            _xibFilePath = filePath;
            string xml = File.ReadAllText(filePath);
           
            //解析 connections
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            _rootXml = doc;

            Console.WriteLine($"Start converting files:{filePath}");


           //string jsonText = JsonConvert.SerializeXmlNode(doc);
           //Console.WriteLine(jsonText);
            //_rootJson = JObject.Parse(jsonText);

            try
            {
                StartParser();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

        }




        //生成声明
        private void StartParser()
        {
            //TODO 从关联的.m文件中提取操作代码?

            //JObject mainView = (JObject)_rootJson["document"]["objects"]["view"];
            XmlElement mainViewXml = _rootXml["document"]["objects"]["view"];

            foreach (XmlElement item in _rootXml["document"]["objects"].ChildNodes)
            {
                if (item.Name != "placeholder")
                {
                    mainViewXml = item;
                    break;
                }
            }

            //寻找主View
            //green="0.4823529411764706" blue="0.62352941176470589" 
            CalculationHelper.ColorToHex("0.4823529411764706", "0.4823529411764706", "0.62352941176470589");

            RunOneView(mainViewXml, true);


#if DEBUG
            Console.WriteLine(_propertyCode);
            Console.WriteLine(_propertyAddViewCode);
            Console.WriteLine(_propertyMasCode);
            Console.WriteLine(_propertyLazyCode);
#endif
        }


        public void SaveParser()
        {
            //头文件引入？
            string fullCode = string.Empty;
            fullCode += "\r\n";

            //存储@interface ()
            fullCode += $"@interface {_mainClassName}() \r\n";
            fullCode += "\r\n";
            fullCode += $"{_propertyCode} \r\n";
            fullCode += $"@end \r\n\r\n";


            //存储@implementation 
            fullCode += $"@implementation {_mainClassName} \r\n";
            fullCode += "\r\n";

            //存储init()函数
            fullCode += "- (instancetype)init { \r\n";
            fullCode += "    if (self = [super init]) { \r\n";
            fullCode += "        [self setupView]; \r\n";
            fullCode += "    } \r\n";
            fullCode += "    return self; \r\n";
            fullCode += "} \r\n";
            fullCode += "\r\n";

            //生成setupView()
            fullCode += "- (void)setupView { \r\n";
            fullCode += _propertyAddViewCode.Insert(0, "    ").Replace("\r\n", "\r\n    ");
            fullCode += "\r\n";
            fullCode += _propertyMasCode;
            fullCode += "\r\n";
            fullCode += "} \r\n";
            fullCode += "\r\n";

            //存储懒加载
            fullCode += _propertyLazyCode;
            fullCode += "\r\n";
            fullCode += "@end\r\n";
           

            var fileName = Path.GetFileNameWithoutExtension(_xibFilePath);
            var savePath = Path.Combine(Directory.GetParent(_xibFilePath).FullName, $"{fileName}.m");

            Console.WriteLine($"Save file:{savePath}");

            if (File.Exists(savePath))
            {
                savePath += ".rename";
            }

            File.WriteAllText(savePath, fullCode);

            //TODO 头文件
            //File.WriteAllText(Path.Combine(Directory.GetParent(_xibFilePath).FullName, $"{fileName}.h"), fullCode);

        }

        /// <summary>
        /// 仅支持自定义View 还不支持tableviewcell等xib
        /// </summary>
        /// <param name="xml"></param>
        /// <param name="isMain"></param>
        /// <param name="parentViewPropertyName"></param>
        /// <param name="dontAddView">不要添加这个view进代码，一般表示继承过来的</param>
        /// <param name="truePropertyName">因为contentView不能从connections中取到变量名，所以直传contentView</param>
        private void RunOneView(XmlElement xml, 
            bool isMain, 
            string parentViewPropertyName = "", 
            bool dontAddView = false, 
            string truePropertyName = "")
        {
            //取当前的角色
            string name = xml.Name;
            string id = xml.GetAttribute("id");
            string propertyName = truePropertyName != "" ? truePropertyName : IdToName(id); //属性名
            string customClass = xml.GetAttribute("customClass");
            string className = CalculationHelper.GetOCClassName(name, customClass); // 类型名 UIView UIButton
            xml.SetAttribute("xibToMasonryNamePropertyName", propertyName);

            if (isMain)
            {
                //获取当前View的名字
                //主View是self.view
                propertyName = "view";
                _controlNameXml = xml["connections"];  //存储propertyName的列表
                _mainClassName = className;

                //TODO 继承类名
                _propertyMasCode += "\r\n    //TODO 主view需要根据情况自己调整\r\n";
            } 
            else
            {
                if (dontAddView == false)
                {
                    _propertyCode += $"@property(nonatomic, strong) {className} *{propertyName}; \r\n";
                    string parentViewName = parentViewPropertyName == "" ? "self" : parentViewPropertyName;
                    _propertyAddViewCode += $"[self.{parentViewName} addSubview:self.{propertyName}]; \r\n";
                    _propertyLazyCode += GetLazyCode(xml, propertyName, className) + "\r\n";
                }
            }

            if (dontAddView == false)
            {
                //主view需自行调整
                _propertyMasCode += GetMasCode(xml, propertyName, className) + "\r\n";
            }

            XmlElement contentView = FindContentView(xml);

            if (contentView != null)
            {
                //如果是有contentView，则判断为tableviewcell等cell的xib
                propertyName = "view"; //propertyName不用也无所谓
                RunOneView(contentView, false, propertyName, true, "contentView");
                
            }
            else
            {
                XmlElement subView = xml["subviews"];
                if (subView != null)
                {
                    foreach (XmlElement item in subView.ChildNodes)
                    {
                        RunOneView(item, false, propertyName);
                    }
                }
            }
        }


        private string GetMasCode(XmlElement xml, string propertyName, string className)
        {
            string masCode = string.Empty;

            foreach (XmlElement item in xml.ChildNodes)
            {
                if (item.Name == "rect")
                {
                    //TODO 可能存在多种key （contentStretch、frame）
                    item.GetAttribute("key");

                    masCode += $"    [self.{propertyName} mas_makeConstraints:^(MASConstraintMaker * make) {{ \r\n";
                    masCode += $"        make.width.scale375_offset({item.GetAttribute("width")});\r\n";
                    masCode += $"        make.height.scale375_offset({item.GetAttribute("height")});\r\n";
                    masCode += $"        make.x.scale375_offset({item.GetAttribute("x")});\r\n";
                    masCode += $"        make.y.scale375_offset({item.GetAttribute("y")});\r\n";
                    masCode += $"    }}];\r\n";

                    //TODO 尺寸
                    //autoresizingMask
                    //自动依赖父级
                }
            }
            //Console.WriteLine(masCode);
            return masCode;
        }


        /// <summary>
        /// 目前支持button和基本的view
        /// </summary>
        /// <param name="xml"></param>
        /// <param name="propertyName"></param>
        /// <param name="className"></param>
        /// <returns></returns>
        private string GetLazyCode(XmlElement xml, string propertyName, string className)
        {
            string lazyCode = string.Empty;

            lazyCode += $"- ({className} *){propertyName} {{ \r\n";
            lazyCode += $"    if (!_{propertyName}) {{ \r\n";

            string funcValueName = className switch
            {
                "UIButton" => "button",
                _ => "view",
            };

            lazyCode += className switch
            {
                "UIButton" => $"        UIButton *{funcValueName} = [UIButton buttonWithType:UIButtonTypeCustom]; \r\n",
                _ => $"        {className} *view = [{className} new]; \r\n",
            };

            //属性、文字、图片、颜色
            foreach (XmlElement item in xml.ChildNodes)
            {
                //TODO支持label等常用控件属性设置
                if (item.Name == "state")
                {
                    string stateName = $"UIControlState{CalculationHelper.UpperFirstChar(item.GetAttribute("key"))}";
                    if (string.IsNullOrWhiteSpace(item.GetAttribute("title")) == false) //设置标题
                    {
                        lazyCode += $"        [{funcValueName} setTitle:@\"{item.GetAttribute("title")}\" forState:{stateName}]; \r\n";
                    }

                    if (string.IsNullOrWhiteSpace(item.GetAttribute("image")) == false) //设置图片
                    {
                        lazyCode += $"        [{funcValueName} setImage:[UIImage imageNamed:@\"{item.GetAttribute("image")}\"] forState:{stateName}]; \r\n";
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
                    lazyCode += NodeColor(item, funcValueName);
                }
                else if (item.Name == "fontDescription") //字体
                {
                    lazyCode += NodeFont(item, funcValueName);
                }
                else if (item.Name == "constraints")
                {
                    //TODO 约束，取当前的约束
                    //TODO 如果不包含 firstItem和secondItem ，视为私有存储供给GetMasCode处理
                    //TODO 如果包含，则加入同一个结构体中，在最后的GetMasCode处理
                }
                else if (item.Name == "rect")
                {
                    //GetMasCode处理
                }

            }

            //方法
            if (xml["connections"] != null)
            {
                foreach (XmlElement item in xml["connections"].ChildNodes)
                {
                    if (item.Name == "action")
                    {
                        //Console.WriteLine("实现方法{0}", item.GetAttribute("eventType"));

                        string eventName = $"UIControlEvent{CalculationHelper.UpperFirstChar(item.GetAttribute("eventType"))}";
                        string selector = item.GetAttribute("selector");

                        lazyCode += $"        [{funcValueName} addTarget:self action:@selector({selector}) forControlEvents:{eventName}]; \r\n";
                    }
                }
            }

            lazyCode += $"        _{propertyName} = {funcValueName}; \r\n";
            lazyCode += "        } \r\n";
            lazyCode += "    } \r\n";
            lazyCode += $"    return _{propertyName}; \r\n";
            lazyCode += "} \r\n";

            //Console.WriteLine(lazyCode);
            return lazyCode;
        }

        //[UIFont fontWithName:@"DFLiJinHeiW8" size:12];
        private string NodeFont(XmlElement xml, string funcValueName, string state = "")
        {
            var lazyCode = "";
            if (string.IsNullOrWhiteSpace(xml.GetAttribute("key")) == false)
            {

                var type = xml.GetAttribute("type");
                var fontName = xml.GetAttribute("name");
                var fontSize = xml.GetAttribute("pointSize");

                if (type == "system")
                {
                    //TODO  weight="semibold" 
                    lazyCode += $"        {funcValueName}.font = [UIFont systemFontOfSize:scale375_value({fontSize})]; \r\n";
                }
                else
                {
                    lazyCode += $"        {funcValueName}.font = [UIFont fontWithName:@\"{fontName}\" size:scale375_value({fontSize})]; \r\n";
                }

                lazyCode += $"        {funcValueName}.font = [UIFont fontWithName:@\"{fontName}\" size:scale375_value({fontSize})]; \r\n";

                return lazyCode;
            }
            return "";
        }


        
        
        private string NodeColor(XmlElement xml, string funcValueName,  string state = "")
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

                alpha = alpha.Length > 4 ? alpha.Substring(0, 4) : alpha;

                if (string.IsNullOrWhiteSpace(state))
                {
                    lazyCode += $"        [{funcValueName} set{CalculationHelper.UpperFirstChar(xml.GetAttribute("key"))}:[UIColor hx_colorWithHexString:@\"#{hex}\" alpha:{alpha}]]; \r\n";
                } 
                else
                {
                    lazyCode += $"        [{funcValueName} set{CalculationHelper.UpperFirstChar(xml.GetAttribute("key"))}:[UIColor hx_colorWithHexString:@\"#{hex}\" alpha:{alpha}] " +
                        $"forState:{state}]; \r\n";
                }
                return lazyCode;
            }
            return "";
        }



        private XmlElement FindContentView(XmlElement xml)
        {
            XmlElement contentView = null;
            //需要寻找contentView
            foreach (XmlElement item in xml.ChildNodes)
            {
                if (item.GetAttribute("key") == "contentView")
                {
                    contentView = item;
                    break;
                }
            }
            return contentView;
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
