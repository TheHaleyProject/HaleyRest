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
using Haley.Models;
using Haley.WPF.Controls;


namespace OAuth2Test {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : PlainWindow {
        public MainWindow() {
            var vm = new AuthVM();
            WindowObserver wobserver = new WindowObserver(this, vm);
            InitializeComponent();
            this.DataContext = vm;

        }
    }
}
