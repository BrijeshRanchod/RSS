// MainWindow.xaml.cs
using System.Windows;
using RSSPOS.ViewModels;

namespace RSSPOS;

public partial class MainWindow : Window
{
    public MainWindow(PosViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}
