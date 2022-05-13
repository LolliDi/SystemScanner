using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
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
            try
            {
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
                computer.Users = GetUserList();
                computer.IpInternet = new WebClient().DownloadString("https://api.ipify.org"); //получаем с сайта свой ип
                computer.PCName = Dns.GetHostName();
                computer.UserNick = Environment.UserName;
                computer.IpLocal = GetLocalIp();
                GetInfo();
                computer.DateCheck = DateTime.Now;
                DBCl.db.SaveChanges();
                UpdateContexts();

                MessageBox.Show("Данные считаны и записаны в БД");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Произошла ошибка, проверьте подключение к интернету.\nИнформация: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public string GetLocalIp()
        {
            var ips = Dns.GetHostAddresses(Dns.GetHostName());
            Regex r = new Regex(@"\A192\.168\.");
            foreach (IPAddress ip in ips)
            {
                if (r.IsMatch(ip.ToString()))
                {
                    return ip.ToString();
                }
            }
            return "";
        }

        public string GetUserList()
        {
            string s = "";
            DirectoryEntry localMachine = new DirectoryEntry("WinNT://" + Environment.MachineName);
            foreach (DirectoryEntry child in localMachine.Children)
            {
                if (child.SchemaClassName == "User" && child.Name != "DefaultAccount" && child.Name != "WDAGUtilityAccount")
                {
                    s += child.Name + ", ";
                }
            }
            return s.Substring(0,s.Length-2);
        }



        public void UpdateContexts() //обновляем данные для отображения
        {
            OSinfo.DataContext = oS;
            StackPanelMather.DataContext = motherBoard;
            StckComputer.DataContext = computer;
            StackPanelProcessor.DataContext = processor;
            ListViewMemory.Items.Refresh();
            ListViewMemory.ItemsSource = physicalMemories;
            ListViewHard.Items.Refresh();
            ListViewHard.ItemsSource = hardDrives;
            ListViewVideo.Items.Refresh();
            ListViewVideo.ItemsSource = videoControllers;
        }

        #region GetHardwareInfo
        public void GetInfo() //собираем информацию о текущем ПК
        {
            GetHardWareInfo("Win32_Processor");
            GetHardWareInfo("Win32_VideoController");
            List<ComputersVideo> c = DBCl.db.ComputersVideo.Where(x => x.IdPC == idPC).ToList();
            foreach(VideoControllers v in videoControllers) //смотрим каких связей не хватает в таблице и добавляем
            {
                ComputersVideo cv = c.FirstOrDefault(x => x.IdVideo == v.Id);
                if(cv==null)
                {
                    DBCl.db.ComputersVideo.Add(new ComputersVideo()
                    {
                        IdPC = idPC,
                        IdVideo = v.Id,
                    });
                }
            }
            foreach(ComputersVideo v in c) //смотрим какие лишние и удаляем
            {
                VideoControllers vc = videoControllers.FirstOrDefault(x => x.Id == v.IdVideo);
                if(vc==null)
                {
                    DBCl.db.ComputersVideo.Remove(v);
                }
            }
            DBCl.db.SaveChanges();
            
            List<PhysicalMemory> pm = DBCl.db.PhysicalMemory.Where(x => x.IdPC == idPC).ToList();
            foreach (PhysicalMemory mem in pm) //удаляем все плашки ОП для этого пк
            {
                DBCl.db.PhysicalMemory.Remove(mem);
            }
            DBCl.db.SaveChanges();
            GetHardWareInfo("Win32_PhysicalMemory");
            foreach (PhysicalMemory mem in physicalMemories) //добавляем текущие
            {
                DBCl.db.PhysicalMemory.Add(mem);
            }
            DBCl.db.SaveChanges();

            GetHardWareInfo("Win32_DiskDrive");
            List<ComputerHard> ch = DBCl.db.ComputerHard.Where(x => x.IdPC == idPC).ToList();
            foreach (HardDrives hd in hardDrives) //смотрим каких связей не хватает в таблице и добавляем
            {
                ComputerHard cv = ch.FirstOrDefault(x => x.IdHard == hd.Id);
                if (cv == null)
                {
                    DBCl.db.ComputerHard.Add(new ComputerHard()
                    {
                        IdPC = idPC,
                        IdHard = hd.Id,
                    });
                }
            }
            foreach (ComputerHard v in ch) //смотрим какие лишние и удаляем
            {
                HardDrives hd = hardDrives.FirstOrDefault(x => x.Id == v.IdHard);
                if (hd == null)
                {
                    DBCl.db.ComputerHard.Remove(v);
                }
            }
            DBCl.db.SaveChanges();
            GetHardWareInfo("Win32_OperatingSystem");
            GetHardWareInfo("Win32_BaseBoard");
        }


        public List<string> GetHardwareInfo(string WIN32_Class, string ClassItemField) //получение определенного параметра системы
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

        private void GetHardWareInfo(string key) //получение всей инфы о ключе
        {
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
                            processor = DBCl.db.Processors.FirstOrDefault(x => x.Model == model);
                            if (processor == null)
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
                            string modelVideo = obj["Caption"].ToString().Trim();
                            string videoProcessor = obj["VideoProcessor"].ToString().Trim();
                            double adapterRAMMB = Convert.ToDouble(obj["AdapterRAM"].ToString().Trim()) / 1024 / 1024;
                            VideoControllers v = DBCl.db.VideoControllers.FirstOrDefault(x => x.Manufacturer == manufacturer && x.Model == modelVideo);
                            if (v == null)
                            {
                                v = new VideoControllers()
                                {
                                    Manufacturer = manufacturer,
                                    Model = modelVideo,
                                    VideoProcessor = videoProcessor,
                                    AdapterRAMMB = adapterRAMMB
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
                            if (hd == null)
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
                            string chip = GetHardwareInfo("Win32_Processor", "SocketDesignation")[0];
                            motherBoard = DBCl.db.MotherBoards.FirstOrDefault(x => x.Model == modelMother && x.Manufacturer == manufacturerMother);
                            if (motherBoard == null)
                            {
                                motherBoard = new MotherBoards()
                                {
                                    Manufacturer = manufacturerMother,
                                    Model = modelMother,
                                    MaxPhysicalMemoryMB = maxPhysicalMemoryMB,
                                    SlotsMemory = slotsMemory,
                                    MemoryType = physicalMemories[0].MemoryType,
                                    ChipSet = chip,
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

        public string GetMemoryType(int type) //тип оперативной памяти
        {
            string outValue = string.Empty;

            switch (type)
            {
                case 0x0: outValue = "DDR4"; break;
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
        #endregion

        #region EditInformationAndViews
        private bool isToggleProcessor;
        private bool isToggleMemory;
        bool isToggleBoard;

        private void ShowPanel(ItemsControl sp, ref bool toggle, int height, Button btn)
        {
            DoubleAnimation da = new DoubleAnimation();
            if (!toggle)
            {
                da.To = height;
                da.Duration = TimeSpan.FromSeconds(0.25);
                sp.BeginAnimation(Border.HeightProperty, da);
                toggle = true;
                btn.Content = "/\\";
            }
            else
            {
                da.To = 0;
                da.Duration = TimeSpan.FromSeconds(0.25);
                sp.BeginAnimation(Border.HeightProperty, da);
                toggle = false;
                btn.Content = "\\/";
            }
        }
        private void ShowPanel(GroupBox sp, ref bool toggle, int height, Button btn)
        {
            DoubleAnimation da = new DoubleAnimation();
            if (!toggle)
            {
                da.To = height;
                da.Duration = TimeSpan.FromSeconds(0.25);
                sp.BeginAnimation(Border.HeightProperty, da);
                toggle = true;
                btn.Content = "/\\";
            }
            else
            {

                da.To = 0;
                da.Duration = TimeSpan.FromSeconds(0.25);
                sp.BeginAnimation(Border.HeightProperty, da);
                toggle = false;
                btn.Content = "\\/";
            }
        }
        bool isToggleVideo;
        bool isToggleOSInfo;
        private void BtnOSInfo_Click(object sender, RoutedEventArgs e)
        {
            ShowPanel(GroupOSInfo, ref isToggleOSInfo, 210, BtnOSInfo);
        }
        private void ManufacturerBoard_Changed(object sender, TextChangedEventArgs e)
        {
            motherBoard.Manufacturer = (sender as TextBox).Text;
        }
        private void Socket_Changed(object sender, TextChangedEventArgs e)
        {
            motherBoard.ChipSet = (sender as TextBox).Text;
        }
        private void ModelBoard_Changed(object sender, TextChangedEventArgs e)
        {
            motherBoard.Model = (sender as TextBox).Text;
        }
        public double? SetNumerableValue(double? m, TextBox tb)
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
            ShowPanel(GroupBoard, ref isToggleBoard, 125, BtnBoard);
        }
        private void Processor_Click(object sender, RoutedEventArgs e)
        {
            ShowPanel(GroupProcessor, ref isToggleProcessor, 210, BtnProcessor);
        }
        private void MemoryStck_Click(object sender, RoutedEventArgs e)
        {
            ShowPanel(ListViewMemory, ref isToggleMemory, physicalMemories.Count * 92, BtnRAM);
        }
        private void Video_Click(object sender, RoutedEventArgs e)
        {
            ShowPanel(ListViewVideo, ref isToggleVideo, videoControllers.Count * 135, BtnVideo);
        }
        private void ManufacturerVideo_Changed(object sender, TextChangedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            videoControllers.FirstOrDefault(x => x.Id == Convert.ToInt32(tb.Uid)).Manufacturer = tb.Text;
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
            ShowPanel(ListViewHard, ref isToggleHard, hardDrives.Count * 130, BtnHard);
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
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            DBCl.db.SaveChanges();
            MessageBox.Show("Данные сохранены");
        }
        private void CompName_Changed(object sender, TextChangedEventArgs e)
        {
            computer.Name = (sender as TextBox).Text;
        }
        private void CompNumber_Changed(object sender, TextChangedEventArgs e)
        {
            computer.InventoryNumber = (sender as TextBox).Text;
        }
        private void Room_Changed(object sender, TextChangedEventArgs e)
        {
            computer.RoomNumber = (sender as TextBox).Text;
        }
        private void Description_Changed(object sender, TextChangedEventArgs e)
        {
            computer.Description = (sender as TextBox).Text;
        }
        #endregion

        private void ReloadInfo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DBCl.db = new ComputersInfoEntities1();
                computer = DBCl.db.Computers.FirstOrDefault(x => x.id == idPC);
                motherBoard = DBCl.db.MotherBoards.FirstOrDefault(x => x.Id == computer.MotherBoardId);
                processor = DBCl.db.Processors.FirstOrDefault(x => x.Id == computer.ProcessorId);
                physicalMemories = DBCl.db.PhysicalMemory.Where(x => x.IdPC == idPC).ToList();
                hardDrives.Clear();
                foreach (ComputerHard ch in DBCl.db.ComputerHard.Where(x => x.IdPC == idPC).ToList())
                {
                    hardDrives.Add(DBCl.db.HardDrives.FirstOrDefault(x => x.Id == ch.IdHard));
                }
                videoControllers.Clear();
                foreach (ComputersVideo ch in DBCl.db.ComputersVideo.Where(x => x.IdPC == idPC).ToList())
                {
                    videoControllers.Add(DBCl.db.VideoControllers.FirstOrDefault(x => x.Id == ch.IdVideo));
                }
                UpdateContexts();
                MessageBox.Show("Несохраненные изменения сброшены");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Произошла ошибка:\n"+ex,"Ошибка",MessageBoxButton.OK,MessageBoxImage.Error);
            }
        }

        
    }
}
