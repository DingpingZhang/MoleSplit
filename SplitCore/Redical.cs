using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace MoleSplit
{
    /// <summary>
    /// 子图识别器
    /// </summary>
    class Redical : ARecognizer, IAddAttribute
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
            public string[] Tag { get; private set; }
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
                this.Tag = new string[0];
                if (tags.Length > 1)
                {
                    this.Tag = new string[tags.Length - 1];
                    for (int i = 0; i < this.Tag.Length; i++)
                    {
                        this.Tag[i] = tags[i + 1];
                    }
                }

                for (int i = 1; i < info.Length; i++)
                {
                    string[] element = info[i].Split(new char[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    string regexStr = '^' + element[0] + '_';
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
        private List<Subgraph> _redicalToMatch; // 用于匹配的子图
        private List<Subgraph> _redicalToRename; // 用于重命名的子图
        // ---------------------------------------------------------------------------------
        private Subgraph _redical; // 指向当前正在解析的子图
        private bool _isBreak; // 中断递归
        private int[] _matched; // 记录已匹配的原子索引
        private int _nAtom; // 原子个数
        private int _nSign;
        // ---------------------------------------------------------------------------------
        public override void Load(string text)
        {
            this._redicalToMatch = new List<Subgraph>();
            this._redicalToRename = new List<Subgraph>();

            var item = text.Split(new string[] { "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            var r = new Regex(@"=\((.+?)\)", RegexOptions.Compiled);
            for (int i = 0; i < item.Length; i++)
            {
                string[] temp = r.Match(item[i]).Groups[1].Value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries); // 提取附加坐标
                int[] tempInt = new int[temp.Length];
                for (int j = 0; j < temp.Length; j++)
                {
                    tempInt[j] = int.Parse(temp[j]);
                }
                string tempInfo = r.Replace(item[i], "");
                var tempRedcical = new Subgraph(tempInfo);
                tempRedcical.SpecialAtom = tempInt;

                switch (item[i][0])
                {
                    case '_': this._redicalToRename.Add(tempRedcical);
                        break;
                    case '*': this._redicalToMatch.Add(tempRedcical);
                        break;
                    default:
                        if (tempInt.Length > 0)
                        {
                            this._redicalToRename.Add(tempRedcical);
                        }
                        else
                        {
                            this._redicalToMatch.Add(tempRedcical);
                        }
                        break;
                }
            }
        }
        public override void Parse()
        {
            base.DefinedFragment = new Dictionary<string, int>();
            this._nAtom = base.Molecule.AtomList.Length; // 取出原子个数，性能分析指出：此项使用调用极为频繁，若调用属性（函数）将大大拖慢程序
            this._nSign = 1;
            for (int i = 0; i < this._redicalToMatch.Count; i++)
            {
                this._redical = this._redicalToMatch[i];
                var result = this.Match(); // 匹配出结果
                for (int j = 0; j < result.Count; j++)
                {
                    var tempName = this._redical.Name + this.RecAttribute(this._redical.Tag, result[j]); // 进行属性判断
                    if (!base.DefinedFragment.ContainsKey(tempName)) { base.DefinedFragment.Add(tempName, 0); }
                    base.DefinedFragment[tempName]++; // 装入结果
                }
            }
        }
        public void AddAttribute()
        {
            this._nAtom = base.Molecule.AtomList.Length;
            for (int i = 0; i < this._redicalToRename.Count; i++)
            {
                this._redical = this._redicalToRename[i];
                this.Rename(this._redicalToRename[i].SpecialAtom);
                base.Molecule.Sign = new int[this._nAtom];
            }
        }
        // ---------------------------------------------------------------------------------
        private string RecAttribute(string[] attributeTag, int index)
        {
            string attribute = "";
            for (int i = 0; i < attributeTag.Length; i++)
            {
                if (attributeTag[i][0] != '-')
                {
                    attribute = base.Molecule.AtomList[index].Contains(attributeTag[i]) ? '_' + attributeTag[i] : "";
                }
                else
                {
                    string tempTag = attributeTag[i].Remove(0, 1);
                    if (base.Molecule.AtomList[index].Contains(tempTag)) { continue; } // 1.自己不能含有该属性
                    for (int j = 0; j < base.Molecule.AtomList.Length; j++)
                    {
                        if (this.Molecule.AdjMat[index, j] != 0
                         && base.Molecule.AtomList[j].Contains(tempTag)) // 2.自己所连的原子中，具有该属性
                        {
                            attribute = '_' + attributeTag[i];
                            break;
                        }
                    }
                }
                if (attribute != "") { return attribute; }
            }
            return "";
        }
        private void Rename(int[] renameIndex)
        {
            this.MatchCore(() =>
            {
                for (int i = 0; i < this._matched.Length; i++) // 全部还原
                {
                    base.Molecule.Sign[this._matched[i]] = -1; // 后续原子能用，首原子不能用
                }
                // 重命名
                if (this._redical.Name[0] == '_') // 属性添加
                {
                    for (int i = 0; i < renameIndex.Length; i++)
                    {
                        base.Molecule.AtomList[this._matched[renameIndex[i]]] += this._redical.Name;
                    }
                }
                else // 全名重载
                {
                    for (int i = 0; i < renameIndex.Length; i++)
                    {
                        base.Molecule.AtomList[this._matched[renameIndex[i]]] = this._redical.Name;
                    }
                }
            });
        }
        private List<int> Match()
        {
            var core_List = new List<int>();

            this.MatchCore(() =>
            {
                for (int i = 0; i < this._redical.SpecialAtom.Length; i++) // 还原SpecialAtom
                {
                    base.Molecule.Sign[this._redical.SpecialAtom[i]] = 0;
                }
                core_List.Add(this._matched[0]);
                this._nSign++;
            });
            return core_List;
        }
        // Core -------------------------------------------------------------------------------------
        private void MatchCore(Action operation)
        {
            if (this._nAtom < this._redical.AtomCodeList.Length) { return; }
            this._matched = new int[this._redical.AtomCodeList.Length];
            for (int i = 0; i < this._nAtom; i++)
            {
                if (base.Molecule.Sign[i] == 0
                 && this._redical.AtomCodeList[0].IsMatch(base.Molecule.AtomList[i]))
                {
                    this._matched[0] = i;
                    this._isBreak = false;
                    int backupState = base.Molecule.Sign[this._matched[0]]; // 备份原子访问状态
                    base.Molecule.Sign[this._matched[0]] = this._nSign;
                    this.Match_R(1);
                    if (this._isBreak)
                    {
                        operation();
                    }
                    else
                    {
                        base.Molecule.Sign[i] = backupState; // 当匹配失败时，从备份中还原原子状态
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
                if (this._redical.AdjMat[n - 1][i] == 0) { continue; }
                for (int p_M_Next = 0; p_M_Next < this._nAtom; p_M_Next++)
                {
                    if (base.Molecule.AdjMat[this._matched[i], p_M_Next] != 0
                     && base.Molecule.Sign[p_M_Next] <= 0
                     && this._redical.AtomCodeList[n].IsMatch(base.Molecule.AtomList[p_M_Next]))
                    {
                        this._matched[n] = p_M_Next;
                        if (this.Compare(n, this._matched))
                        {
                            int backupState = base.Molecule.Sign[this._matched[n]];
                            base.Molecule.Sign[this._matched[n]] = this._nSign;
                            this.Match_R(n + 1);
                            if (this._isBreak) { return; }
                            base.Molecule.Sign[this._matched[n]] = backupState;
                        }
                    }
                }
            }
        }
        private bool Compare(int n, int[] matched)
        {
            for (int j = 0; j < n; j++)
            {
                if (base.Molecule.AdjMat[matched[n], matched[j]] != this._redical.AdjMat[n - 1][j]
               && !(this._redical.AdjMat[n - 1][j] > 4 && base.Molecule.AdjMat[matched[n], matched[j]] != 0))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
