using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MoleSplit
{
    /// <summary>
    /// 将mol文件中的信息转化为可用字段
    /// </summary>
    class MoleInfo
    {
        /// <summary>
        /// 分子邻接矩阵
        /// </summary>
        public int[,] AdjMat { get; private set; }
        /// <summary>
        /// 原子列表（对应邻接矩阵）
        /// </summary>
        public string[] AtomList { get; set; }
        /// <summary>
        /// 标记原子是否可用
        /// 0：未尝用也；
        /// 1：不可使用；
        /// -1：首原子不可使用，后续匹配中可以使用；
        /// </summary>
        public int[] Sign { get; set; }
        /// <summary>
        /// 电荷标记（在H统计中用到）
        /// </summary>
        public int[] Charge { get; private set; }
        /// <summary>
        /// 标记分子中的环数
        /// </summary>
        public int NRing { get; private set; }

        // --------------------------------------------------------------------------------------
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="text">分子信息文本</param>
        public MoleInfo(string text)
        {
            string[] molInfo;
            if (text.Contains(" H "))
                molInfo = this.GetTextWithoutH(text);
            else
                molInfo = text.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            string[] tempStrs = molInfo[3].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            int nAtom = int.Parse(tempStrs[0]);
            int nSide = int.Parse(tempStrs[1]);
            if (nAtom > 1000) { nSide = nAtom % 1000; nAtom /= 1000; }

            this.AdjMat = new int[nAtom, nAtom];
            this.AtomList = new string[nAtom];
            this.Sign = new int[nAtom];
            this.Charge = new int[nAtom];
            this.NRing = (nSide - nAtom + 1);

            int p1 = 0, p2 = 0, b = 0;
            string[] nBond = new string[nAtom];
            for (int i = 0; i < nSide; i++)
            {
                tempStrs = molInfo[i + 4 + nAtom].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                p1 = int.Parse(tempStrs[0]);
                p2 = int.Parse(tempStrs[1]);
                if (p1 < 1000) { b = int.Parse(tempStrs[2]); } else { b = p2; p2 = p1 % 1000; p1 /= 1000; }
                p1--; p2--;
                // 写入邻接矩阵
                this.AdjMat[p1, p2] = b; // 交换下标（因为无向图是对称阵）
                this.AdjMat[p2, p1] = b;
                //
                nBond[p1] += b;
                nBond[p2] += b;
            }
            for (int i = 0; i < nAtom; i++)
            {
                tempStrs = molInfo[i + 4].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                string tempAtomCode = tempStrs[3] + StringSort(nBond[i]) + '_';
                this.AtomList[i] = tempAtomCode;
                this.Charge[i] = tempStrs[5] == "0" ? 0 : 4 - int.Parse(tempStrs[5]);
            }
        }
        private string[] GetTextWithoutH(string text)
        {
            var molInfo = text.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            string[] tempStrs = molInfo[3].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            int nAtom = int.Parse(tempStrs[0]);
            int nSide = int.Parse(tempStrs[1]);
            if (nAtom > 1000) { nSide = nAtom % 1000; nAtom /= 1000; }

            var temp = new List<string>();
            var index = new List<int>();
            for (int i = 0; i < 4; i++) temp.Add(molInfo[i]);
            for (int i = 0; i < nAtom; i++)
            {
                if (molInfo[i + 4].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)[3] != "H")
                    temp.Add(molInfo[i + 4]);
                else
                    index.Add(i);
            }
            temp[3] = (nAtom - index.Count) + " " + (nSide - index.Count);
            int p1 = 0, p2 = 0;
            for (int i = 0; i < nSide; i++)
            {
                tempStrs = molInfo[i + 4 + nAtom].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                p1 = int.Parse(tempStrs[0]);
                p2 = int.Parse(tempStrs[1]);
                if (p1 >= 1000)  { p2 = p1 % 1000; p1 /= 1000; }
                p1--; p2--;
                if (!(index.Contains(p1) || index.Contains(p2)))
                    temp.Add(molInfo[i + 4 + nAtom]);
            }
            return temp.ToArray();
        }
        private string StringSort(string str)
        {
            if (str == null) { return ""; }
            char[] tempCharArray = str.ToCharArray();
            Array.Sort(tempCharArray);
            Array.Reverse(tempCharArray);
            return new string(tempCharArray);
        }
    }
}
