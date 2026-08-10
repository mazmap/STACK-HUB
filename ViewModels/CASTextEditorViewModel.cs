using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace STACK_HUB.ViewModels;

public partial class CASTextEditorViewModel : ViewModelBase
{
    private readonly Action<string> _onTextChanged;

    [ObservableProperty]
    private string _text = string.Empty;

    public CASTextEditorViewModel(string initialText, Action<string> onTextChanged)
    {
        _text = initialText ?? string.Empty;
        _onTextChanged = onTextChanged;
    }

    partial void OnTextChanged(string value)
    {
        _onTextChanged?.Invoke(value);
    }
}