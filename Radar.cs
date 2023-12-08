using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Weather_Images.Form1;

namespace Weather_Images
{
    public class Radar : INotifyPropertyChanged
    {
        public Radar(string inputFolder)
        {
            Folder = inputFolder;
        }

        public string Folder { get; }
        public string DefaultLink { get; } = "https://ims.gov.il/sites/default/files/ims_data/map_images/IMSRadar4GIS/IMSRadar4GIS_";
        public event PropertyChangedEventHandler PropertyChanged;

        public RadarState State
        {
            get => _state;
            set
            {
                if (!Equals(_state, value))
                {
                    _state = value;
                    OnPropertyChanged();
                }
            }
        }
        RadarState _state = default;
        public int Progress
        {
            get => _progress;
            set
            {
                if (!Equals(_progress, value))
                {
                    _progress = value;
                    OnPropertyChanged();
                }
            }
        }
        int _progress = default;

        int successfulDownloadsCount = 0;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public async Task ExececuteAsync()
        {
            State = RadarState.Initializing;
            PrepareLinks();
            await DownloadImagesAsync();
            await ProcessDownloadedImagesAsync();
            State = RadarState.Waiting;
        }
        public List<string> Links { get; } = new List<string>();
        public Dictionary<string, string> LinksAndFileNames { get; } = new Dictionary<string, string>();
        public void PrepareLinks()
        {
            LinksAndFileNames.Clear();
            Links.Clear();

            GenerateRadarLinks();

            // Exclude links for files that already exist in the folder
            foreach (var existing in Directory.GetFiles(Folder, "*.png").Select(Path.GetFileNameWithoutExtension))
            {
                var link = Links.FirstOrDefault(_ => _.Contains(existing));
                if (link != null)
                {
                    Links.Remove(link);
                }
            }
        }

        private void GenerateRadarLinks()
        {
            DateTime current = RoundDown(DateTime.Now, 1);

            if (!Directory.Exists(Folder + "\\Dates"))
            {
                Directory.CreateDirectory(Folder + "\\Dates");
            }

            using (StreamWriter w = new StreamWriter(Folder + "\\Dates\\" + "dates.txt"))
            using (StreamWriter ww = new StreamWriter(Folder + "\\Dates\\" + "datesTime.txt"))
            {
                for (int i = 0; i < 200; i++)
                {
                    var date = current.ToString("yyyyMMddHHmm");
                    ww.WriteLine(current.ToString());
                    w.WriteLine(date);
                    var link = DefaultLink + date + "_0.png";
                    Links.Add(link);
                    LinksAndFileNames.Add(link, current.ToString("yyyy_MM_dd_HH_mm"));
                    current = current.AddMinutes(-1);
                }
            }
        }
        private DateTime RoundDown(DateTime dt, int NearestMinuteInterval) =>
            new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute / NearestMinuteInterval * NearestMinuteInterval, 0);

        private async Task DownloadImagesAsync()
        {
            Progress = 0;
            int totalFiles = Links.Count;
            int completedFiles = 0;

            if (Links.Any())
            {
                // Signal that the actual download has started
                State = RadarState.Downloading;

                using (HttpClient client = new HttpClient())
                {
                    foreach (var link in Links)
                    {
                        var fileName = LinksAndFileNames[link];
                        var filePath = Path.Combine(Folder, fileName + ".png");

                        try
                        {
                            // Check if the file already exists
                            if (!File.Exists(filePath))
                            {
                                // Download the file
                                byte[] fileData = await client.GetByteArrayAsync(link);

                                // Save the file
                                File.WriteAllBytes(filePath, fileData);
                            }
                        }
                        catch (Exception ex)
                        {
                            // Handle download errors if necessary
                            Console.WriteLine($"Error downloading file: {ex.Message}");
                        }

                        completedFiles++;
                        Progress = (int)((double)completedFiles / totalFiles * 100);
                    }
                }
            }

            // Notify completion
            State = RadarState.DownloadCompleted;
            Progress = 100;
            await Task.Delay(2000);
        }

        private async Task ProcessDownloadedImagesAsync()
        {
            Progress = 0;

            List<string> filesToProcess = Directory.GetFiles(Folder,
                "*.png").Where(_ => !_.Contains("_Processed")).ToList();

            // Check if there are any files to process
            if (filesToProcess.Count > 0)
            {
                for (int i = 0; i < filesToProcess.Count; i++)
                {
                    var filePath = Path.Combine(Folder, filesToProcess[i]);

                    State = RadarState.ImageProcessing;

                    // Process the downloaded image
                    RadarImagesConvertor convertor = new RadarImagesConvertor(filePath, Folder);

                    // Signal that the image has been processed
                    State = RadarState.ImageProcessed;

                    // Increment the progress based on the number of images processed
                    Progress = (int)((double)(i + 1) / filesToProcess.Count * 100);

                    // Add an await here
                    await Task.Delay(50);
                }
            }
        }
    }
}