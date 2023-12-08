using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Weather_Images
{
    public partial class Form1 : Form
    {
        public enum RadarState
        {
            Waiting,
            Initializing,
            Downloading,
            DownloadCompleted,
            ImageProcessing,
            ImageProcessed,
        }

#if false && DEBUG
        TimeSpan UpdateInterval { get; } = TimeSpan.FromSeconds(10);
#else
        TimeSpan UpdateInterval { get; } = TimeSpan.FromMinutes(5);
#endif

        readonly ProgressBar _downloadProgress;
        DateTime _nextDownloadTime = DateTime.Now;
        string folderToSaveImage;
        Radar _radar;
        int successfulDownloadsCount = 0;
        private bool isImageProcessed = false;

        public Form1()
        {
            InitializeComponent();

            _downloadProgress = new ProgressBar
            {
                Location = lblNextTimeDownload.Location,
                Size = lblNextTimeDownload.Size,
                Dock = DockStyle.Top,
                Value = 0,
            };

            lblImagesCounter.Text = "Images Counter: 0";
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            //lblNextTimeDownload.Visible = false;
            Controls.Add(_downloadProgress);
        }

        private async Task updateLabelAsync()
        {
            while (!Disposing)
            {
                if (_radar.State == RadarState.Waiting)
                {
                    TimeSpan countdown = _nextDownloadTime - DateTime.Now + TimeSpan.FromSeconds(0.99);
                    if (countdown <= TimeSpan.FromSeconds(0.99))
                    {
                        lblNextTimeDownload.Visible = false;
                        _downloadProgress.Visible = true;

                        // Check if it's time to start a new download
                        if (DateTime.Now >= _nextDownloadTime)
                        {
                            // Initiate a new download
                            await _radar.ExececuteAsync();
                            Debug.Assert(_radar.State == RadarState.Waiting, "Expecting Radar to reset its state.");

                            // Set the next download time
                            _nextDownloadTime = DateTime.Now + UpdateInterval;
                        }
                    }
                    else
                    {
                        lblNextTimeDownload.Visible = true;
                        _downloadProgress.Visible = false;
                        lblNextTimeDownload.Text = "Next download in: " + countdown.ToString(@"mm\:ss");
                        await Task.Delay(500);
                    }
                }
            }
        }

        private void StartDownload()
        {
            _radar.PropertyChanged += (sender, args) =>
            {
                switch (args.PropertyName)
                {
                    case nameof(_radar.State):
                        // Update the title bar
                        BeginInvoke((MethodInvoker)delegate
                        {
                            if (_radar.State == RadarState.ImageProcessed)
                            {
                                // Text = $"Radar - {_radar.State}";
                                successfulDownloadsCount++;  // Increment the counter
                                lblImagesCounter.Text = $"Images Count: {successfulDownloadsCount}";  // Update the label
                            }
                            else if (_radar.State == RadarState.ImageProcessing)
                            {
                                Text = "Radar - ImagesProcessing";
                            }
                            else
                            {
                                Text = $"Radar - {_radar.State}";
                            }
                        });
                        break;
                    case nameof(_radar.Progress):
                        // Update the progress bar
                        if (!Disposing) BeginInvoke((MethodInvoker)delegate
                        {
                            _downloadProgress.Value = _radar.Progress;
                        });
                        break;
                }
            };
            // Start timer
            Task task = updateLabelAsync();
            Disposed += async (sender, eventArgs) =>
            {
                await task;
                task.Dispose();
            };
        }

        private void btnDownload_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
            {
                // Set the initial folder (optional)
                folderDialog.SelectedPath = @"C:\";

                // Show the dialog and capture the result
                DialogResult result = folderDialog.ShowDialog();

                // Check if the user selected a folder
                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(folderDialog.SelectedPath))
                {
                    btnDownload.Enabled = false;

                    // Use the selected folder path
                    string selectedFolder = folderDialog.SelectedPath;

                    // Now you can perform actions with the selected folder path
                    // For example, display it in a TextBox or perform further processing
                    //textBoxFolderPath.Text = selectedFolder;

                    folderToSaveImage = selectedFolder;
                    textBoxDownloadFolder.Text = selectedFolder;
                    _radar = new Radar(folderToSaveImage);

                    StartDownload();
                }
            }
        }
    }
}