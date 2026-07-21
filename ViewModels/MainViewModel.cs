using STACK_HUB.Models;

namespace STACK_HUB.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private StackQuestion? _activeQuestion;
    
    public StackQuestion? ActiveQuestion
    {
        get => _activeQuestion;
        set
        {
            _activeQuestion = value;
        }
    }

    public MainViewModel()
    {
       var q = new StackQuestion();
       ActiveQuestion = q;
    }
}