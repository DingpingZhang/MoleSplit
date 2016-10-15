using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using MoleSplit.Core;

namespace MoleSplit
{
    /// <summary>
    /// 拆分完成后处理事件
    /// </summary>
    /// <param name="e">拆分结果</param>
    public delegate void SplitEndEventHandler(SplitEndEventArgs e);

    /// <summary>
    /// 拆分后处理事件参数类
    /// </summary>
    public class SplitEndEventArgs
    {
        /// <summary>
        /// 当前被解析的分子
        /// </summary>
        public MoleInfo Molecule { get; set; }

        /// <summary>
        /// 预定义碎片
        /// </summary>
        public Dictionary<string, int> DefinedFragment { get; set; }

        /// <summary>
        /// 未定义碎片
        /// </summary>
        public Dictionary<string, int> UndefinedFragment { get; set; }
    }

    /// <summary>
    /// 分子自动拆分
    /// </summary>
    public class MoleSplitter
    {

        /// <summary>
        /// 定义的分子片段
        /// </summary>
        public Dictionary<string, int> DefinedFragment { get; protected set; }

        /// <summary>
        /// 未定义的分子片段
        /// </summary>
        public Dictionary<string, int> UndefineFragment { get; protected set; }

        // ------------------------------------------------------------------------------------

        /// <summary>
        /// 待解析的分子
        /// </summary>
        private MoleInfo _molecule;

        // ------------------------------------------------------------------------------------

        /// <summary>
        /// 用于拆分完成后对结果进行进一步处理
        /// </summary>
        public event SplitEndEventHandler SplitEnd;

        /// <summary>
        /// 解析器组
        /// </summary>
        private List<RecognizerBase> _recognizerList;

        // ------------------------------------------------------------------------------------

        /// <summary>
        /// 加载mol文件
        /// </summary>
        /// <param name="filePath"></param>
        public void LoadMolFile(string filePath)
        {
            if (!(File.Exists(filePath) && new Regex(".mol$").IsMatch(filePath))) { return; }
            using (var sr = new StreamReader(filePath))
            {
                _molecule = new MoleInfo(sr.ReadToEnd());
            }
        }

        /// <summary>
        /// 加载定义文件
        /// </summary>
        /// <param name="filePath"></param>
        public void LoadDefineFile(string filePath)
        {
            if (_molecule != null) { _molecule.AtomState = new int[_molecule.AtomState.Length]; }

            if (!(File.Exists(filePath) && new Regex(".mdef$").IsMatch(filePath))) { return; }
            string[] temp;
            using (var sr = new StreamReader(filePath))
            {
                temp = sr.ReadToEnd().Split(new[] { '#' }, StringSplitOptions.RemoveEmptyEntries);
            }
            _recognizerList = new List<RecognizerBase>();
            for (var i = 0; i < temp.Length; i += 2)
            {
                var tempType = Type.GetType("MoleSplit.Core." + temp[i]);
                if (tempType == null) continue;
                var tempInstance = Activator.CreateInstance(tempType) as RecognizerBase;
                if (tempInstance == null) continue;
                tempInstance.Load(temp[i + 1]);
                _recognizerList.Add(tempInstance);
            }
        }

        /// <summary>
        /// 启动解析
        /// </summary>
        public void Parse()
        {
            if (_recognizerList == null || _molecule == null) { return; }
            // 1.进行解析
            foreach (var recognizer in _recognizerList)
            {
                recognizer.Molecule = _molecule;
                recognizer.Parse();
            }
            // 2.结算结果
            DefinedFragment = new Dictionary<string, int>();
            UndefineFragment = new Dictionary<string, int>();
            foreach (var paserItem in _recognizerList)
            {
                if (paserItem.DefinedFragment != null && paserItem.DefinedFragment.Count != 0)
                {
                    foreach (var item in paserItem.DefinedFragment)
                    {
                        if (!DefinedFragment.ContainsKey(item.Key))
                            DefinedFragment.Add(item.Key, item.Value);
                        else
                            DefinedFragment[item.Key] += item.Value;
                    }
                }
                if (paserItem.UndefinedFragment != null && paserItem.UndefinedFragment.Count != 0)
                {
                    foreach (var item in paserItem.UndefinedFragment)
                    {
                        if (!UndefineFragment.ContainsKey(item.Key))
                            UndefineFragment.Add(item.Key, item.Value);
                        else
                            UndefineFragment[item.Key] += item.Value;
                    }
                }
            }
            SplitEnd?.Invoke(new SplitEndEventArgs
            {
                Molecule = _molecule,
                DefinedFragment = DefinedFragment,
                UndefinedFragment = UndefineFragment
            });
        }

        /// <summary>
        /// 清空加载的所有数据，包括分子结构信息和基团定义信息
        /// </summary>
        public void Clear()
        {
            _molecule = null;
            SplitEnd = null;
            _recognizerList = null;
            DefinedFragment = null;
            UndefineFragment = null;
        }
    }
}
