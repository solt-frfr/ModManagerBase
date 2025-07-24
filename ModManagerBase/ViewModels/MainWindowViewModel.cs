using System.Collections.ObjectModel;

namespace ModManagerBase.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private ObservableCollection<Meta> _allmods;

    public ObservableCollection<Meta> AllMods
    {
        get { return _allmods; }
        set { SetProperty(ref _allmods, value); }
    }

    public MainWindowViewModel()
    {
        AllMods = new ObservableCollection<Meta> 
        {
            new Meta { Name = "Please Press Refresh." },
        };
    }
}