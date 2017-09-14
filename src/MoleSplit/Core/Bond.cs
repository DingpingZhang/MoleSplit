using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace MoleSplit.Core
{
    /// <summary>
    /// 化学键识别器
    /// </summary>
    internal class Bond : RecognizerBase
    {
        // ---------------------------------------------------------------------------------
        private List<string> _bondPattern;
        private Dictionary<string, string> _bondTag;
        private readonly char[] _bondType = { '-', '=', '≡', '#' };
        // ---------------------------------------------------------------------------------
        public override void Load(string text)
        {
            var itemArray = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            _bondPattern = new List<string>();
            _bondTag = new Dictionary<string, string>();
            foreach (string item in itemArray)
            {
                var temp = item.Split(new[] { '?' }, StringSplitOptions.RemoveEmptyEntries);
                _bondPattern.Add(temp[0]);
                if (temp.Length > 1)
                {
                    _bondTag.Add(temp[0], temp[1]);
                }
            }
        }
        public override void Parse()
        {
            DefinedFragment = new Dictionary<string, int>();
            UndefinedFragment = new Dictionary<string, int>();

            var r = new Regex(@"\D\D?");
            for (int i = 1; i < Molecule.AtomList.Length; i++)
            {
                for (int j = 0; j < i; j++)
                {
                    if (Molecule.AdjMat[i, j] != 0
                    && (Molecule.BondState[i, j] == -1 || Molecule.BondState[i, j] == 0))
                    {
                        var atomLeft = r.Match(Molecule.AtomList[i]).Value;
                        var atomRight = r.Match(Molecule.AtomList[j]).Value;
                        var bondNum = (Molecule.AdjMat[i, j] - 1) 
                            + (Molecule.Charge[i] != 0 && Molecule.Charge[j] != 0 ? 1 : 0); // 如果键两端的原子带电荷，则相当于增加一个键

                        var type1 = atomLeft + _bondType[bondNum] + atomRight;
                        var type2 = atomRight + _bondType[bondNum] + atomLeft;

                        if (_bondPattern.Contains(type2)) { type1 = type2; }
                        if (_bondPattern.Contains(type1))
                        {
                            if (_bondTag.ContainsKey(type1))
                            {
                                type1 += Molecule.BondState[i, j] < 0 ? '_' + _bondTag[type1] : "";
                            }
                            if (!DefinedFragment.ContainsKey(type1)) { DefinedFragment.Add(type1, 0); }
                            DefinedFragment[type1]++;
                        }
                        else
                        {
                            if (!UndefinedFragment.ContainsKey(type1)) { UndefinedFragment.Add(type1, 0); }
                            UndefinedFragment[type1]++;
                        }
                    }
                }
            }
            foreach (var item in Bond_H())
            {
                if (_bondPattern.Contains(item.Key))
                {
                    DefinedFragment.Add(item.Key, item.Value);
                }
                else
                {
                    UndefinedFragment.Add(item.Key, item.Value);
                }
            }
        }
        // ---------------------------------------------------------------------------------
        private Dictionary<string, int> Bond_H()
        {
            var dict = new Dictionary<string, int> { { "C", 4 }, { "Si", 4 }, { "O", 2 }, { "N", 3 }, { "S", 2 } };

            var recorder = new Dictionary<string, int>();
            var r = new Regex(@"^(\D\D?)(\d+?)_");
            for (int i = 0; i < Molecule.AtomList.Length; i++)
            {
                Match m = r.Match(Molecule.AtomList[i]);
                if (!dict.ContainsKey(m.Groups[1].Value)) { continue; }
                int temp = dict[m.Groups[1].Value] + Molecule.Charge[i];
                string bond = m.Groups[2].Value;
                foreach (char charBond in bond)
                {
                    temp -= (charBond - 48);
                }
                if (temp > 0)
                {
                    var bondHName = m.Groups[1].Value + "-H";
                    if (!recorder.ContainsKey(bondHName)) { recorder.Add(bondHName, 0); }
                    recorder[bondHName] += temp;
                }
            }
            return recorder;
        }
    }
}
