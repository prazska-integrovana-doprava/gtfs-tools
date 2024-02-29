using System.Windows;

namespace TrainsEditor
{
    /// <summary>
    /// Interaction logic for TextWindow.xaml
    /// </summary>
    public partial class TextWindow : Window
    {
        public TextWindow()
        {
            InitializeComponent();
        }

        public static void ShowTextInfo(string text)
        {
            var window = new TextWindow();
            window.textBox.Text = text;
            window.Show();
        }
    }
}
