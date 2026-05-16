using System.Windows;
using System.Windows.Controls;

namespace BlockFactory.Desktop.Controls
{
    public partial class LoadingOverlay : UserControl
    {
        public static readonly DependencyProperty IsLoadingProperty =
            DependencyProperty.Register(
                nameof(IsLoading),
                typeof(bool),
                typeof(LoadingOverlay),
                new PropertyMetadata(false));

        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register(
                nameof(Message),
                typeof(string),
                typeof(LoadingOverlay),
                new PropertyMetadata("جاري التحميل..."));

        public bool IsLoading
        {
            get => (bool)GetValue(IsLoadingProperty);
            set => SetValue(IsLoadingProperty, value);
        }

        public string Message
        {
            get => (string)GetValue(MessageProperty);
            set => SetValue(MessageProperty, value);
        }

        public LoadingOverlay()
        {
            InitializeComponent();
        }
    }
}
