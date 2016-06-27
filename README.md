# MoleSplit
本项目是用于在以基团贡献法估算物性时，完成分子拆分任务的程序集，基于C#语言开发。

***

## 1.介绍

![](https://raw.githubusercontent.com/DingpingZhang/MoleSplit/master/images/GroupContributionMethodProcess.png)

使用基团贡献法进行物性估算，通常可以分为三个步骤：

1. 选择：确定需要估算的化合物及物性；（一些物性的估算可能还涉及其它参数的确定，如温度、压力等）
2. 拆分：根据需要估算的物性，选择适当的基团贡献法，并按该方法的定义基团对分子进行拆分，得到估算所需的基团数目列表；
3. 计算：将上述步骤中的数据带入相应的计算公式，完成物性估算。

本程序集用于完成上述步骤2的任务：以指定基团贡献法拆分指定的化合物。

## 2.程序集的调用
### 2.1 一般使用


    using MoleSplit;

    namespace Tutorial
    {
        class Program
        {
            static void Main(string[] args)
            {
                var moleSplitter = new MoleSplitter(); // 创建一个空的拆分器实例，此时该实例不具备拆分能力
                moleSplitter.LoadDefineFile(@"MSDFile.mdef"); // 加载分子拆分定义文件，此步骤将使该实例获得指定基团贡献法（由加载文件决定）的拆分能力
                moleSplitter.LoadMolFile(@"molFile.mol"); // 加载指定分子的mol文件
                moleSplitter.Parse(); // 进行结构解析
                Dictionary<string, int> result = moleSplitter.DefinedFragment; // 获取拆分结果
                Dictionary<string, int> otherFragment = moleSplitter.UndefineFragment; // 获取拆分结束过，分子中剩余的片段，此集合元素个数若不为0，将意味着拆分失败。（该方法不适用与改分子）
            }
        }
    }


### 2.2 针对含有特殊基团的基团贡献法
特殊基团是指所有在本程序集（SplitCore中提供的方法）识别范围之外的基团。在MoleSplitter类中提供了名为SplitEnd的事件，该事件接受MoleSplit.SplitEndEventHandler类型的委托。

![](https://raw.githubusercontent.com/DingpingZhang/MoleSplit/master/images/MoleSplitter.png)


    MoleSplit.SplitEndEventHandler additionalSplitOperation = new SplitEndEventHandler((splitResultArgs) =>
    {
        // Do something.
    });


SplitEndEventHandler委托中接受的参数为MoleSplit.SplitEndEventArgs类型，其中含有三个属性。

![](https://raw.githubusercontent.com/DingpingZhang/MoleSplit/master/images/SplitEndEventArgs.png)

其中MoleInfo类型的属性中包含了正在进行拆分的分子的相关信息。

![](https://raw.githubusercontent.com/DingpingZhang/MoleSplit/master/images/MoleInfo.png)
