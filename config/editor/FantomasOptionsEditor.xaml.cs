namespace FantomasVs.Editor
{
    using System.Windows.Controls;
    using System.Windows.Data;

    /// <summary>
    /// Interaction logic for FantomasOptionsPage2.xaml
    /// </summary>
    public partial class FantomasOptionsEditor : UserControl
    {
        public FantomasOptionsPage FantomasOptions { get; }
        
        public EditorPage<FantomasOptionsPage> Editor { get; }

        public FantomasOptionsEditor()
        {
            InitializeComponent();
            FantomasOptions = new FantomasOptionsPage();
            Editor = new EditorPage<FantomasOptionsPage>(FantomasOptions);
            DataContext = Editor;
            PropertyView.ItemsSource = Editor.View;
        }

    }
}
