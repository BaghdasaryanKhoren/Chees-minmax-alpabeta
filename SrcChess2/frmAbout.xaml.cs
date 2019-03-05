using System.Windows;

namespace SrcChess2 {
    /// <summary>
    /// Interaction logic for frmAbout.xaml
    /// </summary>
    public partial class frmAbout : Window {
        
        /// <summary>
        /// Class CTor
        /// </summary>
        public frmAbout() {
            InitializeComponent();
        }

        /// <summary>
        /// Called when the Ok button is closed
        /// </summary>
        /// <param name="sender">   Sender object</param>
        /// <param name="e">        Event argument</param>
        private void butOk_Click(object sender, RoutedEventArgs e) {
            Close();
        }
    }
}
