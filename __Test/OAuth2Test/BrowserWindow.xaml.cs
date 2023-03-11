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
using Haley.WPF.Controls;

namespace OAuth2Test
{
    /// <summary>
    /// Interaction logic for BrowserWindow.xaml
    /// </summary>
    public partial class BrowserWindow : PlainWindow
    {
        string _htmlContent;
        string _url;
        bool _isContent;
        public BrowserWindow(string content,bool isContent = true)
        {
            _isContent= isContent;
            if (isContent) {
                _htmlContent = content;
            } else {
                _url = content;
            }
            InitializeComponent();
        }

        public BrowserWindow() : this(null) { }

        protected override async void OnContentRendered(EventArgs e) {
            base.OnContentRendered(e);
            if (wv2 == null) return;

            await wv2.EnsureCoreWebView2Async();
            if (string.IsNullOrWhiteSpace(_htmlContent) && string.IsNullOrWhiteSpace(_url)) return;
            if (_isContent) {
                wv2.NavigateToString(_htmlContent);
            } else {
                wv2.Source = new Uri(_url);
            }
        }

        internal void CloseWindow() {
            this.DialogResult = true;
        }
    }
}
