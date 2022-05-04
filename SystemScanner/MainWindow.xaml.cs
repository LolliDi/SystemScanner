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
        List<Processors> processors = new List<Processors>();
        List<VideoControllers> videoControllers = new List<VideoControllers>();
        List<PhysicalMemory> physicalMemories = new List<PhysicalMemory>();
        List<HardDrives> hardDrives = new List<HardDrives>();
        List<MotherBoards> motherBoards = new List<MotherBoards>();
        List<OS> oS = new List<OS>();
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
            Computers pc = DBCl.db.Computers.FirstOrDefault(c => c.macAdress == macAddr);
            if (pc != null)
            {
                idPC = pc.id;
            }
            else
            {
                DBCl.db.Computers.Add(new Computers()
                {
                    macAdress = macAddr,
                });
                DBCl.db.SaveChanges();
                idPC = DBCl.db.Computers.FirstOrDefault(c => c.macAdress == macAddr).id;
            }
            GetHardWareInfo("Win32_Processor");
            GetHardWareInfo("Win32_VideoController");
            GetHardWareInfo("Win32_PhysicalMemory");
            GetHardWareInfo("Win32_DiskDrive");
            GetHardWareInfo("Win32_OperatingSystem");
            GetHardWareInfo("Win32_BaseBoard");
            ListViewMemory.ItemsSource = physicalMemories;
            ListViewProcessors.ItemsSource = processors;
            ListViewVideo.ItemsSource = videoControllers;
            ListViewMather.ItemsSource = motherBoards;
            //GetHardWareInfo("Win32_PhysicalMemory");
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
                            processors.Add(new Processors()
                            {
                                IdPC = idPC,
                                Model = obj["Name"].ToString().Trim(),
                                NumberOfCores = Convert.ToInt32(obj["NumberOfCores"].ToString().Trim()),
                                StartClockSpeed = Convert.ToDouble(obj["CurrentClockSpeed"].ToString().Trim()),
                                ThreadCount = Convert.ToInt32(obj["ThreadCount"].ToString().Trim()),
                                L2CacheMB = Convert.ToDouble(obj["L2CacheSize"].ToString().Trim()) / 1024,
                                L3CacheMB = Convert.ToDouble(obj["L3CacheSize"].ToString().Trim()) / 1024,

                            });
                            break;
                        case "Win32_VideoController":
                            videoControllers.Add(new VideoControllers()
                            {
                                IdPC = idPC,
                                Manufacturer = obj["AdapterCompatibility"].ToString().Trim(),
                                Model = obj["Caption"].ToString().Trim(),
                                VideoProcessor = obj["VideoProcessor"].ToString().Trim(),
                                AdapterRAMMB = Convert.ToDouble(obj["AdapterRAM"].ToString().Trim()) / 1024 / 1024,
                                MaxRefreshRate = Convert.ToDouble(obj["MaxRefreshRate"].ToString().Trim()),
                                CurrentVerticalResolution = Convert.ToInt32(obj["CurrentVerticalResolution"].ToString().Trim()),
                                CurrentVHorizontalResolution = Convert.ToInt32(obj["CurrentHorizontalResolution"].ToString().Trim()),
                            });
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
                            hardDrives.Add(new HardDrives()
                            {
                                IdPC = idPC,
                                Model = obj["Caption"].ToString().Trim(),
                                SizeGB = Convert.ToInt64(obj["Size"].ToString().Trim()) / 1000000,
                            }); ;
                            break;
                        case "Win32_BaseBoard":
                            motherBoards.Add(new MotherBoards()
                            {
                                Manufacturer = obj["Manufacturer"].ToString().Trim(),
                                Model = obj["Product"].ToString().Trim(),
                                IdPC = idPC,
                                MaxPhysicalMemoryMB = Convert.ToInt32(GetHardwareInfo("Win32_PhysicalMemoryArray", "maxCapacity")[0]) / 1024,
                                SlotsMemory = Convert.ToInt32(GetHardwareInfo("Win32_PhysicalMemoryArray", "MemoryDevices")[0]),

                            });
                            break;
                        case "Win32_OperatingSystem":
                            oS.Add(new OS()
                            {
                                IdPC = idPC,
                                Architecture = obj["OSArchitecture"].ToString().Trim(),
                                Version = obj["Version"].ToString().Trim(),
                                Title = obj["Caption"].ToString().Trim(),
                            });
                            break;
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
        //private void btn_Click(object sender, RoutedEventArgs e)
        //{
        //    DoubleAnimation da = new DoubleAnimation();
        //    if (!isToggleProcessor)
        //    {
        //        da.To = 90;
        //        da.Duration = TimeSpan.FromSeconds(1);
        //        brd.BeginAnimation(Border.HeightProperty, da);
        //        isToggleProcessor = true;
        //    }
        //    else
        //    {
        //        da.To = 0;
        //        da.Duration = TimeSpan.FromSeconds(1);
        //        brd.BeginAnimation(Border.HeightProperty, da);
        //        isToggleProcessor = false;
        //    }
        //}

        private void ShowPanel(ListView sp, ref bool toggle)
        {
            DoubleAnimation da = new DoubleAnimation();
            if (!toggle)
            {
                
                da.To = 90;
                da.Duration = TimeSpan.FromSeconds(1);
                sp.BeginAnimation(Border.HeightProperty, da);
                toggle = true;

            }
            else
            {

                da.To = 0;
                da.Duration = TimeSpan.FromSeconds(1);
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
