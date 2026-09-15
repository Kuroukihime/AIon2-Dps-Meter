using AionDpsMeter.UI.ViewModels;
using Microsoft.AspNetCore.Components;

namespace AionDpsMeter.UI.Pages
{
 
    public partial class MainDpsStyle2 : ComponentBase
    {

        [Parameter]
        public MainDpsViewModel? ViewModel { get; set; }
    }
}