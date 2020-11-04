using ObjcetC.Core;
using ObjcetC.Core.Properties;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;

namespace ObjcetC.Core
{
    public class CodeLazyLoad
    {

        /**
         /** 最大人数*/
        //@property(nonatomic, strong) UILabel* memberMaxLabel;
        /** 房间ID*/
        //@property(nonatomic, strong) UILabel* roomIdLabel;
        /** 游戏时长图标*/
        //@property(nonatomic, strong) UIImageView* roomGamingTimeImageView;
        /** 游戏时长*/
        //@property(nonatomic, strong) UILabel* roomGamingTimeLabel;

        public string GetLazyLoad(string code)
        {
            Debug.WriteLine(code);
            var codeLine = code.Split("\r\n".ToCharArray());

            string content = "";
            foreach (var item in codeLine)
            {
                string func = LineStringToLazyLoadCode(item);

                content += string.IsNullOrWhiteSpace(func) ?  "" : func + "\r\n\r\n";

            }
            return content;
        }

        public string LineStringToLazyLoadCode(string line)
        {
            if (line.Contains("IBOutlet"))
            {
                return "";
            }
            if (line.Trim().StartsWith("@property"))
            {
                Regex regex = new Regex(@"\)(.*)\s(\*+)(.*);");
                Match match = regex.Match(line);

                if (match.Groups.Count == 4)
                {
                    string propertyType = match.Groups[1].Value.Trim();
                    string propertyStar = match.Groups[2].Value.Trim();
                    string propertyName = match.Groups[3].Value.Trim();

                    Debug.WriteLine(propertyName);
                    string codeContent = ObjcetC.Core.Properties.Resources.NormalCode;
                    switch (propertyType)
                    {
                        case "UILabel":
                            codeContent = ObjcetC.Core.Properties.Resources.LabelCode;
                            break;
                        case "UIImageView":
                            codeContent = ObjcetC.Core.Properties.Resources.ImageViewCode;
                            break;
                        case "UIButton":
                            codeContent = ObjcetC.Core.Properties.Resources.ButtonCode;
                            break;
                    }

                    codeContent = codeContent.Replace("<propertyName>", propertyName);
                    codeContent = codeContent.Replace("<propertyStar>", propertyStar);
                    codeContent = codeContent.Replace("<propertyType>", propertyType);
                    codeContent = codeContent.Replace("<PropertyName>", propertyName.Substring(0, 1).ToUpper() + propertyName.Substring(1));

                    return codeContent;
                }
            }
            return "";
        }
    }
}
