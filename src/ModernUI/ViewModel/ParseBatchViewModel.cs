using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using ModernUI.Model;
using MoleSplit;
using ModernUI.Repository;
using System.IO;
using System.Threading;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ModernUI.ViewModel
{
    class ParseBatchViewModel : INotifyPropertyChanged
    {

        /// <summary>
        /// 方法名称与定义文件路径 键值对
        /// </summary>
        public Dictionary<string, string> MethodNameToPath { get; set; }

        /// <summary>
        /// 计数：已处理完成的文件
        /// </summary>
        private double _parseProgress;
        public double ParseProgress
        {
            get
            {
                return _parseProgress;
            }
            set
            {
                _parseProgress = value;
                OnPropertyChanged();
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName]string propertyName = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        public int FileNum { get; set; }

        public int SplitedFileCount { get; set; }

        private MoleSplitter _moleSplitter = new MoleSplitter();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="directoryPath"></param>
        /// <param name="methodName"></param>
        public void ParseFileBatch(string directoryPath, string methodName)
        {
            var saveFilePath = directoryPath + "\\Result_" + methodName + ".txt";
            var molFilesPath = Directory.GetFiles(directoryPath);
            FileNum = molFilesPath.Length;
            SplitedFileCount = 0;

            _moleSplitter.LoadDefineFile(MethodNameToPath[methodName]);
            Regex r_FetchCAS = new Regex(@"(\d+?-\d\d?-\d\d?)\.mol", RegexOptions.Compiled);
            for (int i = 0; i < molFilesPath.Length; i++)
            {
                _moleSplitter.LoadMolFile(molFilesPath[i]);
                _moleSplitter.Parse();
                if (_moleSplitter.UndefineFragments.Count != 0)
                {
                    _moleSplitter.DefinedFragments.Clear();
                }
                var strCAS = r_FetchCAS.Match(molFilesPath[i]).Groups[1].Value;
                if (strCAS != "")
                {
                    SplitResultBatchRepository.WriteData(saveFilePath, strCAS, _moleSplitter.DefinedFragments);
                }
                ParseProgress = (++SplitedFileCount * 1.0 / FileNum);
            }
        }
    }
}
