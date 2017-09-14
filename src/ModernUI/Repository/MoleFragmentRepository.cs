using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MoleSplit;
using ModernUI.Model;

namespace ModernUI.Repository
{
    class MoleFragmentRepository
    {
        private static MoleSplitter _moleSplitter = new MoleSplitter();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="defineFilePath"></param>
        /// <param name="CAS"></param>
        /// <returns></returns>
        public static ObservableCollection<MoleFragment> GetData(string defineFilePath, string CAS)
        {
            var moleFragment = new ObservableCollection<MoleFragment>();
            string molFilePath = @"molFileRepository\" + CAS + ".mol";

            _moleSplitter.LoadMolFile(molFilePath);
            _moleSplitter.LoadDefineFile(defineFilePath);
            _moleSplitter.Parse();
            foreach (var item in _moleSplitter.DefinedFragments)
            {
                moleFragment.Add(new MoleFragment { Fragment_Name = item.Key, Count = item.Value });
            }
            if (_moleSplitter.UndefineFragments.Count != 0)
            {
                return new ObservableCollection<MoleFragment>() { new MoleFragment { Fragment_Name = "No Results", Count = 0 } };
            }
      
            return moleFragment;
        }
    }
}
