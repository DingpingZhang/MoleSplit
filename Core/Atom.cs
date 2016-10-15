using System;
using System.Collections.Generic;
using System.Linq;

namespace MoleSplit.Core
{
    /// <summary>
    /// 原子识别器
    /// </summary>
    internal class Atom : RecognizerBase
    {
        // ---------------------------------------------------------------------------------
        private Dictionary<string, string> _atomPattern;
        private Dictionary<string, string[]> _atomTag;
        // ---------------------------------------------------------------------------------
        public override void Load(string text)
        {
            _atomPattern = new Dictionary<string, string>();
            _atomTag = new Dictionary<string, string[]>();
            var itemArray = text.Split(new[] { "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string item in itemArray)
            {
                string[] atom = item.Split(new[] { '\n', '\r', ':' }, StringSplitOptions.RemoveEmptyEntries);
                var temp = atom[0].Split(new[] { '?' }, StringSplitOptions.RemoveEmptyEntries);
                _atomPattern.Add(atom[1], temp[0]);
                if (temp.Length > 1)
                {
                    var tags = new string[temp.Length - 1];
                    for (int j = 0; j < tags.Length; j++)
                    {
                        tags[j] = temp[j + 1];
                    }
                    _atomTag.Add(atom[1], tags);
                }
            }
        }

        public override void Parse()
        {
            DefinedFragment = new Dictionary<string, int>();
            UndefinedFragment = new Dictionary<string, int>();

            for (int i = 0; i < Molecule.AtomList.Length; i++)
            {
                if (Molecule.AtomState[i] != 0) { continue; }
                string atomCode = Molecule.AtomList[i].Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries)[0];
                if (_atomPattern.ContainsKey(atomCode))
                {
                    var trueName = _atomPattern[atomCode];
                    if (_atomTag.ContainsKey(atomCode))
                    {
                        trueName += RecAttribute(_atomTag[atomCode], i);
                    }
                    if (!DefinedFragment.ContainsKey(trueName)) { DefinedFragment.Add(trueName, 0); }
                    DefinedFragment[trueName]++;
                }
                else
                {
                    if (!UndefinedFragment.ContainsKey(atomCode)) { UndefinedFragment.Add(atomCode, 0); }
                    UndefinedFragment[atomCode]++;
                }
            }
        }
        // ---------------------------------------------------------------------------------
        private string RecAttribute(IEnumerable<string> attributeTag, int index)
        {
            var attribute = "";
            foreach (var attributeTagItem in attributeTag)
            {
                if (attributeTagItem[0] != '-')
                {
                    attribute = Molecule.AtomList[index].Contains(attributeTagItem) ? '_' + attributeTagItem : "";
                }
                else
                {
                    string tempTag = attributeTagItem.Remove(0, 1);
                    if (Molecule.AtomList[index].Contains(tempTag)) continue; // 1.自己不能含有该属性
                    if (Molecule.AtomList.Where((t, j) => Molecule.AdjMat[index, j] != 0
                                                          && t.Contains(tempTag)).Any())
                    {
                        attribute = '_' + attributeTagItem;
                    }
                }
                if (attribute != "") return attribute;
            }
            return "";
        }
    }
}
