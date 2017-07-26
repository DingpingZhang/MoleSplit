using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using MoleSplit.Core;

namespace MoleSplit
{
    /// <summary>
    /// 分子自动拆分
    /// </summary>
    public class MoleSplitter
    {
        /// <summary>
        /// 用于拆分完成后对结果进行进一步处理
        /// </summary>
        public event EventHandler<SplitedEventArgs> Splited;

        /// <summary>
        /// 解析器组
        /// </summary>
        private ICollection<RecognizerBase> _recognizers;

        // ------------------------------------------------------------------------------------

        /// <summary>
        /// 加载定义文件
        /// </summary>
        /// <param name="filePath"></param>
        public void LoadDefineFile(string filePath)
        {
            if (!(File.Exists(filePath) && new Regex(".mdef$").IsMatch(filePath))) { return; }
            string[] temp;
            using (var sr = new StreamReader(filePath))
            {
                temp = sr.ReadToEnd().Split(new[] { '#' }, StringSplitOptions.RemoveEmptyEntries);
            }
            _recognizers = new List<RecognizerBase>();
            for (var i = 0; i < temp.Length; i += 2)
            {
                var tempType = Type.GetType("MoleSplit.Core." + temp[i]);
                if (tempType == null) continue;
                if (Activator.CreateInstance(tempType) is RecognizerBase tempInstance)
                {
                    tempInstance.Load(temp[i + 1]);
                    _recognizers.Add(tempInstance);
                }
            }
        }

        /// <summary>
        /// 启动解析
        /// </summary>
        public (IReadOnlyCollection<MoleculeFragment> DefinedFragments, IReadOnlyCollection<MoleculeFragment> UndefinedFragments) Parse(Molecule molecule)
        {
            if (_recognizers == null) throw new ArgumentException();

            // 1.进行解析
            foreach (var recognizer in _recognizers)
            {
                recognizer.Molecule = molecule;
                recognizer.Parse();
            }
            // 2.结算结果
            var tempDefinedFragments = new Dictionary<string, int>();
            var tempUndefineFragments = new Dictionary<string, int>();
            foreach (var paserItem in _recognizers)
            {
                if (paserItem.DefinedFragment != null && paserItem.DefinedFragment.Count != 0)
                {
                    foreach (var item in paserItem.DefinedFragment)
                    {
                        if (!tempDefinedFragments.ContainsKey(item.Key))
                            tempDefinedFragments.Add(item.Key, item.Value);
                        else
                            tempDefinedFragments[item.Key] += item.Value;
                    }
                }
                if (paserItem.UndefinedFragment != null && paserItem.UndefinedFragment.Count != 0)
                {
                    foreach (var item in paserItem.UndefinedFragment)
                    {
                        if (!tempUndefineFragments.ContainsKey(item.Key))
                            tempUndefineFragments.Add(item.Key, item.Value);
                        else
                            tempUndefineFragments[item.Key] += item.Value;
                    }
                }
            }
            var definedFragments = (from item in tempDefinedFragments
                                    select new MoleculeFragment(item.Key, item.Value)).ToArray();
            var undefinedFragments = (from item in tempUndefineFragments
                                      select new MoleculeFragment(item.Key, item.Value)).ToArray();
            Splited?.Invoke(this, new SplitedEventArgs
            {
                Molecule = molecule,
                DefinedFragment = definedFragments,
                UndefinedFragment = undefinedFragments
            });
            return (definedFragments, undefinedFragments);
        }

        /// <summary>
        /// 清空加载的所有数据，包括分子结构信息和基团定义信息
        /// </summary>
        public void Clear()
        {
            _recognizers = null;
            Splited = null;
        }
    }
}
