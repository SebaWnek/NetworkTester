using System;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;

namespace Tester
{
    /// <summary>
    /// Logika interakcji dla klasy MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static string rawData;

        string username;
        string location;
        string internetPing;
        string internetUpload;
        string internetDownload;
        string pcName;
        int testCount = 0;

        string password = "Seba";

        readonly string locationIPNorth = "8.8.4.4";
        readonly string locationIPSouth = "8.8.8.8";
        readonly string locationPSTNorth = @"north\";
        readonly string locationPSTSouth = @"south\";

        string transferPath;
        double size;
        double time;
        double speed;

        string intTransferPath;
        double intSize;
        double intTime;
        double intSpeed;
        int timeout;

        string pstBaseLocation;
        string pstLocation;
        int pstFileCount;
        double pstTotalSize;
        double pstLargestSize;
        int pstSizeLimit;
        int pstOverLimit;

        Task task;

        ObservableCollection<PingListItem> pingList = new ObservableCollection<PingListItem>();

        public MainWindow()
        {
            InitializeComponent();
            pingGrid.ItemsSource = pingList;
            pingList.Add(new PingListItem("Cloudflare", "1.1.1.1"));
            pingList.Add(new PingListItem("Google DNS", "8.8.8.8"));
            pingList.Add(new PingListItem("Wrong", "0.0.0.0"));
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            username = usernameBox.Text;
            location = locationBox.Text;
            internetPing = internetPingBox.Text;
            internetUpload = internetUploadBox.Text;
            internetDownload = internetDownloadBox.Text;

            transferPath = transferPathBox.Text;
            intTransferPath = intTransferPathBox.Text;
            timeout = int.Parse(timeoutBox.Text);

            pstBaseLocation = pstBaseLocationBox.Text;
            switch (locationComboBox.SelectedIndex)
            {
                case 0:
                    pstLocation = pstBaseLocation + locationPSTSouth + username + @"\";
                    pingList.Add(new PingListItem("South", locationIPSouth));
                    break;
                case 1:
                    pstLocation = pstBaseLocation + locationPSTNorth + username + @"\";
                    pingList.Add(new PingListItem("North", locationIPNorth));
                    break;
            }
            pstLocationBox.Text = pstLocation;
            pstSizeLimit = int.Parse(pstSizeLimitBox.Text);

            pingList.Add(new PingListItem("Default Gateway", Tools.GetDefaultGateway().ToString()));

            pcName = Environment.MachineName;
            pcNameBox.Text = pcName;

            stateBox.Text = "Ready!";
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            SaveButton_Click(sender, e);

            testCount = 1 + 1 + 1 + pingList.Count;
            progressBar.Maximum = testCount;

            task = Task.Run(() =>
            {
                Dispatcher.Invoke(new Action(() =>
                {
                    startButton.IsEnabled = false;
                    saveButton.IsEnabled = false;
                    stateBox.Text = "Testing local transfer speed!";
                    progressBar.Value += 1;
                }));
                size = Tools.CheckFileSize(transferPath);
                if (size > 0)
                {
                    time = Tools.CheckTransferTime(transferPath);
                    speed = size / time;
                }
                Dispatcher.Invoke(new Action(() =>
                {
                    if (size > 0)
                    {
                        sizeBox.Text = ((int)(size / (1024 * 1024))).ToString("F");
                        timeBox.Text = time.ToString();
                        speedBox.Text = ((speed) * 8 / (1024)).ToString("F");
                    }
                    else
                    {
                        sizeBox.Text = "failed";
                        timeBox.Text = "failed";
                        speedBox.Text = "failed";
                    }
                }));

                Thread.Sleep(100);
                Dispatcher.Invoke(new Action(() =>
                {
                    stateBox.Text = "Checking PST files!";
                    progressBar.Value += 1;
                }));
                double[] pstInfo = Tools.GetPstInfo(pstLocation, pstSizeLimit * 1024 * 1024);
                if (pstInfo[0] >= 0)
                {
                    pstFileCount = (int)pstInfo[0];
                    pstTotalSize = pstInfo[1];
                    pstLargestSize = pstInfo[2];
                    pstOverLimit = (int)pstInfo[3];
                    Dispatcher.Invoke(new Action(() =>
                    {
                        pstCountBox.Text = pstFileCount.ToString();
                        pstTotalSizeBox.Text = ((int)(pstTotalSize / (1024 * 1024))).ToString("F");
                        pstLargestBox.Text = ((int)(pstLargestSize / (1024 * 1024))).ToString("F");
                        pstOverLimitBox.Text = pstOverLimit.ToString();
                    }));
                }
                else
                {
                    Dispatcher.Invoke(new Action(() =>
                    {
                        pstCountBox.Text = "failed";
                        pstFileCount = -1;
                    }));
                }

                Thread.Sleep(100);
                for (int i = 0; i < pingList.Count; i++)
                //foreach (PingListItem item in pingList)
                {
                    Dispatcher.Invoke(new Action(() =>
                    {
                        stateBox.Text = $"Pinging {i + 1} of {pingList.Count}";
                        progressBar.Value += 1;
                    }));
                    pingList[i].Ping = Tools.GetPingResult(pingList[i].Adress);
                    Thread.Sleep(100);
                }

                Thread.Sleep(100);
                Dispatcher.Invoke(new Action(() =>
                {
                    stateBox.Text = "Testing internet transfer speed!";
                    progressBar.Value += 1;
                }));
                Tools.CheckInternetTransfer(intTransferPath, timeout);


            });

        }

