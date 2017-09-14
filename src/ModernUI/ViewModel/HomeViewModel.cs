using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using ModernUI.Model;
using MoleSplit;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ModernUI.ViewModel
{
    class HomeViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// 方法名称与定义文件路径 键值对
        /// </summary>
        public Dictionary<string, string> MethodNameToPath { get; set; }

        /// <summary>
        /// 拆分结果
        /// </summary>
        private ObservableCollection<MoleFragment> _moleculeFragemnt;
        public ObservableCollection<MoleFragment> MoleculeFragment
        {
            get
            {
                return this._moleculeFragemnt;
            }
            set
            {
                this._moleculeFragemnt = value;
                this.OnPropertyChanged();
            }
        }


        private MoleSplitter _moleSplitter = new MoleSplitter();

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName]string propertyName = "")
        {
            if (PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        public void ParseMole(string molFilePath, string MethodName)
        {
            string rdFilePath = this.MethodNameToPath[MethodName];
            this._moleSplitter.LoadDefineFile(rdFilePath);
            this._moleSplitter.LoadMolFile(molFilePath);
            this._moleSplitter.Parse();
            if (this._moleSplitter.UndefineFragments != null && this._moleSplitter.UndefineFragments.Count != 0)
            {
                this.MoleculeFragment = new ObservableCollection<MoleFragment>() { new MoleFragment { Fragment_Name = "It is indivisible in this method.", Count = 0 } };
                return;
            }
            var tempFragmentCollection = new ObservableCollection<MoleFragment>();
            foreach (var item in _moleSplitter.DefinedFragments)
            {
                tempFragmentCollection.Add(new MoleFragment { Fragment_Name = item.Key, Count = item.Value });
            }
            this.MoleculeFragment = tempFragmentCollection;
        }

        public string GetMolFilePath(string cas)
        {
            return @"F:\张定平\Documents\GitHub\MoleSplit\ModernUI\bin\Release\molFileRepository\" + cas + ".mol";
        }
        public string GetCAS(string molFilePath)
        {
            var temp = molFilePath.Split(new char[] { '\\', '.' });
            if (temp.Length - 2 >= 0)
                return temp[temp.Length - 2];
            else
                return "Untitled";
            //return new Regex(@"(\d+?-\d+?-\d+?)\.mol").Match(molFilePath).Groups[1].Value;
        }
    }
}
