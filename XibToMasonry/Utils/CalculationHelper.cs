using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XibToMasonry.Utils
{
    public class CalculationHelper
    {

        public static string UpperFirstChar(string text)
        {
            return text.First().ToString().ToUpper() + text.Substring(1, text.Length - 1);
        }

        public static string ColorToHex(string redString, string greenString, string blueString)
        {
            int red = (int)(double.Parse(redString) * 255);
            int green = (int)(double.Parse(greenString) * 255);
            int blue = (int)(double.Parse(blueString) * 255);

            string hex = Convert.ToString(red, 16).PadLeft(2, '0') + Convert.ToString(green, 16).PadLeft(2, '0') + Convert.ToString(blue, 16).PadLeft(2, '0');

            Console.WriteLine(hex);
            return hex.ToUpper() ;
        }

        //获取class名字
        public static string GetOCClassName(string nodeName, string customClassName)
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
                string name = "UI" + UpperFirstChar(nodeName);
                return name;
            }
        }
    }
}
