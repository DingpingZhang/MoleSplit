using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace MoleSplit.Core
{
    /// <summary>
    /// 环识别器
    /// </summary>
    internal class Ring : RecognizerBase
    {
        private List<string> _operType;
        private List<string> _operObject;
        private bool _isGetEachRing; // 标记是否需要在搜索时获得每一个平面最小环
        // ---------------------------------------------------------------------------------
        private List<int> _ringAtom; // 记录所有环上的原子(无序)
        private List<int[]> _order;// 顺序表
        private List<int[]> _reverseOrder;// 逆序表
        private int[,] _lockSide;// 记录走过的边
        // ---------------------------------------------------------------------------------

        public override void Load(string text)
        {
            _operType = new List<string>();
            _operObject = new List<string>();

            var itemArray = text.Split(new[] { "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            var r = new Regex(@"(.+?)=\((.+?)\)");
            foreach (string item in itemArray)
            {
                var m = r.Match(item);
                _operType.Add(m.Groups[1].Value);
                _operObject.Add(m.Groups[2].Value);
            }
        }

        public override void Parse()
        {
            //if (!this._isGetEachRing) { return; }
            for (int i = 0; i < _operType.Count; i++)
            {
                if (_operType[i] == "PRINT")
                {
                    int[][] tempRingArray = null;
                    switch (_operObject[i])
                    {
                        case "RING":
                            tempRingArray = GetRing(".");
                            break;
                        case "C_RING":
                            tempRingArray = GetRing("^C");
                            break;
                        case "SAT_C_RING":
                            tempRingArray = GetRing("^C1");
                            break;
                    }
                    DefinedFragment = new Dictionary<string, int>();

                    if (tempRingArray == null) continue;
                    foreach (var tempRing in tempRingArray)
                    {
                        var name = tempRing.Length.ToString() + '_' + _operObject[i];
                        if (!DefinedFragment.ContainsKey(name)) { DefinedFragment.Add(name, 0); }
                        DefinedFragment[name]++;
                    }
                }
                else
                {
                    AddAttribute(_operType[i], _operObject[i]);
                }
            }
        }

        private void AddAttribute(string operType, string operObject)
        {
            switch (operObject)
            {
                case "RING":
                    SignRing(GetRing("."), operType);
                    break;
                case "C_RING":
                    SignRing(GetRing("^C"), operType);
                    break;
                case "SAT_C_RING":
                    SignRing(GetRing("^C1"), operType);
                    break;
                case "RING_BOND":
                    SignRing(GetRing("."));
                    break;
                case "C_RING_BOND":
                    SignRing(GetRing("^C"));
                    break;
                case "SAT_C_RING_BOND":
                    SignRing(GetRing("^C111?"));
                    break;
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
            foreach (var ring in ringList)
            {
                foreach (var ringAtom in ring)
                {
                    Molecule.AtomList[ringAtom] += tag;
                }
            }
        }

        /// <summary>
        /// 为给定的一组环标记（基于边）
        /// </summary>
        /// <param name="ringList"></param>
        private void SignRing(int[][] ringList)
        {
            foreach (int[] ring in ringList)
            {
                for (int j = 0; j < ring.Length; j++)
                {
                    var p1 = ring[j];
                    var p2 = (j < ring.Length - 1) ? ring[j + 1] : ring[0];
                    var sign = (Molecule.BondState[p1, p2] == 1 || Molecule.BondState[p2, p1] == 1) ? -2 : -1;

                    Molecule.BondState[p1, p2] = sign;
                    Molecule.BondState[p2, p1] = sign;
                }
            }
        }

        /// <summary>
        /// 给出分子中所有在环上的原子索引（无法区别每个环）
        /// </summary>
        /// <returns>原子索引数组列表</returns>
        private int[][] GetRing()
        {
            _isGetEachRing = false;
            SerachCore();
            return new[] { _ringAtom.ToArray() };
        }

        private int[][] GetRings()
        {
            _isGetEachRing = true;
            SerachCore();
            List<int[]> result = new List<int[]>();
            foreach (int[] ring in _order)
            {
                int[] tempRing = new int[ring[0]];
                int currentP = 1; while (ring[currentP] == 0) { currentP++; }
                int headP = currentP, p = 0;
                do
                {
                    tempRing[p++] = currentP - 1;
                    currentP = ring[currentP];
                } while (currentP != headP);
                result.Add(tempRing);
            }
            return result.ToArray();
        }

        /// <summary>
        /// 按照传入的正则表达式，给出符合要求的环列表（可区分每个环）
        /// </summary>
        /// <param name="regex">环上原子需满足的正则表达式</param>
        /// <returns>原子索引数组列表</returns>
        private int[][] GetRing(string regex)
        {
            int[][] ringArray;
            switch (Molecule.NRing)
            {
                case 0: ringArray = new int[0][]; break;
                case 1: ringArray = GetRing(); break;
                default: ringArray = GetRings(); break;
            }
            var temp = new List<int[]>();
            var r = new Regex(regex);
            foreach (var ring in ringArray)
            {
                int j = 0;
                for (; j < ring.Length; j++)
                {
                    if (!r.IsMatch(Molecule.AtomList[ring[j]]))
                    {
                        break;
                    }
                }
                if (j == ring.Length)
                {
                    temp.Add(ring);
                }
            }
            return temp.ToArray();
        }

        // Core ---------------------------------------------------------------------------------

        private void SerachCore()
        {
            _order = new List<int[]>();
            _reverseOrder = new List<int[]>();
            _lockSide = new int[Molecule.AtomList.Length, Molecule.AtomList.Length];
            _ringAtom = new List<int>();
            if (Molecule.NRing == 0) { return; }
            SearchRing(new List<int>(), 0);
        }

        private void SearchRing(List<int> path, int currentP)
        {
            for (int i = 0; i < path.Count; i++)
            {
                if (path[i] == currentP)
                {
                    for (int p = i; p < path.Count; p++)
                    {
                        if (!_ringAtom.Contains(path[p])) { _ringAtom.Add(path[p]); }
                    }
                    if (_isGetEachRing)
                    {
                        RecordRing(path, i);
                        RepelRing();
                    }
                    return;
                }
            }
            for (int nextP = 0; nextP < Molecule.AtomList.Length; nextP++)
            {
                if (nextP != currentP // 不撞当前点
                 && Molecule.AdjMat[currentP, nextP] != 0 // 有路可走
                 && _lockSide[currentP, nextP] != 1) // 没走过的
                {
                    path.Add(currentP); // 记录
                    _lockSide[nextP, currentP] = 1; // 不逆行
                    SearchRing(path, nextP); // 递归
                    path.RemoveAt(path.Count - 1); // 退回时删除路径
                }
            }
        }

        private void RecordRing(List<int> path, int p)
        {
            int[] tempOrder = new int[Molecule.AtomList.Length + 1];
            int[] tempReverseOrder = new int[Molecule.AtomList.Length + 1];

            tempOrder[0] = path.Count - p;

            int head = path[p] + 1;
            int tail = path[path.Count - 1] + 1;
            int q = path.Count - 1;
            for (; p < path.Count - 1; p++, q--)
            {
                tempOrder[path[p] + 1] = path[p + 1] + 1;
                tempReverseOrder[path[q] + 1] = path[q - 1] + 1;
            }
            tempOrder[path[p] + 1] = head;
            tempReverseOrder[path[q] + 1] = tail;

            _order.Add(tempOrder);
            _reverseOrder.Add(tempReverseOrder);
        }

        private void RepelRing()
        {
            for (int i = 0; i < _order.Count - 1; i++)
            {
                // 环长度相等则不需要合并
                if (_order[_order.Count - 1][0] == _order[i][0]) { continue; }
                // p1指向小环，p2指向大环
                int p1 = _order[_order.Count - 1][0] > _order[i][0] ? i : _order.Count - 1;
                int p2 = _order[_order.Count - 1][0] < _order[i][0] ? i : _order.Count - 1;
                // 备份
                int[] tempRing = new int[_order[p2].Length];
                // 删长边
                int num = 0;
                for (int j = 1; j < _order[p2].Length; j++)
                {
                    if (_order[p2][j] != 0 && _order[p2][j] != _order[p1][j])
                    {
                        tempRing[j] = _order[p2][j];
                        num++;
                    }
                }
                if ((_order[p2][0] - num) <= _order[p1][0] / 2) { continue; } // 重合度不够，则不合并环
                // 更新新环的长度
                tempRing[0] = 2 * num + _order[p1][0] - _order[p2][0];
                // 补短边
                int headP = 1; while (tempRing[headP] == 0) { headP++; }
                int currentP = headP;
                //for (int j = 0; tempRing[currentP] != headP; j++) // 莫名其妙
                while (tempRing[currentP] != headP)
                {
                    if (tempRing[currentP] == 0)
                    {
                        tempRing[currentP] = _reverseOrder[p1][currentP];
                    }
                    currentP = tempRing[currentP];
                }
                _order[p2] = tempRing;
            }
        }
    }
}
