using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace MoleSplit.Core
{
    /// <summary>
    /// 元素识别器
    /// </summary>
    internal class Element : RecognizerBase
    {
        private string[] _elementPattern;
        // ---------------------------------------------------------------------------------
        public override void Load(string text)
        {
            _elementPattern = text.Split(new[] { '\r', '\n', ';' }, StringSplitOptions.RemoveEmptyEntries);
        }

        public override void Parse()
        {
            DefinedFragment = new Dictionary<string, int>();

            var r = new Regex(@"^\D\D?");
            foreach (var atom in Molecule.AtomList)
            {
                var temp = r.Match(atom).Value;
                if (_elementPattern.Contains(temp))
                {
                    if (!DefinedFragment.ContainsKey(temp)) { DefinedFragment.Add(temp, 0); }
                    DefinedFragment[temp]++;
                }
            }
            if (_elementPattern.Contains("H")) // 统计H元素个数
            {
                int n = Element_H();
                if (n > 0)
                {
                    DefinedFragment.Add("H", n);
                }
            }
        }
        private int Element_H()
        {
            var dict = new Dictionary<string, int> { { "C", 4 }, { "Si", 4 }, { "O", 2 }, { "N", 3 }, { "S", 2 } };

            int numH = 0;
            var r = new Regex(@"^(\D\D?)(\d+?)_");
            for (int i = 0; i < Molecule.AtomList.Length; i++)
            {
                Match m = r.Match(Molecule.AtomList[i]);
                if (!dict.ContainsKey(m.Groups[1].Value)) { continue; }
                int temp = dict[m.Groups[1].Value] + Molecule.Charge[i];
                string bond = m.Groups[2].Value;
                // ReSharper disable once LoopCanBeConvertedToQuery
                //for (int j = 0; j < bond.Length; j++)
                //{
                //    temp -= (bond[j] - 48);
                //}
                temp = bond.Aggregate(temp, (current, t) => current - (t - 48));
                numH += (temp > 0 ? temp : 0);
            }
            return numH;
        }
    }
}
