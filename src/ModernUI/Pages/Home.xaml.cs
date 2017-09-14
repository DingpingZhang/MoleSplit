using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

using ModernUI;
using ModernUI.Repository;
using ModernUI.ViewModel;


namespace ModernUI.Pages
{
    /// <summary>
    /// Interaction logic for Home.xaml
    /// </summary>
    public partial class Home : UserControl
    {
        private HomeViewModel _homeViewModel;

        public Home()
        {
            InitializeComponent();

            this._homeViewModel = new HomeViewModel();
            this._homeViewModel.MethodNameToPath = MethodRepository.FetchData();
            this.DataContext = this._homeViewModel;
        }

        private void btnParse_Click(object sender, RoutedEventArgs e)
        {
            if (tbCAS.Text == null || tbCAS.Text == "") { return; }
            this._homeViewModel.ParseMole(this._homeViewModel.GetMolFilePath(tbCAS.Text), cmbMethod.SelectedItem.ToString());
        }

        private void DataGrid_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) { return; }

            var molFilePath = ((Array)e.Data.GetData(DataFormats.FileDrop)).GetValue(0).ToString();
            this._homeViewModel.ParseMole(molFilePath, cmbMethod.SelectedItem.ToString());
            this.tbCAS.Text = this._homeViewModel.GetCAS(molFilePath);
        }

    }
}
