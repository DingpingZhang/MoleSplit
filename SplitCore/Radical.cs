using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace MoleSplit.SplitCore
{
    /// <summary>
    /// 子图识别器
    /// </summary>
    internal class Radical : RecognizerBase
    {
        private class Subgraph
        {
            /// <summary>
            /// 基团名称
            /// </summary>
            public string Name { get; private set; }

            /// <summary>
            /// 待判断的基团属性
            /// </summary>
            //public string[] Tag { get; private set; }

            /// <summary>
            /// 基团邻接矩阵的条件：
            /// (1)第一个元素必须是与母体相连的原子（一般为C）；
            /// (2)原子顺序必须逐层排列
            /// </summary>
            public int[][] AdjMat { get; private set; }

            /// <summary>
            /// 原子列表（对应邻接矩阵）
            /// </summary>
            public Regex[] AtomCodeList { get; private set; }

            /// <summary>
            /// 重命名原子 或 辅助定位原子的坐标
            /// </summary>
            public int[] SpecialAtom { get; set; }

            public Subgraph(string text)
            {
                var info = text.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                var tags = info[0].Split(new char[] { '?', ':' }, StringSplitOptions.RemoveEmptyEntries);

                this.AdjMat = new int[info.Length - 2][];
                this.AtomCodeList = new Regex[info.Length - 1];
                this.Name = tags[0];
                //this.Tag = new string[0];
                //if (tags.Length > 1)
                //{
                //    this.Tag = new string[tags.Length - 1];
                //    for (int i = 0; i < this.Tag.Length; i++)
                //    {
                //        this.Tag[i] = tags[i + 1];
                //    }
                //}
                for (int i = 1; i < info.Length; i++)
                {
                    string[] element = info[i].Split(new char[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    string regexStr = '^' + element[0];

                    regexStr = regexStr.Replace("_", "_(_.+?_)*_");
                    regexStr = regexStr.Replace("$", @"_$");
                    if (regexStr[regexStr.Length - 1] != '$') { regexStr += '_'; }

                    this.AtomCodeList[i - 1] = new Regex(regexStr, RegexOptions.Compiled);
                    int[] temp = new int[i - 1];
                    for (int k = 0; k < temp.Length; k++)
                    {
                        temp[k] = int.Parse(element[k + 1]);
                    }
                    if (temp.Length != 0) { this.AdjMat[i - 2] = temp; }
                }
            }
        }
        // ---------------------------------------------------------------------------------
        private List<Subgraph> _radicalToMatch; // 用于匹配的子图
        private List<Subgraph> _radicalToRename; // 用于重命名的子图
        // ---------------------------------------------------------------------------------
        private Subgraph _radical; // 指向当前正在解析的子图
        private bool _isBreak; // 中断递归
        private int[] _matched; // 记录已匹配的原子索引
        private int _nAtom; // 原子个数
        private int _sign; // 给原子打上的状态码
        private bool[] _lock; // 在原子的匹配过程中锁住正在使用的原子
        private bool _isLockingBond; // 是否启用基于键的屏蔽
        // ---------------------------------------------------------------------------------

        public override void Load(string text)
        {
            this._radicalToMatch = new List<Subgraph>();
            this._radicalToRename = new List<Subgraph>();

            var item = text.Split(new string[] { "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            var r = new Regex(@"=\((.+?)\)", RegexOptions.Compiled);

            int i = 0;
            if (item[0] == "LOCKING_BOND")
            {
                this._isLockingBond = true;
                i = 1;
            }
            for (; i < item.Length; i++)
            {
                string[] temp = r.Match(item[i]).Groups[1].Value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries); // 提取附加坐标
                int[] tempInt = new int[temp.Length];
                for (int j = 0; j < temp.Length; j++)
                {
                    tempInt[j] = int.Parse(temp[j]);
                }
                string tempInfo = r.Replace(item[i], "");
                var tempRadical = new Subgraph(tempInfo);
                tempRadical.SpecialAtom = tempInt;

                switch (item[i][0])
                {
                    case '_': this._radicalToRename.Add(tempRadical);
                        break;
                    case '*': this._radicalToMatch.Add(tempRadical);
                        break;
                    default:
                        if (tempInt.Length > 0)
                        {
                            this._radicalToRename.Add(tempRadical);
                        }
                        else
                        {
                            this._radicalToMatch.Add(tempRadical);
                        }
                        break;
                }
            }
        }

        public override void Parse()
        {
            this._nAtom = base.Molecule.AtomList.Length; // 取出原子个数，性能分析指出：此项使用调用极为频繁，若调用属性（函数）将大大拖慢程序
            this._lock = new bool[this._nAtom];
            base.DefinedFragment = new Dictionary<string, int>();
            this.AddAttribute(); // 标记属性
            this._sign = 1;
            for (int i = 0; i < this._radicalToMatch.Count; i++)
            {
                this._radical = this._radicalToMatch[i];
                List<string> lastResults = new List<string>();
                var count = 0;
                this.MatchCore(() =>
                {
                    int[] tempIntArray = null;
                    if (this._radical.SpecialAtom.Length == this._radical.AtomCodeList.Length) // 全虚拟：虚拟原子不可相同
                    {
                        tempIntArray = this._matched;
                    }
                    else if (this._radical.SpecialAtom.Length != 0) // 部分虚拟：非虚拟原子不可相同，虚拟原子可以相同
                    {
                        tempIntArray = new int[this._radical.AtomCodeList.Length - this._radical.SpecialAtom.Length];
                        for (int j = 0, p = 0; j < this._matched.Length; j++)
                        {
                            if (!this._radical.SpecialAtom.Contains(j))
                            {
                                tempIntArray[p++] = this._matched[j];
                            }
                        }
                    }
                    string str_ResultSort = "";
                    if (tempIntArray != null)
                    {
                        Array.Sort(tempIntArray);
                        for (int j = 0; j < tempIntArray.Length; j++)
                        {
                            str_ResultSort += (tempIntArray[j] + "/");
                        }
                    }
                    if (!lastResults.Contains(str_ResultSort))
                    {
                        count++;
                        if (str_ResultSort != "")
                        {
                            lastResults.Add(str_ResultSort);
                        }
                    }
                    if (this._isLockingBond) { this.LockingBond(); } // do some other things.
                });
                for (int j = 0; j < count; j++)
                {
                    var tempName = (this._radical.Name).Replace("*", "");
                    if (!base.DefinedFragment.ContainsKey(tempName)) { base.DefinedFragment.Add(tempName, 0); }
                    base.DefinedFragment[tempName]++; // 装入结果
                }
            }
        }

        private void AddAttribute()
        {
            this._sign = -1;
            for (int i = 0; i < this._radicalToRename.Count; i++)
            {
                this._radical = this._radicalToRename[i];
                this.MatchCore(() =>
                {
                    if (this._radical.Name[0] == '_') // 属性添加
                    {
                        for (int j = 0; j < this._radicalToRename[i].SpecialAtom.Length; j++)
                        {
                            base.Molecule.AtomList[this._matched[this._radicalToRename[i].SpecialAtom[j]]] += this._radical.Name;
                        }
                    }
                    else // 全名重载
                    {
                        for (int j = 0; j < this._radicalToRename[i].SpecialAtom.Length; j++)
                        {
                            base.Molecule.AtomList[this._matched[this._radicalToRename[i].SpecialAtom[j]]] = this._radical.Name;
                        }
                    }
                });
                base.Molecule.AtomState = new int[this._nAtom];
            }
        }

        // Core -------------------------------------------------------------------------------------

        private void MatchCore(Action operation)
        {
            if (this._nAtom < this._radical.AtomCodeList.Length) { return; }
            this._matched = new int[this._radical.AtomCodeList.Length];
            for (int i = 0; i < this._nAtom; i++)
            {
                if ((base.Molecule.AtomState[i] == 0
                 || (this._radical.Name[0] == '*' && this._radical.SpecialAtom.Contains(0)))
                 && this._radical.AtomCodeList[0].IsMatch(base.Molecule.AtomList[i]))
                {
                    this._matched[0] = i;
                    this._isBreak = false;
                    int backupState = base.Molecule.AtomState[this._matched[0]]; // 备份原子访问状态
                    base.Molecule.AtomState[i] = this._sign;
                    this._lock[i] = true; // 锁定首原子
                    //this._lock = new bool[this._nAtom]; // 没有正在使用中的原子
                    this.Match_R(1);
                    this._lock[i] = false; // 解除锁定
                    // 退出时，将特殊原子全部恢复
                    if (this._radical.Name[0] == '*' && this._radical.SpecialAtom.Contains(0))
                    {
                        base.Molecule.AtomState[i] = backupState;
                    }
                    if (this._isBreak)
                    {
                        operation();
                    }
                    else
                    {
                        base.Molecule.AtomState[i] = backupState; // 当匹配失败时，从备份中还原原子状态
                    }
                }
            }
        }

        private void Match_R(int n)
        {
            if (n == this._matched.Length)
            {
                this._isBreak = true;
                return;
            }
            for (int i = 0; i < n; i++)
            {
                if (this._radical.AdjMat[n - 1][i] == 0) { continue; }
                for (int p_M_Next = 0; p_M_Next < this._nAtom; p_M_Next++)
                {
                    if (base.Molecule.AdjMat[this._matched[i], p_M_Next] != 0
                     && ((base.Molecule.AtomState[p_M_Next] <= 0 || this._radical.SpecialAtom.Contains(n)) && !this._lock[p_M_Next]) // 下个原子的状态码表示可用且该原子并没有正在被使用
                     && this._radical.AtomCodeList[n].IsMatch(base.Molecule.AtomList[p_M_Next]))
                    {
                        this._matched[n] = p_M_Next;
                        if (this.Compare(n, this._matched))
                        {
                            int backupState = base.Molecule.AtomState[this._matched[n]];
                            // 进：修改原子状态 && 锁定原子
                            base.Molecule.AtomState[p_M_Next] = this._sign;
                            this._lock[p_M_Next] = true;

                            this.Match_R(n + 1);
                            if (this._isBreak)
                            {
                                this._lock[p_M_Next] = false;
                                // 退出时，将特殊原子全部恢复
                                if (this._radical.Name[0] == '*' && this._radical.SpecialAtom.Contains(n))
                                {
                                    base.Molecule.AtomState[p_M_Next] = backupState;
                                }
                                return;
                            }
                            // 退：恢复原子状态 && 接触锁定
                            base.Molecule.AtomState[p_M_Next] = backupState;
                            this._lock[p_M_Next] = false;
                        }
                    }
                }
            }
        }

        private bool Compare(int n, int[] matched)
        {
            for (int j = 0; j < n; j++)
            {
                if (base.Molecule.AdjMat[matched[n], matched[j]] != this._radical.AdjMat[n - 1][j]
               && !(this._radical.AdjMat[n - 1][j] > 4 && base.Molecule.AdjMat[matched[n], matched[j]] != 0))
                {
                    return false;
                }
            }
            return true;
        }

        private void LockingBond()
        {
            int sign;
            for (int i = 1; i < this._radical.AtomCodeList.Length; i++)
            {
                for (int j = 0; j < this._radical.AdjMat[i - 1].Length; j++)
                {
                    if (this._radical.AdjMat[i - 1][j] != 0)
                    {
                        sign = (this.Molecule.BondState[this._matched[i], this._matched[j]] == 0
                            && this.Molecule.BondState[this._matched[i], this._matched[j]] == 0) ? 1 : -2;
                        base.Molecule.BondState[this._matched[i], this._matched[j]] = sign;
                        base.Molecule.BondState[this._matched[j], this._matched[i]] = sign;
                    }
                }
            }
        }
    }
}
