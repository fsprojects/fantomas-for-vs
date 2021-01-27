using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;

namespace FantomasVs.Editor
{
    [TemplatePart(Name = "PART_UpButton", Type = typeof(RepeatButton))]
    [TemplatePart(Name = "PART_DownButton", Type = typeof(RepeatButton))]
    [TemplatePart(Name = "PART_Text", Type = typeof(TextBox))]
    public class SpinControl : Control
    {
        public int Value
        {
            get { return (int)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        private RepeatButton UpButtonElement;
        private RepeatButton DownButtonElement;

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(int), typeof(SpinControl), new PropertyMetadata());

        public override void OnApplyTemplate()
        {
            if (UpButtonElement != null) UpButtonElement.Click -= OnUpClick;
            if (DownButtonElement != null) DownButtonElement.Click -= OnDownClick;

            UpButtonElement = GetTemplateChild("PART_UpButton") as RepeatButton;
            DownButtonElement = GetTemplateChild("PART_DownButton") as RepeatButton;

            if (UpButtonElement != null) UpButtonElement.Click += OnUpClick;
            if (DownButtonElement != null) DownButtonElement.Click += OnDownClick;
        }

        private void OnUpClick(object _, object e) => Value += 1;
        private void OnDownClick(object _, object e) => Value -= 1;

    }
}
