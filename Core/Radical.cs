using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace MoleSplit.Core
{
    /// <summary>
    /// 子图识别器
    /// </summary>
    internal class Radical : RecognizerBase
    {
        /// <summary>
        /// 储存子图的类
        /// </summary>
        private class Subgraph
        {
            /// <summary>
            /// 基团名称
            /// </summary>
            public string Name { get; }

            /// <summary>
            /// 基团邻接矩阵的条件：
            /// (1)第一个元素必须是与母体相连的原子（一般为C）；
            /// (2)原子顺序必须逐层排列
            /// </summary>
            public int[][] AdjMat { get; }

            /// <summary>
            /// 原子列表（对应邻接矩阵）
            /// </summary>
            public Regex[] AtomCodeList { get; }

            /// <summary>
            /// 重命名原子 或 辅助定位原子的坐标
            /// </summary>
            public int[] SpecialAtom { get; set; }

            public Subgraph(string text)
            {
                var info = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                var tags = info[0].Split(new[] { '?', ':' }, StringSplitOptions.RemoveEmptyEntries);

                AdjMat = new int[info.Length - 2][];
                AtomCodeList = new Regex[info.Length - 1];
                Name = tags[0];
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
                    string[] element = info[i].Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    string regexStr = '^' + element[0];

                    regexStr = regexStr.Replace("_", "_(_.+?_)*_");
                    regexStr = regexStr.Replace("$", @"_$");
                    if (regexStr[regexStr.Length - 1] != '$') { regexStr += '_'; }

                    AtomCodeList[i - 1] = new Regex(regexStr, RegexOptions.Compiled);
                    int[] temp = new int[i - 1];
                    for (int k = 0; k < temp.Length; k++)
                    {
                        temp[k] = int.Parse(element[k + 1]);
                    }
                    if (temp.Length != 0) { AdjMat[i - 2] = temp; }
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
            _radicalToMatch = new List<Subgraph>();
            _radicalToRename = new List<Subgraph>();

            var item = text.Split(new[] { "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            var r = new Regex(@"=\((.+?)\)", RegexOptions.Compiled);

            int i = 0;
            if (item[0] == "LOCKING_BOND")
            {
                _isLockingBond = true;
                i = 1;
            }
            for (; i < item.Length; i++)
            {
                string[] temp = r.Match(item[i]).Groups[1].Value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries); // 提取附加坐标
                int[] tempInt = new int[temp.Length];
                for (int j = 0; j < temp.Length; j++)
                {
                    tempInt[j] = int.Parse(temp[j]);
                }
                string tempInfo = r.Replace(item[i], "");
                var tempRadical = new Subgraph(tempInfo) {SpecialAtom = tempInt};

                switch (item[i][0])
                {
                    case '_': _radicalToRename.Add(tempRadical);
                        break;
                    case '*': _radicalToMatch.Add(tempRadical);
                        break;
                    default:
                        if (tempInt.Length > 0)
                        {
                            _radicalToRename.Add(tempRadical);
                        }
                        else
                        {
                            _radicalToMatch.Add(tempRadical);
                        }
                        break;
                }
            }
        }

        public override void Parse()
        {
            _nAtom = Molecule.AtomList.Length; // 取出原子个数，性能分析指出：此项使用调用极为频繁，若调用属性（函数）将大大拖慢程序
            _lock = new bool[_nAtom];
            DefinedFragment = new Dictionary<string, int>();
            AddAttribute(); // 标记属性
            _sign = 1;
            foreach (Subgraph redical in _radicalToMatch)
            {
                _radical = redical;
                List<string> lastResults = new List<string>();
                var count = 0;
                MatchCore(() =>
                {
                    int[] tempIntArray = null;
                    if (_radical.SpecialAtom.Length == _radical.AtomCodeList.Length) // 全虚拟：虚拟原子不可相同
                    {
                        tempIntArray = _matched;
                    }
                    else if (_radical.SpecialAtom.Length != 0) // 部分虚拟：非虚拟原子不可相同，虚拟原子可以相同
                    {
                        tempIntArray = new int[_radical.AtomCodeList.Length - _radical.SpecialAtom.Length];
                        for (int j = 0, p = 0; j < _matched.Length; j++)
                        {
                            if (!_radical.SpecialAtom.Contains(j))
                            {
                                tempIntArray[p++] = _matched[j];
                            }
                        }
                    }
                    string strResultSort = "";
                    if (tempIntArray != null)
                    {
                        Array.Sort(tempIntArray);
                        strResultSort = tempIntArray.Aggregate(strResultSort, (current, t) => current + (t + "/"));
                    }
                    if (!lastResults.Contains(strResultSort))
                    {
                        count++;
                        if (strResultSort != "")
                        {
                            lastResults.Add(strResultSort);
                        }
                    }
                    if (_isLockingBond) { LockingBond(); } // do some other things.
                });
                for (int j = 0; j < count; j++)
                {
                    var tempName = (_radical.Name).Replace("*", "");
                    if (!DefinedFragment.ContainsKey(tempName)) { DefinedFragment.Add(tempName, 0); }
                    DefinedFragment[tempName]++; // 装入结果
                }
            }
        }

        private void AddAttribute()
        {
            _sign = -1;
            foreach (var redical in _radicalToRename)
            {
                _radical = redical;
                MatchCore(() =>
                {
                    if (_radical.Name[0] == '_') // 属性添加
                    {
                        foreach (var specialAtom in redical.SpecialAtom)
                        {
                            Molecule.AtomList[_matched[specialAtom]] += _radical.Name;
                        }
                    }
                    else // 全名重载
                    {
                        foreach (var specialAtom in redical.SpecialAtom)
                        {
                            Molecule.AtomList[_matched[specialAtom]] = _radical.Name;
                        }
                    }
                });
                Molecule.AtomState = new int[_nAtom];
            }
        }

        // Core -------------------------------------------------------------------------------------

        private void MatchCore(Action operation)
        {
            if (_nAtom < _radical.AtomCodeList.Length) { return; }
            _matched = new int[_radical.AtomCodeList.Length];
            for (int i = 0; i < _nAtom; i++)
            {
                if ((Molecule.AtomState[i] == 0
                 || (_radical.Name[0] == '*' && _radical.SpecialAtom.Contains(0)))
                 && _radical.AtomCodeList[0].IsMatch(Molecule.AtomList[i]))
                {
                    _matched[0] = i;
                    _isBreak = false;
                    int backupState = Molecule.AtomState[_matched[0]]; // 备份原子访问状态
                    Molecule.AtomState[i] = _sign;
                    _lock[i] = true; // 锁定首原子
                    //this._lock = new bool[this._nAtom]; // 没有正在使用中的原子
                    Match_R(1);
                    _lock[i] = false; // 解除锁定
                    // 退出时，将特殊原子全部恢复
                    if (_radical.Name[0] == '*' && _radical.SpecialAtom.Contains(0))
                    {
                        Molecule.AtomState[i] = backupState;
                    }
                    if (_isBreak)
                    {
                        operation();
                    }
                    else
                    {
                        Molecule.AtomState[i] = backupState; // 当匹配失败时，从备份中还原原子状态
                    }
                }
            }
        }

        private void Match_R(int n)
        {
            if (n == _matched.Length)
            {
                _isBreak = true;
                return;
            }
            for (int i = 0; i < n; i++)
            {
                if (_radical.AdjMat[n - 1][i] == 0) { continue; }
                for (int pMNext = 0; pMNext < _nAtom; pMNext++)
                {
                    if (Molecule.AdjMat[_matched[i], pMNext] != 0
                     && ((Molecule.AtomState[pMNext] <= 0 || _radical.SpecialAtom.Contains(n)) && !_lock[pMNext]) // 下个原子的状态码表示可用且该原子并没有正在被使用
                     && _radical.AtomCodeList[n].IsMatch(Molecule.AtomList[pMNext]))
                    {
                        _matched[n] = pMNext;
                        if (Compare(n, _matched))
                        {
                            int backupState = Molecule.AtomState[_matched[n]];
                            // 进：修改原子状态 && 锁定原子
                            Molecule.AtomState[pMNext] = _sign;
                            _lock[pMNext] = true;

                            Match_R(n + 1);
                            if (_isBreak)
                            {
                                _lock[pMNext] = false;
                                // 退出时，将特殊原子全部恢复
                                if (_radical.Name[0] == '*' && _radical.SpecialAtom.Contains(n))
                                {
                                    Molecule.AtomState[pMNext] = backupState;
                                }
                                return;
                            }
                            // 退：恢复原子状态 && 接触锁定
                            Molecule.AtomState[pMNext] = backupState;
                            _lock[pMNext] = false;
                        }
                    }
                }
            }
        }

        private bool Compare(int n, int[] matched)
        {
            for (int j = 0; j < n; j++)
            {
                if (Molecule.AdjMat[matched[n], matched[j]] != _radical.AdjMat[n - 1][j]
               && !(_radical.AdjMat[n - 1][j] > 4 && Molecule.AdjMat[matched[n], matched[j]] != 0))
                {
                    return false;
                }
            }
            return true;
        }

        private void LockingBond()
        {
            for (int i = 1; i < _radical.AtomCodeList.Length; i++)
            {
                for (int j = 0; j < _radical.AdjMat[i - 1].Length; j++)
                {
                    if (_radical.AdjMat[i - 1][j] != 0)
                    {
                        var sign = (Molecule.BondState[_matched[i], _matched[j]] == 0
                                    && Molecule.BondState[_matched[i], _matched[j]] == 0) ? 1 : -2;
                        Molecule.BondState[_matched[i], _matched[j]] = sign;
                        Molecule.BondState[_matched[j], _matched[i]] = sign;
                    }
                }
            }
        }
    }
}
