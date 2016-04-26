using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace MoleSplit.SplitCore
{
    /// <summary>
    /// 元素识别器
    /// </summary>
    class Element : RecognizerBase
    {
        private string[] _elementPattern;
        // ---------------------------------------------------------------------------------
        public override void Load(string text)
        {
            this._elementPattern = text.Split(new char[] { '\r', '\n', ';' }, StringSplitOptions.RemoveEmptyEntries);
        }

        public override void Parse()
        {
            base.DefinedFragment = new Dictionary<string, int>();

            var r = new Regex(@"^\D\D?");
            for (int i = 0; i < base.Molecule.AtomList.Length; i++)
            {
                var temp = r.Match(base.Molecule.AtomList[i]).Value;
                if (this._elementPattern.Contains(temp))
                {
                    if (!base.DefinedFragment.ContainsKey(temp)) { base.DefinedFragment.Add(temp, 0); }
                    base.DefinedFragment[temp]++;
                }
            }
            if (this._elementPattern.Contains("H")) // 统计H元素个数
            {
                int n = this.Element_H();
                if (n > 0)
                {
                    base.DefinedFragment.Add("H", n);
                }
            }
        }
        private int Element_H()
        {
            var dict = new Dictionary<string, int>() { { "C", 4 }, { "Si", 4 }, { "O", 2 }, { "N", 3 }, { "S", 2 }, };

            int H_Num = 0;
            var r = new Regex(@"^(\D\D?)(\d+?)_");
            for (int i = 0; i < base.Molecule.AtomList.Length; i++)
            {
                Match m = r.Match(base.Molecule.AtomList[i]);
                if (!dict.ContainsKey(m.Groups[1].Value)) { continue; }
                int temp = dict[m.Groups[1].Value] + base.Molecule.Charge[i];
                string bond = m.Groups[2].Value;
                for (int j = 0; j < bond.Length; j++)
                {
                    temp -= (bond[j] - 48);
                }
                H_Num += temp;
            }
            return H_Num;
        }
    }
}
