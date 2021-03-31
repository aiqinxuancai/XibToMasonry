using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using XibToMasonry.Utils;

namespace XibToMasonry.Parser
{
    public class ViewParser
    {

        //声明
        private string _propertyCode = string.Empty;

        //AddSubview
        private string _propertyAddViewCode = string.Empty;

        //懒加载
        private string _propertyLazyCode = string.Empty;

        //布局
        private string _propertyMasCode = string.Empty;

        private string _mainClassName;

        private int _fakeNameIndex; //用于实际没有命名的view的顺序ID

        //ID=>屬性名對照对应列表
        //key = id, value = name;
        private Dictionary<string, string> _propertyNameIdHashtable = new Dictionary<string, string>();

        //依赖列表
        private List<XmlElement> _constraintList = new List<XmlElement>();


        private const string kOffsetFuncName = "scale375_offset"; //scale375_offset //offset


        private XmlElement _objectsXml;

        private string _sourceFilePath = string.Empty;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="xml">objects</param>
        public ViewParser(XmlElement xml, string sourceFilePath)
        {
            _fakeNameIndex = 0;
            _objectsXml = xml;
            _sourceFilePath = sourceFilePath;
        }



        public void StartParser()
        {
            foreach (XmlElement item in _objectsXml.ChildNodes)
            {
                //TODO 如果支持storyboard需要修改
                if (item.Name != "placeholder")
                {
                    RunOneView(item, true);
                }
                else
                {
                    ChangeIdNameToHashtable(item["connections"]); //存储属性名列表
                }
            }
        }
        /// <summary>
        /// 仅支持自定义View 还不支持tableviewcell等xib
        /// </summary>
        /// <param name="xml"></param>
        /// <param name="isMain"></param>
        /// <param name="parentViewPropertyName"></param>
        /// <param name="dontAddView">不要添加这个view进代码，一般表示继承过来的</param>
        /// <param name="truePropertyName">因为contentView不能从connections中取到变量名，所以直传contentView</param>
        private void RunOneView(XmlElement xml, bool isMain, string parentViewPropertyName = "", bool dontAddView = false,
            string truePropertyName = "")
        {
            //取当前的角色
            string name = xml.Name;
            string id = xml.GetAttribute("id");

            //写出时再替换
            string propertyName = id; //truePropertyName != "" ? truePropertyName : IdToName(id); //属性名

            string customClass = xml.GetAttribute("customClass"); //todo storyboardIdentifier 

            string className = CalculationHelper.GetOCClassName(name, customClass); // 类型名 UIView UIButton
            xml.SetAttribute("xibToMasonryNamePropertyName", propertyName);

            _propertyNameIdHashtable[id] = truePropertyName != "" ? truePropertyName : IdToName(id);

            //优先判断并提取约束
            if (isMain)
            {
                //_controlNameXml =xml [“ connections”];  //存储propertyName的列表
                _mainClassName = className;
                _propertyNameIdHashtable[id] = "self";
                ChangeIdNameToHashtable(xml["connections"]); //存储属性名列表

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

                //contentView需要主动存储约束
                XmlElement constraints = contentView["constraints"];
                if (constraints != null)
                {
                    foreach (XmlElement item in constraints.ChildNodes)
                    {
                        _constraintList.Add(item);
                    }
                }

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


            //最后统一替换成属性名
            foreach (var item in _propertyNameIdHashtable)
            {
                fullCode = fullCode.Replace(item.Key, item.Value);
            }

            var savePath = Path.Combine(Directory.GetParent(_sourceFilePath).FullName, $"{_mainClassName}_mas.m");

            Console.WriteLine($"Save file:{savePath}");

            File.WriteAllText(savePath, fullCode);

            //TODO 头文件
            //File.WriteAllText(Path.Combine(Directory.GetParent(_xibFilePath).FullName, $"{fileName}.h"), fullCode);

#if DEBUG
            Console.WriteLine(_propertyCode);
            Console.WriteLine(_propertyAddViewCode);
            Console.WriteLine(_propertyMasCode);
            Console.WriteLine(_propertyLazyCode);
#endif
        }




        private string GetMasCode(XmlElement xml, string propertyName, string className)
        {
            string masCode = string.Empty;
            masCode += $"    [self.{propertyName} mas_makeConstraints:^(MASConstraintMaker * make) {{ \r\n";
            foreach (XmlElement item in xml.ChildNodes)
            {
                if (item.Name == "rect")
                {

                    var rectKey = item.GetAttribute("key");


                    if (rectKey == "frame")
                    {
                        masCode += $"        make.width.{kOffsetFuncName}({item.GetAttribute("width")});\r\n";
                        masCode += $"        make.height.{kOffsetFuncName}({item.GetAttribute("height")});\r\n";
                        masCode += $"        make.left.{kOffsetFuncName}({item.GetAttribute("x")});\r\n";
                        masCode += $"        make.top.{kOffsetFuncName}({item.GetAttribute("y")});\r\n";
                    }
                    else if (rectKey == "contentStretch")
                    {
                        //TODO 可能存在多种key （contentStretch、frame）
                    }

                }
                else if (item.Name == "constraints")
                {
                    // TODO约束，取当前的约束
                    // TODO如果不包含第一项和第二项，则专有存储供给获取Mas Code处理
                    // TODO如果包含，则加入同一个结构体中，在最后的Get Mas Code处理

                    masCode += $"        // 以下是由约束生成，需自行处理和上面rect的冲突\r\n";

                    foreach (XmlElement subItem in item)
                    {
                        if (subItem.Name == "constraint")
                        {
                            //约束
                            masCode += NodeConstraint(subItem, propertyName, false);
                        }
                    }

                    //TODO 有bug
                    //检查总约束列表
                    foreach (XmlElement subItem in _constraintList.ToArray())
                    {
                        masCode += NodeConstraint(subItem, propertyName, true);
                    }


                    //检测全局约束是否有firstItem等于自己的

                }
            }
            masCode += $"    }}];\r\n";
            //Console.WriteLine(masCode);
            return masCode;
        }





        /// <summary>
        /// 生成懒加载代码
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
                            lazyCode += NodeColor(subItem, funcValueName);
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
            }

