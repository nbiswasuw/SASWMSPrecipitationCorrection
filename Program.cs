using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using WinSCP;

namespace PrecipitationCorrection
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("Realtime and Automatic IMERG Satellite Estimated Precipitation Correction System of SASWE Research Group");
            Console.WriteLine("Scripts developed by Nishan Kumar Biswas, contact: nbiswas@uw.edu, nishan.wre.buet@gmail.com");
            Console.WriteLine("----------------------------------------------------------");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Parameters and variables are initiating....");
            StringBuilder logText = new StringBuilder();
            StringBuilder rainfallData = new StringBuilder();
            StringBuilder waterLevelData = new StringBuilder();
            DirectoryInfo iniDi = new DirectoryInfo(@"C:\Users\nbiswas\Desktop\Nishan\SASWE\AutoCorrection");
            string[] correctionDate = File.ReadAllLines(iniDi + @"\Programs\ExecutionFiles\Correction_Date.txt");
            DateTime today = DateTime.ParseExact(correctionDate[0], "yyyy-MM-dd", CultureInfo.InvariantCulture);
            //DateTime today = new DateTime(2016, 10, 12, 0, 0, 0);
            string plainDate = today.ToString("yyyyMMdd");
            string hiphenDate = today.ToString("yyyy-MM-dd");
            string preplainDate = today.AddDays(-1).ToString("yyyyMMdd");
            string prehiphenDate = today.AddDays(-1).ToString("yyyy-MM-dd");
            Directory.CreateDirectory(iniDi + @"\RawRainfall\WebData_" + hiphenDate);
            DirectoryInfo di = new DirectoryInfo(iniDi + @"\RawRainfall\WebData_" + hiphenDate);

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Realtime Precipitation Correction started for Date: " + hiphenDate);
            Console.ResetColor();
            logText.AppendLine("Precipitation Correction started for Date: " + hiphenDate + ", started at " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));


            ////-------------------------------------- Rainfall Data Correcting to remove illegal characters -- ----------------------------------------------------------------
            try
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                logText.AppendLine("IMERG Satellite estimated rainfall Data correcting started...");
                Console.WriteLine("IMERG Satellite estimated rainfall Data correction started...");
                Console.ResetColor();

                string[] rainfallFIle = File.ReadAllLines(di + @"\Rainfall_" + hiphenDate + ".txt");
                DateTime[] downloadDate = new DateTime[rainfallFIle.Length];
                string[] downloadStation = new string[rainfallFIle.Length];
                string[] downloadRain = new string[rainfallFIle.Length];

                int falseChar = 0;
                int invalidChar = 0;

                for (int i = 0; i < rainfallFIle.Length; i++)
                {
                    try
                    {
                        var dispersedText = rainfallFIle[i].Split(',');
                        downloadDate[i] = DateTime.ParseExact(dispersedText[0], "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                        downloadStation[i] = dispersedText[1];
                        dispersedText[2] = dispersedText[2].Trim(new Char[] { 'R', 'F', '-', '*', '.' });
                        if (dispersedText[2].Trim() == "999" || dispersedText[2].Trim() == "9999" || dispersedText[2].Trim() == "999.99" || dispersedText[2].Trim() == "N/A" || dispersedText[2] == "*" || dispersedText[2].Trim() == "**" || dispersedText[2].Trim() == "NA" || dispersedText[2].Trim() == "")
                        {
                            downloadRain[i] = "-";
                            falseChar = falseChar + 1;
                        }
                        else if (dispersedText[2].Trim() == "NIL" || dispersedText[2].Trim() == "NILL" || dispersedText[2].Trim() == "N")
                        {
                            downloadRain[i] = "0";
                            invalidChar = invalidChar + 1;
                        }
                        else if (dispersedText[2].Trim() == "TRACE" || dispersedText[2].Trim() == "Traces" || dispersedText[2].Trim() == "TR" || dispersedText[2].Trim() == "TR." || dispersedText[2].Trim() == "T")
                        {
                            downloadRain[i] = "0.01";
                            invalidChar = invalidChar + 1;
                        }
                        else
                        {
                            downloadRain[i] = dispersedText[2];
                        }
                    }
                    catch (Exception)
                    {
                        continue;
                    }

                }

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(invalidChar + " value replaced with exact value from collected rainfall data.");
                Console.WriteLine(falseChar + " value removed from collected rainfall data.");
                logText.AppendLine(invalidChar + " value replaced with exact value from collected rainfall data.");
                logText.AppendLine(falseChar + " value removed from collected rainfall data.");
                Console.ResetColor();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("Today rainfall over GBM Basin's CSV file is creating ....");
                logText.AppendLine("Today rainfall over GBM Basin's CSV file is creating ....");
                Console.ResetColor();
                string[] stationInfo = File.ReadAllLines(iniDi + @"\NecessaryFiles\RainfallStationCoordinate.csv");
                string[] stationName = new string[stationInfo.Length - 1];
                float[] stationLat = new float[stationInfo.Length - 1];
                float[] stationLon = new float[stationInfo.Length - 1];
                for (int i = 1; i < stationInfo.Length; i++)
                {
                    var dispersedText = stationInfo[i].Split(',');
                    stationName[i - 1] = dispersedText[0];
                    stationLat[i - 1] = float.Parse(dispersedText[1]);
                    stationLon[i - 1] = float.Parse(dispersedText[2]);
                }

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("lat,lon,rf");

                int counter = 0;
                for (int i = 0; i < rainfallFIle.Length; i++)
                {
                    if (downloadDate[i] != null || downloadStation[i] != null || downloadRain != null)
                    {
                        if (downloadDate[i].ToString("yyyy-MM-dd") == hiphenDate && downloadRain[i] != "-")
                        {
                            try
                            {
                                float rainamount = float.Parse(downloadRain[i]);
                                for (int j = 0; j < stationName.Length; j++)
                                {
                                    if (String.Equals(downloadStation[i], stationName[j], StringComparison.CurrentCultureIgnoreCase))
                                    {
                                        sb.AppendLine(stationLat[j].ToString("00.00") + "," + stationLon[j].ToString("00.00") + "," + rainamount.ToString("0.00"));
                                        counter = counter + 1;
                                    }
                                }
                            }
                            catch (FormatException)
                            {
                                continue;
                            }
                        }
                    }
                }
                File.WriteAllText(iniDi + @"\ProcessedRainfall\Obs_RF_" + prehiphenDate + ".txt", sb.ToString());
                sb.Clear();

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Rainfall CSV file successfully created. No. of stations found: " + counter);
                logText.AppendLine("Rainfall CSV file successfully created. No. of stations found: " + counter);
                Console.ResetColor();
            }
            catch (Exception error)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Rainfall data cannot be corrected or csv file cannot be created. Please check the raw file. Error: " + error.Message);
                logText.AppendLine("Rainfall data cannot be corrected or csv file cannot be created. Please check the raw file. Error: " + error.Message);
                Console.ResetColor();
            }

            try
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                logText.AppendLine("Rainfall data is dividing according to the Basins ...");
                Console.WriteLine("Rainfall data is dividing according to the Basins ...");
                Console.ResetColor();

                string rfDate = today.AddDays(-1).ToString("yyyy-MM-dd");
                float[] brahmaBBox = { 82.0f, 23.75f, 97.75f, 31.5f };
                float[] gangesBBox = { 73.375f, 22.50f, 89.0f, 31.50f };
                float[] indusBBox = { 66.20f, 25.30f, 82.30f, 37.00f };
                float[] pakistanBBox = { 60.90f, 23.70f, 77.90f, 37.20f };


                string[] obsRfFile = File.ReadAllLines(iniDi + @"\ProcessedRainfall\Obs_RF_" + rfDate + ".txt");
                float[] obsLat = new float[obsRfFile.Length - 1];
                float[] obsLon = new float[obsRfFile.Length - 1];
                float[] obsRF = new float[obsRfFile.Length - 1];
                for (int i = 0; i < obsRfFile.Length - 1; i++)
                {
                    var textParser = obsRfFile[i + 1].Split(',');
                    obsLat[i] = float.Parse(textParser[0]);
                    obsLon[i] = float.Parse(textParser[1]);
                    obsRF[i] = float.Parse(textParser[2]);
                }

                StringBuilder gangesRF = new StringBuilder();
                List<float> gangesRainfall = new List<float>();
                gangesRF.AppendLine("lat,lon,rf");
                StringBuilder brahmaRF = new StringBuilder();
                List<float> brahmaRainfall = new List<float>();
                brahmaRF.AppendLine("lat,lon,rf");
                StringBuilder indusRF = new StringBuilder();
                List<float> indusRainfall = new List<float>();
                indusRF.AppendLine("lat,lon,rf");

                StringBuilder pakistanRF = new StringBuilder();
                List<float> pakistanRainfall = new List<float>();
                pakistanRF.AppendLine("lat,lon,rf");

                for (int i = 0; i < obsRF.Length; i++)
                {
                    if (obsLon[i] >= gangesBBox[0] && obsLon[i] <= gangesBBox[2] && obsLat[i] >= gangesBBox[1] && obsLat[i] <= gangesBBox[3])
                    {
                        gangesRainfall.Add(obsRF[i]);
                        gangesRF.AppendLine(obsLat[i].ToString("0.00") + "," + obsLon[i].ToString("0.00") + "," + obsRF[i].ToString("0.00"));
                    }
                    if (obsLon[i] >= brahmaBBox[0] && obsLon[i] <= brahmaBBox[2] && obsLat[i] >= brahmaBBox[1] && obsLat[i] <= brahmaBBox[3])
                    {
                        brahmaRainfall.Add(obsRF[i]);
                        brahmaRF.AppendLine(obsLat[i].ToString("0.00") + "," + obsLon[i].ToString("0.00") + "," + obsRF[i].ToString("0.00"));
                    }
                    if (obsLon[i] >= indusBBox[0] && obsLon[i] <= indusBBox[2] && obsLat[i] >= indusBBox[1] && obsLat[i] <= indusBBox[3])
                    {
                        indusRainfall.Add(obsRF[i]);
                        indusRF.AppendLine(obsLat[i].ToString("0.00") + "," + obsLon[i].ToString("0.00") + "," + obsRF[i].ToString("0.00"));
                    }

                    if (obsLon[i] >= pakistanBBox[0] && obsLon[i] <= pakistanBBox[2] && obsLat[i] >= pakistanBBox[1] && obsLat[i] <= pakistanBBox[3])
                    {
                        pakistanRainfall.Add(obsRF[i]);
                        pakistanRF.AppendLine(obsLat[i].ToString("0.00") + "," + obsLon[i].ToString("0.00") + "," + obsRF[i].ToString("0.00"));
                    }
                }

                File.WriteAllText(iniDi + @"\BasinRainfall\" + preplainDate + ".precip.observed.brahmaputra.txt", brahmaRF.ToString());
                File.WriteAllText(iniDi + @"\BasinRainfall\" + preplainDate + ".precip.observed.ganges.txt", gangesRF.ToString());
                File.WriteAllText(iniDi + @"\BasinRainfall\" + preplainDate + ".precip.observed.indus1.txt", indusRF.ToString());
                File.WriteAllText(iniDi + @"\BasinRainfall\" + preplainDate + ".precip.observed.pakistan.txt", pakistanRF.ToString());

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Rainfall file for Ganges Basin successfully created. No. of stations found: " + gangesRainfall.Count);
                logText.AppendLine("Rainfall file for Ganges Basin successfully created. No. of stations found: " + gangesRainfall.Count);
                Console.WriteLine("Maximum, Average and Minimum observed Rainfall in mm over Ganges Basin: " + gangesRainfall.Max().ToString("0.00") + ", " + gangesRainfall.Average().ToString("0.00") + " and " + gangesRainfall.Min().ToString("0.00"));
                logText.AppendLine("Maximum, Average and Minimum observed Rainfall in mm over Ganges Basin: " + gangesRainfall.Max().ToString("0.00") + ", " + gangesRainfall.Average().ToString("0.00") + " and " + gangesRainfall.Min().ToString("0.00"));

                Console.WriteLine("Rainfall file for Brahmaputra Basin successfully created. No. of stations found: " + brahmaRainfall.Count);
                logText.AppendLine("Rainfall file for Brahmaputra Basin successfully created. No. of stations found: " + brahmaRainfall.Count);
                Console.WriteLine("Maximum, Average and Minimum observed Rainfall in mm over Brahmaputra Basin: " + brahmaRainfall.Max().ToString("0.00") + ", " + brahmaRainfall.Average().ToString("0.00") + " and " + brahmaRainfall.Min().ToString("0.00"));
                logText.AppendLine("Maximum, Average and Minimum observed Rainfall in mm over Brahmaputra Basin: " + brahmaRainfall.Max().ToString("0.00") + ", " + brahmaRainfall.Average().ToString("0.00") + " and " + brahmaRainfall.Min().ToString("0.00"));

                Console.WriteLine("Rainfall file for Indus Basin successfully created. No. of stations found: " + indusRainfall.Count);
                logText.AppendLine("Rainfall file for Indus Basin successfully created. No. of stations found: " + indusRainfall.Count);
                Console.WriteLine("Maximum, Average and Minimum observed Rainfall in mm over Indus Basin: " + indusRainfall.Max().ToString("0.00") + ", " + indusRainfall.Average().ToString("0.00") + " and " + indusRainfall.Min().ToString("0.00"));
                logText.AppendLine("Maximum, Average and Minimum observed Rainfall in mm over Indus Basin: " + indusRainfall.Max().ToString("0.00") + ", " + indusRainfall.Average().ToString("0.00") + " and " + indusRainfall.Min().ToString("0.00"));

                Console.WriteLine("Rainfall file for Pakistan successfully created. No. of stations found: " + pakistanRainfall.Count);
                logText.AppendLine("Rainfall file for Pakistan successfully created. No. of stations found: " + pakistanRainfall.Count);
                Console.WriteLine("Maximum, Average and Minimum observed Rainfall in mm over Pakistan: " + pakistanRainfall.Max().ToString("0.00") + ", " + pakistanRainfall.Average().ToString("0.00") + " and " + pakistanRainfall.Min().ToString("0.00"));
                logText.AppendLine("Maximum, Average and Minimum observed Rainfall in mm over Pakistan: " + pakistanRainfall.Max().ToString("0.00") + ", " + pakistanRainfall.Average().ToString("0.00") + " and " + pakistanRainfall.Min().ToString("0.00"));
                Console.ResetColor();

                gangesRF.Clear();
                brahmaRF.Clear();
                indusRF.Clear();
                gangesRainfall.Clear();
                brahmaRainfall.Clear();
                indusRainfall.Clear();
                pakistanRainfall.Clear();
            }
            catch (Exception error)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Collected Rainfall data cannot be separated into Ganges, Brahmaputra, Indus Basin and Pakistan. Please check the Processed Rainfall file. Error: " + error.Message);
                logText.AppendLine("Collected Rainfall data cannot be separated into Ganges, Brahmaputra, Indus Basin and Pakistan. Please check the Processed Rainfall file. Error: " + error.Message);
                Console.ResetColor();
            }
            try
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("Windows PC is now going to copy IMERG precipitation files from SASWMS Server ....");
                logText.AppendLine("Windows PC is now going to copy IMERG precipitation files from  SASWMS Server ....");
                Console.ResetColor();

                Console.ForegroundColor = ConsoleColor.Green;
                string[] basinName = new string[] { "brahmaputra", "ganges", "indus1"};
                foreach (string element in basinName)
                {
                    try
                    {
                        string dailyDataPath = iniDi + @"\BasinRainfall\" + preplainDate + @".precip.IMERGRT." + element + ".txt";
                        WebClient wb = new WebClient();
                        wb.Credentials = new NetworkCredential("saswms", "S@swe2015");
                        wb.DownloadFile("http://128.95.45.89/repository/" + preplainDate + @".precip.IMERGRT." + element + ".txt", dailyDataPath);
                        Console.WriteLine(dailyDataPath);
                    }
                    catch (WebException e)
                    {

                        string status = ((FtpWebResponse)e.Response).StatusDescription;
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Server Error: " + status);
                    }
                }
                Console.WriteLine("IMERG precipitation files successfully downloaded from SASWMS Server.");
                logText.AppendLine("IMERG precipitation files successfully downloaded from SASWMS Server.");
                Console.ResetColor();

                Console.ForegroundColor = ConsoleColor.Green;
                float[] statsGanges = statsASCII(iniDi + @"\BasinRainfall\" + preplainDate + @".precip.IMERGRT.ganges.txt");
                Console.WriteLine("Maximum, Average and Minimum IMERGRT estimated Rainfall in mm over Ganges Basin: " + statsGanges[0].ToString("0.00") + ", " + statsGanges[1].ToString("0.00") + " and " + statsGanges[2].ToString("0.00"));
                logText.AppendLine("Maximum, Average and Minimum IMERGRT estimated Rainfall in mm over Ganges Basin: " + statsGanges[0].ToString("0.00") + ", " + statsGanges[1].ToString("0.00") + " and " + statsGanges[2].ToString("0.00"));

                float[] statsBrahma = statsASCII(iniDi + @"\BasinRainfall\" + preplainDate + @".precip.IMERGRT.brahmaputra.txt");
                Console.WriteLine("Maximum, Average and Minimum IMERGRT estimated Rainfall in mm over Brahmaputra Basin: " + statsBrahma[0].ToString("0.00") + ", " + statsBrahma[1].ToString("0.00") + " and " + statsBrahma[2].ToString("0.00"));
                logText.AppendLine("Maximum, Average and Minimum IMERGRT estimated Rainfall in mm over Brahmaputra Basin: " + statsBrahma[0].ToString("0.00") + ", " + statsBrahma[1].ToString("0.00") + " and " + statsBrahma[2].ToString("0.00"));

                float[] statsIndus = statsASCII(iniDi + @"\BasinRainfall\" + preplainDate + @".precip.IMERGRT.indus1.txt");
                Console.WriteLine("Maximum, Average and Minimum IMERGRT estimated Rainfall in mm over Indus Basin: " + statsIndus[0].ToString("0.00") + ", " + statsIndus[1].ToString("0.00") + " and " + statsIndus[2].ToString("0.00"));
                logText.AppendLine("Maximum, Average and Minimum IMERGRT estimated Rainfall in mm over Indus Basin: " + statsIndus[0].ToString("0.00") + ", " + statsIndus[1].ToString("0.00") + " and " + statsIndus[2].ToString("0.00"));

                float[] statsPakistan = statsASCII(iniDi + @"\BasinRainfall\" + preplainDate + @".precip.IMERGRT.pakistan.txt");
                Console.WriteLine("Maximum, Average and Minimum IMERGRT estimated Rainfall in mm over Pakistan: " + statsPakistan[0].ToString("0.00") + ", " + statsPakistan[1].ToString("0.00") + " and " + statsPakistan[2].ToString("0.00"));
                logText.AppendLine("Maximum, Average and Minimum IMERGRT estimated Rainfall in mm over Pakistan: " + statsPakistan[0].ToString("0.00") + ", " + statsPakistan[1].ToString("0.00") + " and " + statsPakistan[2].ToString("0.00"));
                Console.ResetColor();
            }
            catch (Exception error)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("IMERG Precipitation data cannot be copied from SASWMS Server. Please check internet connectivity of Computer. Error: " + error.Message);
                logText.AppendLine("IMERG Precipitation data cannot be copied from SASWMS Server. Please check internet connectivity of Computer. Error: " + error.Message);
                Console.ResetColor();
            }

            /*try
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("Windows PC is now going to copy IMERG precipitation files from SASWMS Server ....");
                logText.AppendLine("Windows PC is now going to copy IMERG precipitation files from  SASWMS Server ....");
                Console.ResetColor();

                SessionOptions sessionOptions = new SessionOptions();
                sessionOptions.Protocol = Protocol.Sftp;
                sessionOptions.HostName = "saswe@ovid.u.washington.edu";
                sessionOptions.UserName = "saswe";
                sessionOptions.Password = "Bangla_Power21Feb";
                sessionOptions.SshHostKeyFingerprint = "ssh-rsa 1024 22:c7:83:db:1c:c5:7d:10:15:23:f5:64:36:af:b5:b2";

                Session session = new Session();
                session.Open(sessionOptions);
                TransferOptions transferOptions = new TransferOptions();
                transferOptions.TransferMode = TransferMode.Binary;
                TransferOperationResult transferResult;

                string[] basinName = new string[] { "brahmaputra", "ganges", "indus1", "pakistan" };
                foreach (string element in basinName)
                {
                    string dailyData = @"Repository/" + preplainDate + @".precip.IMERGRT." + element + ".txt";
                    string dailyDataPath = iniDi + @"\BasinRainfall\" + preplainDate + @".precip.IMERGRT." + element + ".txt";
                    transferResult = session.GetFiles(dailyData, dailyDataPath, false, transferOptions);
                    Console.WriteLine(transferResult.Transfers[0].FileName);
                }

                Console.WriteLine("IMERG precipitation files successfully downloaded from SASWMS Server.");
                logText.AppendLine("IMERG precipitation files successfully downloaded from SASWMS Server..");
                Console.ResetColor();

                Console.ForegroundColor = ConsoleColor.Green;
                float[] statsGanges = statsASCII(iniDi + @"\BasinRainfall\" + preplainDate + @".precip.IMERGRT.ganges.txt");
                Console.WriteLine("Maximum, Average and Minimum IMERGRT estimated Rainfall in mm over Ganges Basin: " + statsGanges[0].ToString("0.00") + ", " + statsGanges[1].ToString("0.00") + " and " + statsGanges[2].ToString("0.00"));
                logText.AppendLine("Maximum, Average and Minimum IMERGRT estimated Rainfall in mm over Ganges Basin: " + statsGanges[0].ToString("0.00") + ", " + statsGanges[1].ToString("0.00") + " and " + statsGanges[2].ToString("0.00"));

                float[] statsBrahma = statsASCII(iniDi + @"\BasinRainfall\" + preplainDate + @".precip.IMERGRT.brahmaputra.txt");
                Console.WriteLine("Maximum, Average and Minimum IMERGRT estimated Rainfall in mm over Brahmaputra Basin: " + statsBrahma[0].ToString("0.00") + ", " + statsBrahma[1].ToString("0.00") + " and " + statsBrahma[2].ToString("0.00"));
                logText.AppendLine("Maximum, Average and Minimum IMERGRT estimated Rainfall in mm over Brahmaputra Basin: " + statsBrahma[0].ToString("0.00") + ", " + statsBrahma[1].ToString("0.00") + " and " + statsBrahma[2].ToString("0.00"));

                float[] statsIndus = statsASCII(iniDi + @"\BasinRainfall\" + preplainDate + @".precip.IMERGRT.indus1.txt");
                Console.WriteLine("Maximum, Average and Minimum IMERGRT estimated Rainfall in mm over Indus Basin: " + statsIndus[0].ToString("0.00") + ", " + statsIndus[1].ToString("0.00") + " and " + statsIndus[2].ToString("0.00"));
                logText.AppendLine("Maximum, Average and Minimum IMERGRT estimated Rainfall in mm over Indus Basin: " + statsIndus[0].ToString("0.00") + ", " + statsIndus[1].ToString("0.00") + " and " + statsIndus[2].ToString("0.00"));

                float[] statsPakistan = statsASCII(iniDi + @"\BasinRainfall\" + preplainDate + @".precip.IMERGRT.pakistan.txt");
                Console.WriteLine("Maximum, Average and Minimum IMERGRT estimated Rainfall in mm over Pakistan: " + statsPakistan[0].ToString("0.00") + ", " + statsPakistan[1].ToString("0.00") + " and " + statsPakistan[2].ToString("0.00"));
                logText.AppendLine("Maximum, Average and Minimum IMERGRT estimated Rainfall in mm over Pakistan: " + statsPakistan[0].ToString("0.00") + ", " + statsPakistan[1].ToString("0.00") + " and " + statsPakistan[2].ToString("0.00"));
                Console.ResetColor();
            }
            catch (Exception error)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("IMERG Precipitation data cannot be copied from SASWMS Server. Please check internet connectivity of Computer. Error: " + error.Message);
                logText.AppendLine("IMERG Precipitation data cannot be copied from SASWMS Server. Please check internet connectivity of Computer. Error: " + error.Message);
                Console.ResetColor();
            }*/

            try
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("Applying Bias Correction to the IMERG Precipitation using python and ArcGIS....");
                logText.AppendLine("Applying Bias Correction to the IMERG Precipitation using python and ArcGIS....");
                Console.ResetColor();

                Process pythonBiasCorrection = new Process();
                pythonBiasCorrection = Process.Start(iniDi + @"\Programs\PythonScripts\CombinedRainfallCorrection.py");
                pythonBiasCorrection.WaitForExit();

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Python successfully initialized and hopefully adjusted bias. To check the result please see the GIS folder.");
                logText.AppendLine("Python successfully initialized and hopefully adjusted bias. To check the result please see the GIS folder.");
                Console.ResetColor();
            }
            catch (Exception error)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Python cannot be initialized to apply Bias Correction. Please check the .py file path  and it's contents. Error: " + error.Message);
                logText.AppendLine("Python cannot be initialized to apply Bias Correction. Please check the .py file path and it's contents. Error: " + error.Message);
                Console.ResetColor();
            }

            ///------------------------------------------------------------  Corrected Precipitation 
            try
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("Windows PC is trying to send corrected precipitation to SASWMS Server....");
                logText.AppendLine("Windows PC is trying to send corrected precipitation to SASWMS Server....");
                Console.ResetColor();

                WebClient wb = new WebClient();
                wb.Credentials = new NetworkCredential("saswms", "S@swe2015");
                string[] basinName = new string[] { "brahmaputra", "ganges", "indus1", "pakistan" };
                Console.ForegroundColor = ConsoleColor.Green;
                foreach (string basin in basinName)
                {
                    try
                    {
                        string dailyData = preplainDate + @".precip.IMERGRTCorr." + basin + ".txt";
                        string dailyDataPath = iniDi + @"\BasinRainfall\" + preplainDate + @".precip.imergrtcorr." + basin + ".txt";

                        wb.UploadFile("ftp://128.95.45.89/%2F/opt/lampp/htdocs/repository/" + dailyData, "STOR", dailyDataPath);
                        Console.WriteLine(dailyData);
                        logText.AppendLine(dailyData);
                    }
                    catch (WebException e)
                    {
                        string status = ((FtpWebResponse)e.Response).StatusDescription;
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Server Error: " + status);
                        Console.ResetColor();
                    }
                }

                /*SessionOptions sessionOptions = new SessionOptions();
                sessionOptions.Protocol = Protocol.Sftp;
                sessionOptions.HostName = "saswe@ovid.u.washington.edu";
                sessionOptions.UserName = "saswe";
                sessionOptions.Password = "Bangla_Power21Feb";
                sessionOptions.SshHostKeyFingerprint = "ssh-rsa 1024 22:c7:83:db:1c:c5:7d:10:15:23:f5:64:36:af:b5:b2";

                Session session = new Session();
                session.Open(sessionOptions);
                TransferOptions transferOptions = new TransferOptions();
                transferOptions.TransferMode = TransferMode.Binary;
                TransferOperationResult transferResult;

                string[] basinName = new string[] { "brahmaputra", "ganges", "indus1", "pakistan" };

                foreach (string element in basinName)
                {
                    string dailyData = @"Repository/" + preplainDate + @".precip.IMERGRTCorr." + element + ".txt";
                    string dailyDataPath = iniDi + @"\BasinRainfall\" + preplainDate + @".precip.imergrtcorr." + element + ".txt";
                    transferResult = session.PutFiles(dailyDataPath, dailyData, false, transferOptions);
                    Console.WriteLine(transferResult.Transfers[0].FileName);
                }*/

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Bias corrected IMERG precipitation files successfully sent to SASWMS Server.");
                logText.AppendLine("Bias corrected IMERG precipitation files successfully sent to SASWMS Server.");
                Console.ResetColor();

                Console.ForegroundColor = ConsoleColor.Green;
                float[] statsGanges = statsASCII(iniDi + @"\BasinRainfall\" + preplainDate + @".precip.imergrtcorr.ganges.txt");
                Console.WriteLine("Maximum, Average and Minimum of Bias Corrected IMERGRT estimated Rainfall in mm over Ganges Basin: " + statsGanges[0].ToString("0.00") + ", " + statsGanges[1].ToString("0.00") + " and " + statsGanges[2].ToString("0.00"));
                logText.AppendLine("Maximum, Average and Minimum of Bias Corrected IMERGRT estimated Rainfall in mm over Ganges Basin: " + statsGanges[0].ToString("0.00") + ", " + statsGanges[1].ToString("0.00") + " and " + statsGanges[2].ToString("0.00"));

                float[] statsBrahma = statsASCII(iniDi + @"\BasinRainfall\" + preplainDate + @".precip.imergrtcorr.brahmaputra.txt");
                Console.WriteLine("Maximum, Average and Minimum of Bias Corrected IMERGRT estimated Rainfall in mm over Brahmaputra Basin: " + statsBrahma[0].ToString("0.00") + ", " + statsBrahma[1].ToString("0.00") + " and " + statsBrahma[2].ToString("0.00"));
                logText.AppendLine("Maximum, Average and Minimum of Bias Corrected IMERGRT estimated Rainfall in mm over Brahmaputra Basin: " + statsBrahma[0].ToString("0.00") + ", " + statsBrahma[1].ToString("0.00") + " and " + statsBrahma[2].ToString("0.00"));

                float[] statsIndus = statsASCII(iniDi + @"\BasinRainfall\" + preplainDate + @".precip.imergrtcorr.indus1.txt");
                Console.WriteLine("Maximum, Average and Minimum of Bias Corrected IMERGRT estimated Rainfall in mm over Indus Basin: " + statsIndus[0].ToString("0.00") + ", " + statsIndus[1].ToString("0.00") + " and " + statsIndus[2].ToString("0.00"));
                logText.AppendLine("Maximum, Average and Minimum of Bias Corrected IMERGRT estimated Rainfall in mm over Indus Basin: " + statsIndus[0].ToString("0.00") + ", " + statsIndus[1].ToString("0.00") + " and " + statsIndus[2].ToString("0.00"));

                float[] statsPakistan = statsASCII(iniDi + @"\BasinRainfall\" + preplainDate + @".precip.imergrtcorr.pakistan.txt");
                Console.WriteLine("Maximum, Average and Minimum of Bias Corrected IMERGRT estimated Rainfall in mm over Pakistan: " + statsPakistan[0].ToString("0.00") + ", " + statsPakistan[1].ToString("0.00") + " and " + statsPakistan[2].ToString("0.00"));
                logText.AppendLine("Maximum, Average and Minimum of Bias Corrected IMERGRT estimated Rainfall in mm over Pakistan: " + statsPakistan[0].ToString("0.00") + ", " + statsPakistan[1].ToString("0.00") + " and " + statsPakistan[2].ToString("0.00"));
                Console.ResetColor();
            }
            catch (Exception error)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Bias corrected IMERG Precipitation files cannot be sent to SASWMS Server. Please check internet connectivity of Computer. Error: " + error.Message);
                logText.AppendLine("Bias corrected IMERG Precipitation files cannot be sent to SASWMS Server. Please check internet connectivity of Computer. Error: " + error.Message);
                Console.ResetColor();
            }

            logText.AppendLine("All scheduled tasks completed at " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + ".");
            File.WriteAllText(iniDi + @"\Logfiles\PrecipitationCorrection_" + hiphenDate + ".txt", logText.ToString());

            Process.Start(@"C:\Users\nbiswas\Desktop\Nishan\SASWE\AutoCorrection\Programs\ExecutionFiles\FileMapExchange.exe");
        }

        private static float[] statsASCII(string fileapth)
        {
            float[] values = new float[3];
            string[] contents = File.ReadAllLines(fileapth);

            char[] charSeparator = { ' ' };
            List<float> dataArray = new List<float>();
            for (int i = 6; i < contents.Length; i++)
            {
                var textparse = contents[i].Split(charSeparator, StringSplitOptions.RemoveEmptyEntries);
                for (int j = 0; j < textparse.Length; j++)
                {
                    if (textparse[j] != "-9999")
                    {
                        dataArray.Add(float.Parse(textparse[j]));
                    }
                }
            }
            values[0] = dataArray.Max();
            values[1] = dataArray.Average();
            values[2] = dataArray.Min();
            return values;
        }
    }
}
    

