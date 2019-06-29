using System;
using System.Linq;
using System.IO;
using System.Windows;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.ComponentModel;
using System.Timers;

namespace Tester
{
    public static class Tools
    {
        public static double CheckFileSize(string path)
        {
            try
            {
                return new FileInfo(path).Length;

            }
            catch (Exception)
            {
                return -1;
            }
        }

        public static double CheckTransferTime(string path)
        {
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                ((MainWindow)Application.Current.MainWindow).dlProgressBar.Value = 0;
                ((MainWindow)Application.Current.MainWindow).dlProgressBar.Maximum = 100;
            }));

            Stopwatch stopWatch = new Stopwatch();
            int fileSize = (int)(new FileInfo(path).Length);
            int fileChunk = fileSize / 100;
            byte[] buffer = new byte[fileSize];
            using(FileStream sr = new FileStream(path, FileMode.Open))
            {
                stopWatch.Start();
                for (int i = 0; i < 100; i++)
                {
                    sr.Read(buffer, fileChunk * i, fileChunk);
                    Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        ((MainWindow)Application.Current.MainWindow).dlProgressBar.Value++;
                    }));
                }
                stopWatch.Stop();
            }
            double time = stopWatch.ElapsedMilliseconds;
            return time;
        }

        public static void CheckInternetTransfer(string path, int timeout)
        {
            WebClient webClient;


            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                ((MainWindow)Application.Current.MainWindow).dlProgressBar.Value = 0;
                ((MainWindow)Application.Current.MainWindow).dlProgressBar.Maximum = 100;
            }));
            byte[] data = { 0 };
            Stopwatch stopWatch = new Stopwatch();
            using (webClient = new WebClient())
            {
                Timer timer = new Timer(timeout * 1000);
                timer.Elapsed += ((s, e) =>
                {
                    webClient.CancelAsync();
                });
                timer.Enabled = true;

                webClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(MainWindow.DownloadProgressCallback);
                webClient.DownloadDataCompleted += (sender, e) =>
                {
                    stopWatch.Stop();
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ((MainWindow)Application.Current.MainWindow).TestsCompleted(stopWatch.ElapsedMilliseconds, e);
                    });
                };
                   
                stopWatch.Start();
                webClient.DownloadDataAsync(new Uri(path));
            }
        }

        public static double[] GetPstInfo(string path, int limit)
        {
            try
            {
                double filesCount = 0;
                double filesSize = 0;
                double biggestSize = 0;
                double overLimitCount = 0;
                string[] files = Directory.GetFiles(path);
                string[] folders = Directory.GetDirectories(path);
                foreach (string file in files)
                {
                    if (file.Contains(".pst"))
                    {
                        filesCount++;
                        long currentFileSize = new FileInfo(file).Length;
                        filesSize += currentFileSize;
                        if (currentFileSize > biggestSize)
                        {
                            biggestSize = currentFileSize;
                        }
                        if (currentFileSize > limit)
                        {
                            overLimitCount++;
                        }
                    }
                }

                foreach (string folder in folders)
                {
                    double[] directoryInfo = GetPstInfo(folder, limit);
                    filesCount += directoryInfo[0];
                    filesSize += directoryInfo[1];
                    if (directoryInfo[2] > biggestSize)
                    {
                        biggestSize = directoryInfo[2];
                    }
                    overLimitCount += directoryInfo[3];
                }

                return new double[] { filesCount, filesSize, biggestSize, overLimitCount };
            }
            catch (Exception)
            {
                return new double[] { -1 };
            }
        }

        public static IPAddress GetDefaultGateway()
        {
            return NetworkInterface
                .GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up)
                .Where(n => n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .SelectMany(n => n.GetIPProperties()?.GatewayAddresses)
                .Select(g => g?.Address)
                .Where(a => a != null)
                // .Where(a => a.AddressFamily == AddressFamily.InterNetwork)
                // .Where(a => Array.FindIndex(a.GetAddressBytes(), b => b != 0) >= 0)
                .FirstOrDefault();
        }

        public static string GetPingResult(string ip)
        {
            MainWindow.rawData += $"Pinging {ip}:\n";

            int pingSum = 0;
            int pingCount = 10;
            bool isSuccess = false;
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                ((MainWindow)Application.Current.MainWindow).dlProgressBar.Value = 0;
                ((MainWindow)Application.Current.MainWindow).dlProgressBar.Maximum = pingCount;
            }));

            Ping ping = new Ping();
            PingReply reply;

            for(int i = 0; i < pingCount; i++)
            {
                Application.Current.Dispatcher.Invoke(new Action(() =>
                {
                    ((MainWindow)Application.Current.MainWindow).dlProgressBar.Value++;
                }));
                try
                {
                    reply = ping.Send(ip, 1000);
                }
                catch (Exception)
                {
                    reply = null;
                }
                if (reply != null && reply.Status == IPStatus.Success)
                {
                    pingSum += (int)reply.RoundtripTime;
                    MainWindow.rawData += reply.RoundtripTime.ToString() + "\n";
                    isSuccess = true;
                }
                else if (reply == null)
                {
                    MainWindow.rawData += "null\n";
                }
                else
                {
                    MainWindow.rawData += reply.Status.ToString() + "\n";
                }
            }
            MainWindow.rawData += "\n";
            if (isSuccess)
            {
                return (pingSum / pingCount).ToString(); 
            }
            else
            {
                return "failed";
            }
        }
    }
}