        public void TestsCompleted(long time, DownloadDataCompletedEventArgs e)
        {
            try
            {
                intSize = e.Result.Length;
                intTime = time;
                intSpeed = intSize / intTime;
            }
            catch (Exception)
            {
                intSize = -1;
            }

            Dispatcher.Invoke(new Action(() =>
            {
                if (intSize > 0)
                {
                    intSizeBox.Text = ((int)(intSize / (1024 * 1024))).ToString("F");
                    intTimeBox.Text = intTime.ToString();
                    intSpeedBox.Text = ((intSpeed) * 8 / (1024)).ToString("F"); 
                }
                else
                {
                    intSizeBox.Text = "failed";
                    intTimeBox.Text = "failed";
                    intSpeedBox.Text = "failed";
                }

                generateButton.IsEnabled = true;
                startButton.IsEnabled = true;
                saveButton.IsEnabled = true;
                stateBox.Text = "Finished! Generate report now!";
                MessageBox.Show("Tests completed!");
            }));
            if ((bool)moreOptions.IsChecked)
            {
                tabs.SelectedIndex = 4;
            }
        }

        private void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            string rawDataTmp = "";
            rawDataTmp += $"Username: {username}\n";
            rawDataTmp += $"Location: {location}\n";
            rawDataTmp += $"PC Name: {pcName}\n";
            rawDataTmp += $"Speed Test results: Ping {internetPing}ms, Download {internetDownload}Mbps, Upload {internetUpload}Mbps\n";
            rawDataTmp += $"Local transfer speed result: {speed *8 / (1024):F}Mbps, {size/(1024 * 1024):F}MB transfered in {time}ms\n";
            rawDataTmp += $"Internet transfer speed result: {intSpeed *8 / (1024):F}Mbps, {intSize/(1024 * 1024):F}MB transfered in {intTime}ms\n";
            rawDataTmp += "Ping results:\n";
            foreach (PingListItem item in pingList)
            {
                if (item.Ping != "failed")
                {
                    rawDataTmp += $"{item.Name} - {item.Adress}: {item.Ping}ms\n";
                }
                else
                {
                    rawDataTmp += $"{item.Name} - {item.Adress}: failed\n";
                }
            }
            if (pstFileCount >= 0)
            {
                rawDataTmp += $"Pst files count: {pstFileCount}, total size: {((int)pstTotalSize / (1024 * 1024))}MB, largest file: {((int)pstLargestSize / (1024 * 1024))}MB\n" +
                        $"{pstOverLimit} file(s) over {pstSizeLimit}MB limit\n\n";
            }
            else
            {
                rawDataTmp += "Pst check failed\n\n";
            }
            rawData = rawDataTmp + rawData;
            rawDataBox.Text = rawData;
            tabs.SelectedIndex = 5;
            stateBox.Text = "Report generated!";
        }

        private void TaskmgrButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("taskmgr.exe");
            rawData += "Checked taskmgr.exe\n";
        }

        private void CmdButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var psi = new ProcessStartInfo();
                psi.Domain = cmdDomainBox.Text;
                psi.UserName = cmdUserBox.Text;
                psi.Password = cmdPassBox.SecurePassword;
                psi.FileName = "cmd.exe";
                psi.UseShellExecute = false;
                Process.Start(psi);
            }
            catch (Exception)
            {
                MessageBox.Show("Cannot start cmd as different user, start manually!");
            }
        }

        private void ChromeButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("chrome.exe");
            rawData += "Cleared chrome cache and cookies\n";
        }

        private void IeButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("inetcpl.cpl");
            rawData += "Cleared Internet Explorer cache and cookies\n";
        }

        private void JavaButton_Click(object sender, RoutedEventArgs e)
        {
            rawData += "Cleared Java cache\n";
            try
            {
                Process.Start("javacpl.exe");
            }
            catch (Exception)
            {
                MessageBox.Show("Cannot start Java settings, open manually in Control Panel!");
            }
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(rawData);
            stateBox.Text = "Report copied to clipboard!";
        }

        private void TempButton_Click(object sender, RoutedEventArgs e)
        {
            string tempPath = System.IO.Path.GetTempPath();
            Process.Start(tempPath);
            rawData += "Cleared temp folder";
        }

        private void PasswordButton_Click(object sender, RoutedEventArgs e)
        {
            if(passwordBox.Text == password)
            {
                pcNameBox.IsEnabled = true;
                transferPathBox.IsEnabled = true;
                sizeBox.IsEnabled = true;
                timeBox.IsEnabled = true;
                speedBox.IsEnabled = true;
                intTransferPathBox.IsEnabled = true;
                intSizeBox.IsEnabled = true;
                intTimeBox.IsEnabled = true;
                intSpeedBox.IsEnabled = true;
                pstBaseLocationBox.IsEnabled = true;
                pstLocationBox.IsEnabled = true;
                pstCountBox.IsEnabled = true;
                pstTotalSizeBox.IsEnabled = true;
                pstLargestBox.IsEnabled = true;
                pstSizeLimitBox.IsEnabled = true;
                pstOverLimitBox.IsEnabled = true;
                timeoutBox.IsEnabled = true;
                cmdDomainBox.IsEnabled = true;
            }
        }

        public static void DownloadProgressCallback(object sender, DownloadProgressChangedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                ((MainWindow)Application.Current.MainWindow).dlProgressBar.Value = e.ProgressPercentage;
            });
        }
    }

    public class PingListItem : INotifyPropertyChanged
    {
        public string Name { get; set; }
        public string Adress { get; set; }
        private string ping;

        public string Ping
        {
            get { return ping; }
            set
            {
                ping = value;
                NotifyPropertyChanged("Ping");
            }
        }

        public PingListItem()
        {
        }
        public PingListItem(string adress)
        {
            Adress = adress;
        }
        public PingListItem(string name, string adress)
        {
            Name = name;
            Adress = adress;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

    }
}
