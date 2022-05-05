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
            ListViewMemory.Height = 0;
            ListViewVideo.Height = 0;
            ListViewHard.Height = 0;
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
            StackPanelMather.DataContext = motherBoard;
            StackPanelProcessor.DataContext = processor;
            ListViewMemory.ItemsSource=physicalMemories;
            ListViewHard.ItemsSource = hardDrives;
            ListViewVideo.ItemsSource = videoControllers;
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
        private bool isToggleProcessor;
        private bool isToggleMemory;
        bool isToggleBoard;
        private void ShowPanel(ListView sp, ref bool toggle, int height)
        {
            DoubleAnimation da = new DoubleAnimation();
            if (!toggle)
            {
                da.To = height;
                da.Duration = TimeSpan.FromSeconds(0.25);
                sp.BeginAnimation(Border.HeightProperty, da);
                toggle = true;
            }
            else
            {
                da.To = 0;
                da.Duration = TimeSpan.FromSeconds(0.25);
                sp.BeginAnimation(Border.HeightProperty, da);
                toggle = false;
            }
        }
        private void ShowPanel(GroupBox sp, ref bool toggle, int height)
        {
            DoubleAnimation da = new DoubleAnimation();
            if (!toggle)
            {
                da.To = height;
                da.Duration = TimeSpan.FromSeconds(0.25);
                sp.BeginAnimation(Border.HeightProperty, da);
                toggle = true;
            }
            else
            {

                da.To = 0;
                da.Duration = TimeSpan.FromSeconds(0.25);
                sp.BeginAnimation(Border.HeightProperty, da);
                toggle = false;
            }
        }
        bool isToggleVideo;
        private void ManufacturerBoard_Changed(object sender, TextChangedEventArgs e)
        {
            motherBoard.Manufacturer = (sender as TextBox).Text;
        }
        private void ModelBoard_Changed(object sender, TextChangedEventArgs e)
        {
            motherBoard.Model = (sender as TextBox).Text;
        }
        public double? SetNumerableValue (double? m, TextBox tb)
        {
            try
            {
                if (tb.Text.Length > 0)
                    return Convert.ToDouble(tb.Text);
                else
                    return null;
                
            }
            catch
            {
                tb.Text = m.ToString();
                return m;
            }
        }
        public int? SetNumerableValue(int? m, TextBox tb)
        {
            try
            {
                if (tb.Text.Length > 0)
                    return Convert.ToInt32(tb.Text);
                else
                    return null;
            }
            catch
            {
                tb.Text = m.ToString();
                return m;
            }
        }
        private void MaxPhysicalMemoryMB_Changed(object sender, TextChangedEventArgs e)
        {

            motherBoard.MaxPhysicalMemoryMB = SetNumerableValue(motherBoard.MaxPhysicalMemoryMB, sender as TextBox);
        }
        private void SlotsMemory_Changed(object sender, TextChangedEventArgs e)
        {

            motherBoard.SlotsMemory = SetNumerableValue(motherBoard.SlotsMemory, sender as TextBox);
        }
        private void MemoryType_Changed(object sender, TextChangedEventArgs e)
        {

            motherBoard.MemoryType = (sender as TextBox).Text;
        }
        private void CanalsMemoryCount_Changed(object sender, TextChangedEventArgs e)
        {

            motherBoard.CanalsMemoryCount = SetNumerableValue(motherBoard.CanalsMemoryCount, sender as TextBox);
        }
        private void ManufacturerProcessor_Changed(object sender, TextChangedEventArgs e)
        {
            processor.Manufacturer = (sender as TextBox).Text;
        }
        private void ModelProcessor_Changed(object sender, TextChangedEventArgs e)
        {
            processor.Model = (sender as TextBox).Text;
        }
        private void NumberOfCores_Changed(object sender, TextChangedEventArgs e)
        {
            processor.NumberOfCores = SetNumerableValue(processor.NumberOfCores, sender as TextBox);
        }
        private void ThreadCount_Changed(object sender, TextChangedEventArgs e)
        {
            processor.ThreadCount = SetNumerableValue(processor.ThreadCount, sender as TextBox);
        }
        private void StartClockSpeed_Changed(object sender, TextChangedEventArgs e)
        {
            processor.StartClockSpeed = SetNumerableValue(processor.StartClockSpeed, sender as TextBox);
        }
        private void TechnicalProcess_Changed(object sender, TextChangedEventArgs e)
        {
            processor.TechnicalProcess = SetNumerableValue(processor.TechnicalProcess, sender as TextBox);    
        }
        private void L1CacheMB_Changed(object sender, TextChangedEventArgs e)
        {
            processor.L1CacheMB = SetNumerableValue(processor.L1CacheMB, sender as TextBox);
        }
        private void L2CacheMB_Changed(object sender, TextChangedEventArgs e)
        {
            processor.L2CacheMB = SetNumerableValue(processor.L2CacheMB, sender as TextBox);
        }
        private void L3CacheMB_Changed(object sender, TextChangedEventArgs e)
        {
            processor.L3CacheMB = SetNumerableValue(processor.L3CacheMB, sender as TextBox);
        }
        private void Board_Click(object sender, RoutedEventArgs e)
        {
            ShowPanel(GroupBoard, ref isToggleBoard, 125);
        }
        private void Processor_Click(object sender, RoutedEventArgs e)
        {
            ShowPanel(GroupProcessor, ref isToggleProcessor, 210);
        }
        private void MemoryStck_Click(object sender, RoutedEventArgs e)
        {
            ShowPanel(ListViewMemory, ref isToggleMemory, physicalMemories.Count*92);
        }
        private void Video_Click(object sender, RoutedEventArgs e)
        {
            ShowPanel(ListViewVideo, ref isToggleVideo, videoControllers.Count*225);
        }
        private void ManufacturerVideo_Changed(object sender, TextChangedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            videoControllers.FirstOrDefault(x=>x.Id == Convert.ToInt32(tb.Uid)).Manufacturer = tb.Text;
        }
        private void VideoProc_Changed(object sender, TextChangedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            videoControllers.FirstOrDefault(x => x.Id == Convert.ToInt32(tb.Uid)).VideoProcessor = tb.Text;
        }
        private void AdapterRAMMB_Changed(object sender, TextChangedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            VideoControllers vc = videoControllers.FirstOrDefault(x => x.Id == Convert.ToInt32(tb.Uid));
            vc.AdapterRAMMB = SetNumerableValue(vc.AdapterRAMMB, tb);
        }
        private void MaxFPS_Changed(object sender, TextChangedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            VideoControllers vc = videoControllers.FirstOrDefault(x => x.Id == Convert.ToInt32(tb.Uid));
            vc.MaxRefreshRate = SetNumerableValue(vc.MaxRefreshRate, tb);
        }
        private void Vertical_Changed(object sender, TextChangedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            VideoControllers vc = videoControllers.FirstOrDefault(x => x.Id == Convert.ToInt32(tb.Uid));
            vc.CurrentVerticalResolution = SetNumerableValue(vc.CurrentVerticalResolution, tb);

        }
        private void Horizontal_Changed(object sender, TextChangedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            VideoControllers vc = videoControllers.FirstOrDefault(x => x.Id == Convert.ToInt32(tb.Uid));
            vc.CurrentVHorizontalResolution = SetNumerableValue(vc.CurrentVHorizontalResolution, tb);
        }
        private void MemFrequency_Changed(object sender, TextChangedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            PhysicalMemory vc = physicalMemories.FirstOrDefault(x => x.Id == Convert.ToInt32(tb.Uid));
            vc.Frequency = SetNumerableValue(vc.Frequency, tb);
        }
        private void MemSize_Changed(object sender, TextChangedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            PhysicalMemory vc = physicalMemories.FirstOrDefault(x => x.Id == Convert.ToInt32(tb.Uid));
            vc.SizeMB = SetNumerableValue(vc.SizeMB, tb);
        }
        private void MemType_Changed(object sender, TextChangedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            physicalMemories.FirstOrDefault(x => x.Id == Convert.ToInt32(tb.Uid)).MemoryType = tb.Text;
        }
        bool isToggleHard;
        private void Hard_Click(object sender, RoutedEventArgs e)
        {
            ShowPanel(ListViewHard, ref isToggleHard, hardDrives.Count * 220);
        }
        private void Manufacturer_Changed(object sender, TextChangedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            hardDrives.FirstOrDefault(x => x.Id == Convert.ToInt32(tb.Uid)).Manufacturer = tb.Text;
        }
        private void Type_Changed(object sender, TextChangedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            hardDrives.FirstOrDefault(x => x.Id == Convert.ToInt32(tb.Uid)).Type = tb.Text;
        }
        private void SizeGB_Changed(object sender, TextChangedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            HardDrives vc = hardDrives.FirstOrDefault(x => x.Id == Convert.ToInt32(tb.Uid));
            vc.SizeGB = SetNumerableValue(vc.SizeGB, tb);
        }
        private void Interface_Changed(object sender, TextChangedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            hardDrives.FirstOrDefault(x => x.Id == Convert.ToInt32(tb.Uid)).Interface = tb.Text;
        }
        private void SpeedWrite_Changed(object sender, TextChangedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            HardDrives vc = hardDrives.FirstOrDefault(x => x.Id == Convert.ToInt32(tb.Uid));
            vc.SpeedWriteMBS = SetNumerableValue(vc.SpeedWriteMBS, tb);
        }
        private void SpeedRead_Changed(object sender, TextChangedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            HardDrives vc = hardDrives.FirstOrDefault(x => x.Id == Convert.ToInt32(tb.Uid));
            vc.SpeedReadMBS = SetNumerableValue(vc.SpeedReadMBS, tb);
        }
        private void Buffer_Changed(object sender, TextChangedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            HardDrives vc = hardDrives.FirstOrDefault(x => x.Id == Convert.ToInt32(tb.Uid));
            vc.BufferMB = SetNumerableValue(vc.BufferMB, tb);
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            DBCl.db.SaveChanges();
            MessageBox.Show("Данные сохранены");
        }
    }
}
