using Microsoft.VisualStudio.PlatformUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace FantomasVs
{
    /// <summary>
    /// Interaction logic for ModalDialogWindow.xaml
    /// </summary>
    public partial class InstallResultDialog : DialogWindow
    {
        public InstallResultDialog()
        {
            InitializeComponent();
        }

        public void ShowModal(string text)
        {
            this.MessageText.Text = text;
            MessageContent.Document.Blocks
                .OfType<Paragraph>()
                .SelectMany(c => c.Inlines)
                .OfType<Hyperlink>()
                .ToList()
                .ForEach(block => {
                    var uri = block.NavigateUri.AbsoluteUri;
                    block.RequestNavigate += (s, e) =>
                    {
                        Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
                    };
                    block.ToolTip = $"Open {uri}";  
                });
            this.CloseButton.Click += (s, e) => this.Close();
            this.ShowModal();
        }

        public static void ShowDialog(string text)
        {
            new InstallResultDialog().ShowModal(text);
        }
    }
}