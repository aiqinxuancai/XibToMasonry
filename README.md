# XibToMasonry

使用c#编写，使用.net5编译的跨平台命令行工具，本工具辅助转换较为标准的xib、storyboard文件，主要用于重构代码时，处理令人头疼的布局，将其转换为Masonry代码。

**本工具并不能完全自动将所有的代码进行处理**，你可能需要自己在绝对坐标和约束之间删减代码，但这总比完全重写那些几十个view堆叠在里面的xib要快一些。

你也可以修改源代码将其输出为更适合自己项目的代码。

## 快速开始
下载release包解压，使用命令行运行

```cmd
./XibToMasonry [XibFilePath]
or
./XibToMasonry [Directory]
```

会生成对应的.m文件

## 如何编译

可能用到这个代码的多是iOS开发，所以简单说下如何自己修改后编译。
首先你应该安装macos的[.Net5 SDK](https://dotnet.microsoft.com/download/dotnet/5.0)。
然后在目录中执行命令:
```cmd
dotnet publish -c Release -r osx-x64 /p:PublishSingleFile=true
```

## 其他说明

部分颜色转换代码 hx_colorWithHexString 来自 HexColors 库，请自行添加pod，或自行替换为你的工程中所使用的颜色转换代码.

```
pod 'HexColors', '3.0.0'
```

其他会使用到的代码

```objc
+ (CGFloat)screenWidth375Scale {
    CGFloat width = MIN(CGRectGetWidth([UIScreen mainScreen].bounds), CGRectGetHeight([UIScreen mainScreen].bounds));
    return width / 375.0;
}
```

```objc
#import <Foundation/Foundation.h>
#import <Masonry.h>

#define scale375_offset(value) valueOffset(MASBoxValue([UIUtils screenWidth375Scale] * value))
#define scale375_equalTo(size) equalTo(MASBoxValue(CGSizeMake([UIUtils screenWidth375Scale] * size.width, [UIUtils screenWidth375Scale] * size.height)))
#define scale375_value(value) ([UIUtils screenWidth375Scale] * value)


#define MAS_SCALE_375_OFFSET(VALUE) valueOffset(MASBoxValue([UIUtils screenWidth375Scale] * (VALUE)))

@interface MASConstraint (WFScale)

- (MASConstraint * (^)(id attr))scale375_offset;
- (MASConstraint * (^)(id attr))scale375_equalTo;
- (MASConstraint * (^)(id attr))MAS_SCALE_375_OFFSET;

@end
```