using AionDpsMeter.UI.ViewModels;
using Microsoft.AspNetCore.Components;

namespace AionDpsMeter.UI.Pages
{
    public partial class MainDpsMinimal : ComponentBase
    {

        [Parameter]
        public MainDpsViewModel? ViewModel { get; set; }

    }
}