using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace TemperatureSensor.Client
{
    public static class Helpers
    {
        public static async void ShowContentDialog(string dialogTitle, string dialogMessage)
        {
            var dialog = new ContentDialog()
            {
                Title = string.IsNullOrEmpty(dialogTitle) ? "Storage Demo Client" : dialogTitle
            };

            // Setup Content
            var panel = new StackPanel();

            panel.Children.Add(new TextBlock
            {
                Text = dialogMessage,
                TextWrapping = TextWrapping.Wrap,
            });

            dialog.Content = panel;

            // Add Buttons
            dialog.PrimaryButtonText = "Close";
            dialog.IsPrimaryButtonEnabled = false;

            // Show Dialog
            var result = await dialog.ShowAsync();
        }

        public static async Task<string> GetDeviceMACAddress()
        {
            string MAC = null;

            StreamReader SR = await GetJsonStreamData("http://localhost:8080/api/networking/ipconfig");

            JsonObject ResultData = null;

            try
            {
                string JSONData;
                JSONData = SR.ReadToEnd();
                ResultData = JsonObject.Parse(JSONData);
                JsonArray Adapters = ResultData.GetNamedArray("Adapters");

                for (uint index = 0; index < Adapters.Count; index++)
                {
                    JsonObject Adapter = Adapters.GetObjectAt(index).GetObject();

                    string Type = Adapter.GetNamedString("Type");

                    if (Type.ToLower().CompareTo("ethernet") == 0)
                    {
                        MAC = Adapter.GetNamedString("HardwareAddress");
                        break;
                    }
                }
            }
            catch (Exception E)
            {
                System.Diagnostics.Debug.WriteLine(E.Message);
            }
            return MAC;
        }

        private static async Task<StreamReader> GetJsonStreamData(string URL)
        {
            HttpWebRequest wrGETURL = null;

            Stream objStream = null;

            StreamReader objReader = null;

            try
            {
                wrGETURL = (HttpWebRequest)WebRequest.Create(URL);

                wrGETURL.Credentials = new NetworkCredential("Administrator", "p@ssw0rd");

                HttpWebResponse Response = (HttpWebResponse)(await wrGETURL.GetResponseAsync());

                if (Response.StatusCode == HttpStatusCode.OK)
                {
                    objStream = Response.GetResponseStream();
                    objReader = new StreamReader(objStream);
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("GetData " + e.Message);
            }

            return objReader;
        }
    }
}
