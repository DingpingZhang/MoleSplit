using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Threading;
using ModernUI;
using ModernUI.Repository;
using ModernUI.ViewModel;

namespace ModernUI.Pages
{
    /// <summary>
    /// Interaction logic for ParseBatchPage.xaml
    /// </summary>
    public partial class ParseBatchPage : UserControl
    {
        private ParseBatchViewModel _vm;

        public ParseBatchPage()
        {
            InitializeComponent();

            #region 配置VM
            _vm = new ParseBatchViewModel();
            _vm.MethodNameToPath = MethodRepository.FetchData();
            #endregion

            DataContext = _vm;
        }

        private void btnStartParse_Click(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(tbFilePath.Text))
            {
                MessageBox.Show("             Invalid file path!             ");
                return;
            }
            //pbSplitMole.Visibility = Visibility.Visible; // 让进度条显示出来
            var directoryPath = tbFilePath.Text; // 线程中不让访问控件，只好先拿出来
            var methodName = cmbMethod.SelectedItem.ToString();
            Thread t = new Thread(() =>
            {
                _vm.ParseFileBatch(directoryPath, methodName);
            });
            t.IsBackground = true;
            t.Start();
        }

        private void btnGetFiles_Click(object sender, RoutedEventArgs e)
        {
            var openFile = new System.Windows.Forms.FolderBrowserDialog();
            var result = openFile.ShowDialog();
            if (result.ToString() == "OK")
            {
                tbFilePath.Text = openFile.SelectedPath;
            }
        }
    }
}
