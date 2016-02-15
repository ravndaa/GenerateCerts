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
using System.Windows.Shapes;

namespace GenerateCerts.Windows
{
    /// <summary>
    /// Interaction logic for LoadTxt.xaml
    /// </summary>
    public partial class LoadTxt : Window
    {

        
        public LoadTxt()
        {
            InitializeComponent();
        }

        public string lines
        {
            get { return domains.Text; }
        }

        public bool resetlist
        {
            get { return chk_reset.IsChecked.Value; }
        }

        private string _clicked;
        public string clicked
        {
            get { return _clicked; }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            _clicked = "ok";
            Close();

        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            _clicked = "cancel";
            Close();
        }
    }
}
