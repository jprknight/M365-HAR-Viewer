using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.Storage.Pickers;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace M365_HAR_Viewer
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        public async void LoadFileButton_Click(object sender, RoutedEventArgs e)
        {
            var openPicker = new FileOpenPicker(this.AppWindow.Id)
            {
                // (Optional) Specify the initial location for the picker. 
                //     If the specified location doesn't exist on the user's machine, it falls back to the DocumentsLibrary.
                //     If not set, it defaults to PickerLocationId.Unspecified, and the system will use its default location.
                SuggestedStartLocation = PickerLocationId.Downloads,

                // (Optional) specify the text displayed on the commit button. 
                //     If not specified, the system uses a default label of "Open" (suitably translated).
                //CommitButtonText = "Choose selected files",

                // (Optional) specify file extension filters. If not specified, defaults to all files (*.*).
                FileTypeFilter = { ".har" },

                // (Optional) specify the view mode of the picker dialog. If not specified, defaults to List.
                ViewMode = PickerViewMode.List,
            };

            var result = await openPicker.PickSingleFileAsync();
            if (result is not null)
            {
                var content = System.IO.File.ReadAllText(result.Path);
            }
            else
            {
                // Add your error handling here.
            }

            LoadFileButton.Visibility = Visibility.Collapsed;
        }
    }
}
