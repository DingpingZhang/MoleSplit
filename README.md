# MoleSplit

---

## 0. Description
This project is used to disassembly of molecular structure for Group Contribution Method, which base on C# language development.

## 1. Introduction

![](/images/GroupContributionMethodProcess.png)

When we use the group contribution method to estimate the physical properties, there are usually the following three steps:

1. **Select**: Determine the compounds and physical properties that need to be estimated. (and other parameters, e.g.: T, p etc.)
2. **Split**: According to estimates of the required properties, select the appropriate group contribution method, and the molecular are split, base on the definition of the method, to obtain the list that contain corresponding number of group.
3. **Calculate**: The data in the above steps into the corresponding formula, complete property estimation.

This assembly is used to complete the step 2: split the specified compound with the specified group contribution method.

## 2. Invoke the assembly
### 2.1 For general group

```
using MoleSplit;

namespace Tutorial
{
     class Program
     {
         static void Main(string[] args)
        {
            var moleSplitter = new MoleSplitter();
            moleSplitter.LoadDefineFile(@"MSDFile.mdef");
            moleSplitter.LoadMolFile(@"molFile.mol");
            Dictionary<string, int> result = moleSplitter.DefinedFragment;
            Dictionary<string, int> otherFragment = moleSplitter.UndefineFragment;
        }
    }
}
```

### 2.2 For Special group
Special groups are groups that are outside the scope of the identification of all the methods provided in this assembly (namespace MoleSplit.Core). So, it provides an event named ***SplitEnd*** in the ***MoleSplitter*** class, which accepts the delegate of the ***MoleSplit.SplitEndEventHandler*** type.

![](/images/MoleSplitter.png)

```
MoleSplit.SplitEndEventHandler additionalSplitOperation = new SplitEndEventHandler((splitResultArgs) =>
{
    // The custom code.
});
```

***SplitEndEventHandler*** accepts the parameters of the ***MoleSplit.SplitEndEventArgs*** type, which contains the following three properties:

![](/images/SplitEndEventArgs.png)

The ***MoleInfo*** type contains information about the molecules being split.

![](/images/MoleInfo.png)
