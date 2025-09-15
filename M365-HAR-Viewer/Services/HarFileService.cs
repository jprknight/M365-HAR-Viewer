using HarSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Cryptography.Core;

namespace M365_HAR_Viewer.Services
{
    public class HarFileService
    {
        private static HarFileService _instance;

        public static HarFileService Instance => _instance ??= new HarFileService();

        public string DeserializeHarFile(string data)
        {
            
            try
            {
                Har harFile = HarConvert.Deserialize(data);

                // Extract relevant information
                // ...

                string stuff = "";

                foreach (var entry in harFile.Log.Entries)
                {
                    // Example: Print request URL and response status
                    stuff += ($"Request URL: {entry.Request.Url}");
                    stuff += ($"Response Status: {entry.Response.Status}");
                }
                return stuff;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
            return null;
        }
    }
}
