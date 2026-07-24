using Avalonia.Layout;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using STACK_HUB.Docking.Models;
using STACK_HUB.Docking.Services;
using STACK_HUB.Models;

namespace STACK_HUB.ViewModels;

public partial class MainViewModel : ObservableObject
{
    public StackQuestion? ActiveQuestion { get; set; }

    public MainViewModel()
    {
        var q = new StackQuestion();
        ActiveQuestion = q;
    }
}