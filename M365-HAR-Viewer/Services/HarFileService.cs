using HarSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Cryptography.Core;
using static M365_HAR_Viewer.MainWindow;

namespace M365_HAR_Viewer.Services
{
    public class HarFileService
    {
        private static HarFileService _instance;

        public static HarFileService Instance => _instance ??= new HarFileService();


        
    }
}
