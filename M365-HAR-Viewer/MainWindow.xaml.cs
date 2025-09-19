using HarSharp;
using M365_HAR_Viewer.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.Storage.Pickers;
using Microsoft.WindowsAppSDK.Runtime;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json.Serialization;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Security.Cryptography.Core;
using Windows.UI.Popups;
using static M365_HAR_Viewer.Services.HarFileService;
using static System.Runtime.InteropServices.JavaScript.JSType;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace M365_HAR_Viewer
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private readonly ObservableCollection<Session> _sessions = [];
        public ObservableCollection<Session> Sessions
        {
            get { return _sessions; }
        }

        public MainWindow()
        {
            InitializeComponent();
        }

        public class Session
        {
            public int ID { get; set; }
            public int ResponseCode { get; set; }
            public Uri Uri { get; set; }
        }

        public void AddSessions(string data)
        {
            try
            {
                Har harFileData = HarConvert.Deserialize(data);

                int iSessionID = 0;

                foreach (var entry in harFileData.Log.Entries)
                {
                    iSessionID++;
                    Sessions.Add(new Session()
                    {
                        ID = iSessionID,
                        ResponseCode = entry.Response.Status,
                        Uri = entry.Request.Url
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
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
                try
                {
                    string file = File.ReadAllText(result.Path);

                    AddSessions(file);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message);
                }
            }
            else
            {
                var dialog = new MessageDialog("Failed to open the selected file. Please try again.");
                await dialog.ShowAsync();
            }

            LoadFileButton.Visibility = Visibility.Collapsed;
            SessionsDataGrid.Visibility = Visibility.Visible;
            ToggleViewSwitch.Visibility = Visibility.Visible;
        }

        public void ToggleViewSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            if (ToggleViewSwitch.IsOn)
            {
                SessionsDataGrid.Columns[3].Visibility = Visibility.Visible; // Show Process column
                SessionsDataGrid.GridLinesVisibility = CommunityToolkit.WinUI.UI.Controls.DataGridGridLinesVisibility.All;
            }
            else
            {
                SessionsDataGrid.Columns[3].Visibility = Visibility.Collapsed; // Hide Process column
                SessionsDataGrid.GridLinesVisibility = CommunityToolkit.WinUI.UI.Controls.DataGridGridLinesVisibility.None;
            }
        }

        public void ToggleViewSwitch1_Toggled(object sender, RoutedEventArgs e)
        {
            if (ToggleViewSwitch1.IsOn)
            {
                SessionsDataGrid.Columns[3].Visibility = Visibility.Visible; // Show Process column
                SessionsDataGrid.GridLinesVisibility = CommunityToolkit.WinUI.UI.Controls.DataGridGridLinesVisibility.All;
            }
            else
            {
                SessionsDataGrid.Columns[3].Visibility = Visibility.Collapsed; // Hide Process column
                SessionsDataGrid.GridLinesVisibility = CommunityToolkit.WinUI.UI.Controls.DataGridGridLinesVisibility.None;
            }
        }

        public void SessionsDataGrid_click(object sender, RoutedEventArgs e)
        {

        }
    }
}
