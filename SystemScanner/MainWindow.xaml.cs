using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace SystemScanner
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Computers computer;
        Processors processor;
        List<VideoControllers> videoControllers = new List<VideoControllers>();
        List<PhysicalMemory> physicalMemories = new List<PhysicalMemory>();
        List<HardDrives> hardDrives = new List<HardDrives>();
        MotherBoards motherBoard = new MotherBoards();
        OS oS;
        int idPC;
        public MainWindow()
        {
            InitializeComponent();
            ListViewProcessors.Height = 0;
            ListViewMemory.Height = 0;
            ListViewVideo.Height = 0;
            ListViewMather.Height = 0;
            string macAddr =
            (
                from nic in NetworkInterface.GetAllNetworkInterfaces()
                where nic.OperationalStatus == OperationalStatus.Up
                select nic.GetPhysicalAddress().ToString()
            ).FirstOrDefault();
            computer = DBCl.db.Computers.FirstOrDefault(c => c.MacAdress == macAddr);
            if (computer != null)
            {
                idPC = computer.id;
            }
            else
            {
                computer = new Computers()
                {
                    MacAdress = macAddr,
                };
                DBCl.db.Computers.Add(computer);
                DBCl.db.SaveChanges();
                idPC = computer.id;
            }
            GetInfo();
            computer.DateCheck = DateTime.Now;
            DBCl.db.SaveChanges();
            ListViewMemory.ItemsSource=physicalMemories;
            ListViewProcessors.Items.Add(processor);
            ListViewVideo.ItemsSource = videoControllers;
            ListViewMather.Items.Add( motherBoard);
            //GetHardWareInfo("Win32_PhysicalMemory");
        }

        public void GetInfo()
        {
            GetHardWareInfo("Win32_Processor");
            GetHardWareInfo("Win32_VideoController");

            foreach (VideoControllers v in videoControllers)
            {
                ComputersVideo cv = DBCl.db.ComputersVideo.FirstOrDefault(x => x.IdPC == idPC && x.IdVideo == v.Id);
                if (cv != null)
                {
                    DBCl.db.ComputersVideo.Remove(cv);
                }
            }
            DBCl.db.SaveChanges();
            foreach (VideoControllers v in videoControllers)
            {
                DBCl.db.ComputersVideo.Add(new ComputersVideo()
                {
                    IdPC = idPC,
                    IdVideo = v.Id,
                });
               
            }
            DBCl.db.SaveChanges();
            List<PhysicalMemory> pm = DBCl.db.PhysicalMemory.Where(x=>x.IdPC==idPC).ToList();
            foreach (PhysicalMemory mem in pm)
            {
                DBCl.db.PhysicalMemory.Remove(mem);
            }
            DBCl.db.SaveChanges();
            GetHardWareInfo("Win32_PhysicalMemory");
            foreach(PhysicalMemory mem in physicalMemories)
            {
                DBCl.db.PhysicalMemory.Add(mem);
            }
            DBCl.db.SaveChanges();

            GetHardWareInfo("Win32_DiskDrive");
            foreach (HardDrives v in hardDrives)
            {
                ComputerHard cv = DBCl.db.ComputerHard.FirstOrDefault(x => x.IdPC == idPC && x.IdHard == v.Id);
                if (cv != null)
                {
                    DBCl.db.ComputerHard.Remove(cv);
                }
            }
            DBCl.db.SaveChanges();
            foreach (HardDrives v in hardDrives)
            {
                DBCl.db.ComputerHard.Add(new ComputerHard() { IdPC = idPC, IdHard = v.Id});
            }
            DBCl.db.SaveChanges();
            GetHardWareInfo("Win32_OperatingSystem");
            GetHardWareInfo("Win32_BaseBoard");
        }

        public List<string> GetHardwareInfo(string WIN32_Class, string ClassItemField)
        {
            List<string> result = new List<string>();

            ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM " + WIN32_Class);

            try
            {
                foreach (ManagementObject obj in searcher.Get())
                {
                    if (obj.Properties.Count > 0)
                    {
                        result.Add(obj[ClassItemField].ToString().Trim());
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return result;
        }

        private void GetHardWareInfo(string key)
        {
            //ListViewInfo.Items.Clear();

            ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM " + key);
            try
            {
                foreach (ManagementObject obj in searcher.Get())
                {


                    if (obj.Properties.Count == 0)
                    {
                        MessageBox.Show("Не получилося");
                        return;
                    }
                    switch (key)
                    {
                        case "Win32_Processor":
                            string model = obj["Name"].ToString().Trim();
                            int numberOfCores = Convert.ToInt32(obj["NumberOfCores"].ToString().Trim());
                            double startClockSpeed = Convert.ToDouble(obj["CurrentClockSpeed"].ToString().Trim());
                            int threadCount = Convert.ToInt32(obj["ThreadCount"].ToString().Trim());
                            double l1CacheMB = Convert.ToDouble(GetHardwareInfo("Win32_CacheMemory", "MaxCacheSize")[0].ToString().Trim()) / 1024;
                            double l2CacheMB = Convert.ToDouble(obj["L2CacheSize"].ToString().Trim()) / 1024;
                            double l3CacheMB = Convert.ToDouble(obj["L3CacheSize"].ToString().Trim()) / 1024;
                            processor = DBCl.db.Processors.FirstOrDefault(x=>x.Model == model);
                            if(processor == null)
                            {
                                processor = new Processors()
                                {
                                    Model = model,
                                    NumberOfCores = numberOfCores,
                                    StartClockSpeed = startClockSpeed,
                                    ThreadCount = threadCount,
                                    L1CacheMB = l1CacheMB,
                                    L2CacheMB = l2CacheMB,
                                    L3CacheMB = l3CacheMB,
                                };
                                DBCl.db.Processors.Add(processor);
                                
                            }
                            computer.ProcessorId = processor.Id;
                            DBCl.db.SaveChanges();
                            return;
                        case "Win32_VideoController":
                            string manufacturer = obj["AdapterCompatibility"].ToString().Trim();
                             string   modelVideo = obj["Caption"].ToString().Trim();
                            string videoProcessor = obj["VideoProcessor"].ToString().Trim();
                            double adapterRAMMB = Convert.ToDouble(obj["AdapterRAM"].ToString().Trim()) / 1024 / 1024;
                            double maxRefreshRate = Convert.ToDouble(obj["MaxRefreshRate"].ToString().Trim());
                            int currentVerticalResolution = Convert.ToInt32(obj["CurrentVerticalResolution"].ToString().Trim());
                            int currentVHorizontalResolution = Convert.ToInt32(obj["CurrentHorizontalResolution"].ToString().Trim());
                            VideoControllers v = DBCl.db.VideoControllers.FirstOrDefault(x =>x.Manufacturer==manufacturer&&x.Model==modelVideo);
                            if (v == null)
                            {
                                v = new VideoControllers(){
                                    Manufacturer = manufacturer,
                                    Model = modelVideo,
                                    VideoProcessor = videoProcessor,
                                    AdapterRAMMB = adapterRAMMB,
                                    MaxRefreshRate = maxRefreshRate,
                                    CurrentVerticalResolution = currentVerticalResolution,
                                    CurrentVHorizontalResolution = currentVHorizontalResolution,
                                };
                                DBCl.db.VideoControllers.Add(v);
                                DBCl.db.SaveChanges();
                                 
                            }
                            videoControllers.Add(v);
                            
                            
                            break;
                        case "Win32_PhysicalMemory":

                            physicalMemories.Add(new PhysicalMemory()
                            {
                                IdPC = idPC,
                                SizeMB = Convert.ToDouble(obj["Capacity"].ToString().Trim()) / 1024 / 1024,
                                Frequency = Convert.ToDouble(obj["Speed"].ToString().Trim()),
                                MemoryType = GetMemoryType(Convert.ToInt32(obj["MemoryType"].ToString().Trim())),
                            });
                            break;
                        case "Win32_DiskDrive":
                            string modelDisk = obj["Caption"].ToString().Trim();
                            long sizeGB = Convert.ToInt64(obj["Size"].ToString().Trim()) / 1000000000;
                            HardDrives hd = DBCl.db.HardDrives.FirstOrDefault(x => x.Model == modelDisk);
                            if(hd == null)
                            {
                                hd = new HardDrives()
                                {
                                    Model = modelDisk,
                                    SizeGB = sizeGB,
                                };
                                DBCl.db.HardDrives.Add(hd);
                                DBCl.db.SaveChanges();
                            }
                            hardDrives.Add(hd);
                            break;
                        case "Win32_BaseBoard":
                            string manufacturerMother = obj["Manufacturer"].ToString().Trim();
                            string modelMother = obj["Product"].ToString().Trim();
                            int maxPhysicalMemoryMB = Convert.ToInt32(GetHardwareInfo("Win32_PhysicalMemoryArray", "maxCapacity")[0]) / 1024;
                            int slotsMemory = Convert.ToInt32(GetHardwareInfo("Win32_PhysicalMemoryArray", "MemoryDevices")[0]);
                            motherBoard = DBCl.db.MotherBoards.FirstOrDefault(x => x.Model == modelMother&&x.Manufacturer==manufacturerMother);
                            if (motherBoard == null)
                            {
                                motherBoard = new MotherBoards()
                                {
                                    Manufacturer = manufacturerMother,
                                    Model = modelMother,
                                    MaxPhysicalMemoryMB = maxPhysicalMemoryMB,
                                    SlotsMemory = slotsMemory,
                                    MemoryType = physicalMemories[0].MemoryType,
                                };
                                DBCl.db.MotherBoards.Add(motherBoard);
                                
                            }
                            computer.MotherBoardId = motherBoard.Id;
                            DBCl.db.SaveChanges();
                            break;
                        case "Win32_OperatingSystem":
                            oS = DBCl.db.OS.FirstOrDefault(x => x.IdPC == idPC);
                            if (oS == null)
                            {
                                oS = new OS()
                                {
                                    IdPC = idPC,
                                    Architecture = obj["OSArchitecture"].ToString().Trim(),
                                    Version = obj["Version"].ToString().Trim(),
                                    Title = obj["Caption"].ToString().Trim(),
                                    NumberProduct = obj["SerialNumber"].ToString().Trim(),
                                };
                                DBCl.db.OS.Add(oS);
                                DBCl.db.SaveChanges();
                            }
                            return;
                        default:
                            break;
                    }
                    //foreach (PropertyData data in obj.Properties)
                    //{

                    //    if (data.Value != null && !string.IsNullOrEmpty(data.Value.ToString()))
                    //    {

                    //        //ListViewInfo.Items.Add(new SysInf() { Name = data.Name, Description = data.Value.ToString() });

                    //    }
                    //}
                }

            }
            catch (Exception ex)
            { Console.WriteLine(ex.Message); }

        }

        public string GetMemoryType(int type)
        {
            string outValue = string.Empty;

            switch (type)
            {
                case 0x0: outValue = "DDR4+"; break;
                case 0x1: outValue = "Other"; break;
                case 0x2: outValue = "DRAM"; break;
                case 0x3: outValue = "Synchronous DRAM"; break;
                case 0x4: outValue = "Cache DRAM"; break;
                case 0x5: outValue = "EDO"; break;
                case 0x6: outValue = "EDRAM"; break;
                case 0x7: outValue = "VRAM"; break;
                case 0x8: outValue = "SRAM"; break;
                case 0x9: outValue = "RAM"; break;
                case 0xa: outValue = "ROM"; break;
                case 0xb: outValue = "Flash"; break;
                case 0xc: outValue = "EEPROM"; break;
                case 0xd: outValue = "FEPROM"; break;
                case 0xe: outValue = "EPROM"; break;
                case 0xf: outValue = "CDRAM"; break;
                case 0x10: outValue = "3DRAM"; break;
                case 0x11: outValue = "SDRAM"; break;
                case 0x12: outValue = "SGRAM"; break;
                case 0x13: outValue = "RDRAM"; break;
                case 0x14: outValue = "DDR"; break;
                case 0x15: outValue = "DDR2"; break;
                case 0x16: outValue = "DDR2 FB-DIMM"; break;
                case 0x17: outValue = "Undefined 23"; break;
                case 0x18: outValue = "DDR3"; break;
                case 0x19: outValue = "FBD2"; break;
                default: outValue = "Undefined"; break;
            }
            return outValue;
        }
        struct SysInf
        {
            public string Name { get; set; }
            public string Description { get; set; }

        }

        private bool isToggleProcessor;
        private bool isToggleMemory;

        private void ShowPanel(ListView sp, ref bool toggle)
        {
            DoubleAnimation da = new DoubleAnimation();
            if (!toggle)
            {
                
                da.To = 90;
                da.Duration = TimeSpan.FromSeconds(0.15);
                sp.BeginAnimation(Border.HeightProperty, da);
                toggle = true;

            }
            else
            {

                da.To = 0;
                da.Duration = TimeSpan.FromSeconds(0.15);
                sp.BeginAnimation(Border.HeightProperty, da);
                toggle = false;
                

            }
        }

        private void stck_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ShowPanel(ListViewProcessors, ref isToggleProcessor);
        }

        private void MemoryStck_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ShowPanel(ListViewMemory, ref isToggleMemory);
            
        }

        bool isToggleVideo;
        private void Video_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ShowPanel(ListViewVideo, ref isToggleVideo);
        }
        bool isToggleMather;
        private void Mather_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ShowPanel(ListViewMather, ref isToggleMather);
        }
    }
}
