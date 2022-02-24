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
    public enum InstallAction
    {
        None,
        Global,
        Local,
        UpdateGlobal,
        UpdateLocal,
        ShowDocs
    }

    public enum InstallResult
    {
        Skipped,
        Succeded,
        Failed
    }

    /// <summary>
    /// Interaction logic for InstallChoiceWindow.xaml
    /// </summary>
    public partial class InstallChoiceWindow : DialogWindow
    {
        class ChoiceCommand : ICommand
        {   
            public Action<object> Action { get; }

            public ChoiceCommand(Action<object> action)
            {
                Action = action;
            }

            public event EventHandler CanExecuteChanged;

            public bool CanExecute(object parameter) => true;

            public void Execute(object parameter)
            {
                CanExecuteChanged?.Invoke(this, new EventArgs());
                Debug.Assert(parameter is InstallAction);
                Action?.Invoke(parameter);
            }
        }        

        public InstallChoiceWindow()
        {
            InitializeComponent();
        }       

        public InstallAction GetDialogAction()
        {
            var result = InstallAction.None;            
            void onClick(object parameter)
            {
                Debug.Assert(parameter is InstallAction);
                this.Close();
                result = (InstallAction)parameter;
            }

            this.DataContext = new ChoiceCommand(onClick);
            this.ShowDialog();
                
            return result;
        }

    }
}