using MoleSplit.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MoleSplit
{
    /// <summary>
    /// 拆分后处理事件参数类
    /// </summary>
    public class SplitedEventArgs : EventArgs
    {
        /// <summary>
        /// 当前被解析的分子
        /// </summary>
        public Molecule Molecule { get; set; }

        /// <summary>
        /// 预定义碎片
        /// </summary>
        public IReadOnlyCollection<MoleculeFragment> DefinedFragment { get; set; }

        /// <summary>
        /// 未定义碎片
        /// </summary>
        public IReadOnlyCollection<MoleculeFragment> UndefinedFragment { get; set; }
    }
}
