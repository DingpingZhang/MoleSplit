using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace MoleSplit
{
    /// <summary>
    /// 环识别器
    /// </summary>
    class Ring : ARecognizer, IAddAttribute
    {
        private Dictionary<string, string> _attributeTag;
        private bool _isGetEachRing; // 标记是否需要在搜索时获得每一个平面最小环
        // ---------------------------------------------------------------------------------
        private List<int> _ringAtom; // 记录所有环上的原子(无序)
        private List<int[]> _order;// 顺序表
        private List<int[]> _reOrder;// 逆序表
        private int[,] _lockSide;// 记录走过的边
        // ---------------------------------------------------------------------------------

        public override void Load(string text)
        {
            this._attributeTag = new Dictionary<string, string>();
            var item = text.Split(new string[] { "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            var r = new Regex(@"(.+?)=\((.+?)\)");
            for (int i = 0; i < item.Length; i++)
            {
                var m = r.Match(item[i]);
                var temp_1 = m.Groups[1].Value;
                var temp_2 = m.Groups[2].Value;
                if (temp_2 == "MEMRING")
                {
                    this._isGetEachRing = true;
                }
                else
                {
                    this._attributeTag.Add(temp_2, temp_1);
                }
            }
        }

        public override void Parse()
        {
            if (!this._isGetEachRing) { return; }
            base.DefinedFragment = new Dictionary<string, int>();
            this.SerachCore();
            for (int i = 0; i < this._order.Count; i++)
            {
                var temp = this._order[i][0].ToString() + "_Membered_Ring";
                if (!base.DefinedFragment.ContainsKey(temp)) { base.DefinedFragment.Add(temp, 0); }
                base.DefinedFragment[temp]++;
            }
        }

        public void AddAttribute()
        {
            if (this._attributeTag == null) { return; }

            foreach (var item in this._attributeTag)
            {
                switch (item.Key)
                {
                    case "RING": this.SignRing(this.GetRing(), item.Value);
                        break;
                    case "C_RING": this.SignRing(this.GetRing("^C"), item.Value);
                        break;
                    case "SAT_C_RING": this.SignRing(this.GetRing("^C111?"), item.Value);
                        break;
                    case "RING_BOND": this.SignRing(this.GetRing("."));
                        break;
                    case "C_RING_BOND": this.SignRing(this.GetRing("^C"));
                        break;
                    case "SAT_C_RING_BOND": this.SignRing(this.GetRing("^C111?"));
                        break;
                    default:
                        break;
                }
            }
        }

        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// 为给定的一组环标记（基于点）
        /// </summary>
        /// <param name="ringList">环索引组</param>
        /// <param name="tag">属性标记</param>
        private void SignRing(int[][] ringList, string tag)
        {
            for (int i = 0; i < ringList.Length; i++)
            {
                for (int j = 0; j < ringList[i].Length; j++)
                {
                    base.Molecule.AtomList[ringList[i][j]] += tag;
                }
            }
        }

        /// <summary>
        /// 为给定的一组环标记（基于边）
        /// </summary>
        /// <param name="ringList"></param>
        private void SignRing(int[][] ringList)
        {
            int p_1, p_2, sign;
            for (int i = 0; i < ringList.Length; i++)
            {
                for (int j = 0; j < ringList[i].Length; j++)
                {
                    p_1 = ringList[i][j];
                    p_2 = (j < ringList[i].Length - 1) ? ringList[i][j + 1] : ringList[i][0];
                    sign = (this.Molecule.BondState[p_1, p_2] == 0 && this.Molecule.BondState[p_2, p_1] == 0) ? -1 : -2;

                    this.Molecule.BondState[p_1, p_2] = sign;
                    this.Molecule.BondState[p_2, p_1] = sign;
                }
            }
        }

        /// <summary>
        /// 给出分子中所有在环上的原子索引（无法区别每个环）
        /// </summary>
        /// <returns>原子索引数组列表</returns>
        private int[][] GetRing()
        {
            if (this._ringAtom == null)
            {
                this.SerachCore();
            }
            return new int[1][] { this._ringAtom.ToArray() };
        }

        /// <summary>
        /// 按照传入的正则表达式，给出符合要求的环列表（可区分每个环）
        /// </summary>
        /// <param name="regex">环上原子需满足的正则表达式</param>
        /// <returns>原子索引数组列表</returns>
        private int[][] GetRing(string regex)
        {
            int[][] result;
            switch (base.Molecule.NRing)
            {
                case 0: result = new int[0][]; break;
                case 1: result = this.GetRing(); break;
                default:
                    {
                        this._isGetEachRing = true;
                        this.SerachCore();
                        List<int[]> tempResult = new List<int[]>();
                        Regex r = new Regex(regex);
                        int[] tempRing;
                        for (int i = 0; i < this._order.Count; i++)
                        {
                            tempRing = new int[this._order[i][0]];
                            int current_p = 1; while (this._order[i][current_p] == 0) { current_p++; }
                            bool isAdd = true;
                            int head_p = current_p, p = 0;
                            do
                            {
                                if (!r.IsMatch(base.Molecule.AtomList[current_p - 1]))
                                {
                                    isAdd = false;
                                    break;
                                }
                                tempRing[p] = current_p - 1;
                                current_p = this._order[i][current_p];
                                p++;
                            } while (current_p != head_p);
                            if (isAdd) { tempResult.Add(tempRing); }
                        }
                        this._isGetEachRing = false;
                        result = tempResult.ToArray();
                    } break;
            }
            return result;
        }

        // Core ---------------------------------------------------------------------------------

        private void SerachCore()
        {
            this._order = new List<int[]>();
            this._reOrder = new List<int[]>();
            this._lockSide = new int[base.Molecule.AtomList.Length, base.Molecule.AtomList.Length];
            this._ringAtom = new List<int>();
            if (base.Molecule.NRing == 0) { return; }
            this.SearchRing(new List<int>(), 0);
        }

        private void SearchRing(List<int> path, int current_p)
        {
            for (int i = 0; i < path.Count; i++)
            {
                if (path[i] == current_p)
                {
                    for (int p = i; p < path.Count; p++)
                    {
                        if (!this._ringAtom.Contains(path[p])) { this._ringAtom.Add(path[p]); }
                    }
                    if (this._isGetEachRing)
                    {
                        RecordRing(path, i);
                        RepelRing();
                    }
                    return;
                }
            }
            for (int next_p = 0; next_p < base.Molecule.AtomList.Length; next_p++)
            {
                if (next_p != current_p // 不撞当前点
                 && base.Molecule.AdjMat[current_p, next_p] != 0 // 有路可走
                 && this._lockSide[current_p, next_p] != 1) // 没走过的
                {
                    path.Add(current_p); // 记录
                    this._lockSide[next_p, current_p] = 1; // 不逆行
                    this.SearchRing(path, next_p); // 递归
                    path.RemoveAt(path.Count - 1); // 退回时删除路径
                }
            }
        }

        private void RecordRing(List<int> path, int p)
        {
            int[] temp_o = new int[base.Molecule.AtomList.Length + 1];
            int[] temp_reO = new int[base.Molecule.AtomList.Length + 1];

            temp_o[0] = path.Count - p;

            int head = path[p] + 1;
            int tail = path[path.Count - 1] + 1;
            int q = path.Count - 1;
            for (; p < path.Count - 1; p++, q--)
            {
                temp_o[path[p] + 1] = path[p + 1] + 1;
                temp_reO[path[q] + 1] = path[q - 1] + 1;
            }
            temp_o[path[p] + 1] = head;
            temp_reO[path[q] + 1] = tail;

            this._order.Add(temp_o);
            this._reOrder.Add(temp_reO);
        }

        private void RepelRing()
        {
            for (int i = 0; i < this._order.Count - 1; i++)
            {
                // 环长度相等则不需要合并
                if (this._order[this._order.Count - 1][0] == this._order[i][0]) { continue; }
                // p1指向小环，p2指向大环
                int p1 = this._order[this._order.Count - 1][0] > this._order[i][0] ? i : this._order.Count - 1;
                int p2 = this._order[this._order.Count - 1][0] < this._order[i][0] ? i : this._order.Count - 1;
                // 备份
                int[] tempRing = new int[this._order[p2].Length];
                // 删长边
                int num = 0;
                for (int j = 1; j < this._order[p2].Length; j++)
                {
                    if (this._order[p2][j] != 0 && this._order[p2][j] != this._order[p1][j])
                    {
                        tempRing[j] = this._order[p2][j];
                        num++;
                    }
                }
                if ((this._order[p2][0] - num) <= this._order[p1][0] / 2) { continue; } // 重合度不够，则不合并环
                // 更新新环的长度
                tempRing[0] = 2 * num + this._order[p1][0] - this._order[p2][0];
                // 补短边
                int head_p = 1; while (tempRing[head_p] == 0) { head_p++; }
                int current_p = head_p;
                for (int j = 0; tempRing[current_p] != head_p; j++)
                {
                    if (tempRing[current_p] == 0)
                    {
                        tempRing[current_p] = this._reOrder[p1][current_p];
                    }
                    current_p = tempRing[current_p];
                }
                this._order[p2] = tempRing;
            }
        }
    }
}