            //key的实现
            var contentMode = xml.GetAttribute("contentMode");
            var image = xml.GetAttribute("image");
            var contentHorizontalAlignment = xml.GetAttribute("contentHorizontalAlignment"); //TODO
            var contentVerticalAlignment = xml.GetAttribute("contentVerticalAlignment"); //TODO
            var lineBreakMode = xml.GetAttribute("lineBreakMode"); //TODO

            if (!string.IsNullOrWhiteSpace(image))
            {
                lazyCode += $"        [{funcValueName} setImage:[UIImage imageNamed:@\"{image}\"]]; \r\n";
            }

            if (!string.IsNullOrWhiteSpace(contentMode) && contentMode != "scaleToFill") //scaleToFill默认值
            {
                lazyCode += $"        {funcValueName}.contentMode = UIViewContentMode{CalculationHelper.UpperFirstChar(contentMode)}; \r\n";
            }


            //action的实现
            if (xml["connections"] != null)
            {
                foreach (XmlElement item in xml["connections"].ChildNodes)
                {
                    if (item.Name == "action")
                    {
                        string eventName = $"UIControlEvent{CalculationHelper.UpperFirstChar(item.GetAttribute("eventType"))}";
                        string selector = item.GetAttribute("selector");
                        lazyCode += $"        [{funcValueName} addTarget:self action:@selector({selector}) forControlEvents:{eventName}]; \r\n";
                    }
                }
            }

            lazyCode += $"        _{propertyName} = {funcValueName}; \r\n";
            lazyCode += "        } \r\n";
            lazyCode += $"    return _{propertyName}; \r\n";
            lazyCode += "} \r\n";
            return lazyCode;
        }


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

                return lazyCode;
            }
            return "";
        }


        private string NodeConstraint(XmlElement xml, string funcValueName, bool dontAddToList)
        {
            var lazyCode = "";
            //TODO 有bug

            var firstItem = xml.GetAttribute("firstItem");
            var firstAttribute = xml.GetAttribute("firstAttribute");
            var secondItem = xml.GetAttribute("secondItem");
            var secondAttribute = xml.GetAttribute("secondAttribute");
            var constant = xml.GetAttribute("constant");
            var relation = xml.GetAttribute("relation");
            var multiplier = xml.GetAttribute("multiplier"); //比例

            relation = string.IsNullOrWhiteSpace(relation) ? "equalTo" : relation;
            constant = string.IsNullOrWhiteSpace(constant) ? "0" : constant;

            //是自己的 或者 firstItem 是自己
            if (string.IsNullOrWhiteSpace(firstItem))
            {

                if (string.IsNullOrWhiteSpace(secondItem))
                {
                    if (string.IsNullOrWhiteSpace(multiplier))
                    {
                        lazyCode = $"        make.{firstAttribute}.mas_{relation}({constant});\r\n";
                    }
                    else
                    {
                        //TODO 比例
                    }
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(multiplier))
                    {
                        lazyCode = $"        make.{firstAttribute}.mas_{relation}(self.{secondItem}.mas_{secondAttribute}).{kOffsetFuncName}({constant});\r\n";
                    }
                    else
                    {
                        //TODO 比例
                    }
                }

                return lazyCode;
            }
            else if (xml.GetAttribute("firstItem") == funcValueName)
            {
                lazyCode = $"        make.{firstAttribute}.mas_{relation}(self.{secondItem}.mas_{secondAttribute}).{kOffsetFuncName}({constant});\r\n";
                return lazyCode;
            }
            else
            {
                //不是我的约束 插入到总的约束表
                if (dontAddToList == false)
                {
                    Console.WriteLine($"加入约束{xml.OuterXml}");
                    _constraintList.Add(xml);
                }

            }
            return "";
        }



        /// <summary>
        /// 颜色实现
        /// </summary>
        /// <param name="xml"></param>
        /// <param name="funcValueName"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        private string NodeColor(XmlElement xml, string funcValueName, string state = "")
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
                if (item.GetAttribute("key") == "contentView" || item.GetAttribute("key") == "view")
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
            if (_propertyNameIdHashtable.TryGetValue(id, out string name))
            {
                return name;
            }

            //如果这个id没有对应的名字
            string fakeName = @$"fakeIName{_fakeNameIndex}";
            _propertyNameIdHashtable[id] = fakeName;
            _fakeNameIndex++;
            return fakeName;
        }


        private void ChangeIdNameToHashtable(XmlElement controlNameXml)
        {
            if (controlNameXml != null)
            {
                foreach (XmlElement item in controlNameXml.ChildNodes)
                {
                    if (item.Name == "outlet")
                    {
                        string name = item.GetAttribute("property");
                        string id = item.GetAttribute("destination");
                        _propertyNameIdHashtable[id] = name;
                    }
                }
            }
        }
    }
}
