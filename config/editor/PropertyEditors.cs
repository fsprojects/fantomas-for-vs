using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace FantomasVs.Editor
{

    public interface IPropertyEditor : INotifyPropertyChanged, IDataErrorInfo
    {
        string DisplayName { get; set; }
        string Description { get; set; }
        string Category { get; set; }
        string CustomEditorTemplate { get; set; }
        object BoxedValue { get; set; }
        PropertyInfo SourceProperty { get; set; }
        object Target { get; set; }

        event EventHandler ValueChanged;
    }

    public class PropertyEditor<TValue> : IPropertyEditor
    {
        private string _displayName;
        private string _description;
        private string _category;

        public PropertyInfo SourceProperty { get; set; }

        public object Target { get; set; }

        public string DisplayName
        {
            get => _displayName;
            set { _displayName = value; OnPropertyChanged(); }
        }

        public string Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(); }
        }

        public string Category
        {
            get => _category;
            set { _category = value; OnPropertyChanged(); }
        }

        public TValue Value
        {
            get => (TValue)BoxedValue;
            set { BoxedValue = value; OnPropertyChanged(); }
        }

        public event EventHandler ValueChanged;

        public object BoxedValue
        {
            get => SourceProperty.GetValue(Target);
            set
            {
                SourceProperty.SetValue(Target, value); 
                ValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public string ErrorMessage { get; set; } = "This value is invalid";

        public string CustomEditorTemplate { get; set; } = null;

        public virtual Predicate<TValue> Validate { get; set; } = _ => true;

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = default) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        string IDataErrorInfo.Error => default;


        string IDataErrorInfo.this[string columnName] =>
            columnName == nameof(Value) ?
            (Validate(Value) ? ErrorMessage : default)
            : default;

        public record NamedValue(string Label, TValue Value);

        public static string Humanize(TValue input) =>
            System.Text.RegularExpressions.Regex.Replace(
                input.ToString(), "(?<=[a-z])(?<x>[A-Z])|(?<=.)(?<x>[A-Z])(?=[a-z])", " ${x}");

        public NamedValue[] Values =>
            typeof(TValue).IsEnum ?
            Enum.GetValues(typeof(TValue))
            .Cast<TValue>()
            .Select(v => new NamedValue(Humanize(v), v))
            .ToArray()
            :
            new NamedValue[] { };
    }

    public class EditorTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (container is FrameworkElement element && item is IPropertyEditor editor)
            {
                var propertyType = editor.SourceProperty.PropertyType;
                var typeName = propertyType.IsEnum ? nameof(Enum) : editor.SourceProperty.PropertyType.Name;
                var templateName = editor.CustomEditorTemplate ?? $"{typeName}Template";
                var dataTemplate = element.TryFindResource(templateName) as DataTemplate;
                return dataTemplate;
            }

            return null;
        }
    }

    public class EditorPage<TOpt> : INotifyPropertyChanged where TOpt : class
    {

        public TOpt Target { get; }

        public ObservableCollection<IPropertyEditor> Editors { get; }

        public event Action<string> PropertyEdited;

        public CollectionView View { get; }

        private string searchText = "";
        public string SearchText
        {
            get => searchText;
            set
            {
                searchText = value;
                OnPropertyChanged();
                Refresh();
            }
        }

        protected virtual void Refresh()
        {
            View.Refresh();
        }

        public EditorPage(TOpt target)
        {
            Target = target;

            var properties = typeof(TOpt).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            Editors = new ObservableCollection<IPropertyEditor>(properties.Where(CanCreateEditor).Select(CreateEditor));

            foreach (var editor in Editors)
            {
                editor.ValueChanged += OnValueChanged;
            }

            View = (CollectionView)CollectionViewSource.GetDefaultView(Editors);
            View.GroupDescriptions.Add(new PropertyGroupDescription(nameof(IPropertyEditor.Category)));
            View.Filter = IsMatching;
        }

        private void OnValueChanged(object sender, EventArgs e)
        {
            var editor = (IPropertyEditor)sender;
            PropertyEdited?.Invoke(editor.SourceProperty.Name);
        }

        protected virtual bool IsMatching(object obj)
        {
            bool TextMatch(string field) =>
                String.IsNullOrEmpty(field) ||
                String.IsNullOrEmpty(SearchText) ||
                field.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0;

            if (obj is IPropertyEditor editor)
            {
                return
                    TextMatch(editor.DisplayName) ||
                    TextMatch(editor.Description) ||
                    TextMatch(editor.Category);
            }

            return false;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = default) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private bool CanCreateEditor(PropertyInfo info) =>
            info.CanRead && info.CanWrite;

        private IPropertyEditor CreateEditor(PropertyInfo info)
        {
            var propType = info.PropertyType;
            var editorType = typeof(PropertyEditor<>).MakeGenericType(propType);
            var editor = (IPropertyEditor)Activator.CreateInstance(editorType);

            editor.DisplayName = info.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? info.Name;
            editor.Category = info.GetCustomAttribute<CategoryAttribute>()?.Category ?? "General";
            editor.Description = info.GetCustomAttribute<DescriptionAttribute>()?.Description ?? "TBD";
            editor.SourceProperty = info;
            editor.Target = Target;
            editor.CustomEditorTemplate = info.GetCustomAttribute<EditorAttribute>()?.EditorTypeName;

            return editor;
        }
    }

}
