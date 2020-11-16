using OpenCvSharp;
using System;
using System.Diagnostics;
using System.IO;

//试图将.9图识别黑线并输出rect坐标，然后输出删除黑线的png，未完成
namespace patch9image
{

    public class EdgeInsets
    {
        public int Top;
        public int Left;
        public int Bottom;
        public int Right;

        public EdgeInsets(int top, int left, int bottom, int right)
        {
            Top = top;
            Left = left;
            Bottom = bottom;
            Right = right;
        }

        override public string ToString()
        {
            return $"{Top},{Left},{Bottom},{Right}";
        }

    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            //枚举所在目录的image 
            //读取检测四边的黑色
            //将黑色尺寸输出
            string[] files = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.png");

            foreach(string file in files)
            {
                Console.WriteLine("处理文件");
                byte[] fileData = File.ReadAllBytes(file);

                Console.WriteLine(file);

                Mat mat = Mat.FromImageData(fileData, ImreadModes.Unchanged);
                EdgeInsets rect = MatToRect(mat);
            }
        }

        static EdgeInsets MatToRect(Mat mat)
        {
            var matIndexer = mat.GetGenericIndexer<Vec4b>();
            int left = 0, top = 0, right = 0, bottom = 0;

            //left
            for (int i = 0; i < mat.Cols; i++)
            {
                var index = matIndexer[0, i];
                Console.WriteLine(index);
                if (index == new Vec4b(0, 0, 0, 255))
                {
                    Console.WriteLine(i);
                    left = i ;
                    break;
                }
            }

            //right
            for (int i = mat.Cols - 1; i >= 0; i--)
            {
                var index = matIndexer[0, i];
                Console.WriteLine(index);
                if (index == new Vec4b(0, 0, 0, 255))
                {
                    Console.WriteLine(i);
                    right = i;
                    break;
                }
            }


            //top
            for (int i = 0; i < mat.Rows; i++)
            {
                var index = matIndexer[i, 0];
                Console.WriteLine(index);
                if (index == new Vec4b(0, 0, 0, 255))
                {
                    Console.WriteLine(i);
                    top = i;
                    break;
                }
            }

            //bottom
            for (int i = mat.Rows - 1; i >= 0; i--)
            {
                var index = matIndexer[i, 0];
                Console.WriteLine(index);
                if (index == new Vec4b(0, 0, 0, 255))
                {
                    Console.WriteLine(i);
                    bottom = i;
                    break;
                }
            }

            EdgeInsets edgeInsets = new EdgeInsets(top, left, mat.Cols - 1 - bottom, mat.Rows - 1 - right);

            //Rect rect = Rect.FromLTRB(left, top, right, bottom);
            //Console.WriteLine(rect);
            //Console.WriteLine(mat.Cols - 1 - rect.Right);
            //Console.WriteLine(mat.Rows - 1 - rect.Bottom);
            Console.WriteLine(edgeInsets);
            return edgeInsets;
        }


    }
}
