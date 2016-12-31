using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Speech.Synthesis;
using System.Threading;
using System.Windows.Forms;
using Rahmanism.ir;


namespace Jarvis
{
    using Mute = Configuration.Mute;

    class PerformanceMonitorExec : IDisposable
    {
        #region Fields
        PerformanceMonitor perfMonitor;
        Configuration config;

        NotifyIcon notifyIcon;
        Icon normalIcon, alertIcon;
        SpeechSynthesizer synth;
        VoiceGender voiceGender;
        Thread systemPerformanceInfoWorker;
        string cpuAlertMessage, memoryAlertMessage;
        #endregion

        #region Properties
        public Mute MemAlert = Mute.None,
                    CpuAlert = Mute.None;
        string MemoryAlertMessage
        {
            get { return memoryAlertMessage; }
        }
        string CpuAlertMessage
        {
            get { return cpuAlertMessage; }
        }
        #endregion

        #region Constructor
        public PerformanceMonitorExec()
        {
            Initialization();
        }
        #endregion

        #region Context Menu Event Handlers
        void QuitMenuItemClick(object sender, EventArgs e)
        {
            systemPerformanceInfoWorker.Abort();
            notifyIcon.Dispose();
            Application.Exit();
        }

        /// <summary>
        /// Changes mute available memory alert property to None
        /// </summary>
        void NoMuteMemAlertMenuItemClick(object sender, EventArgs e)
        {
            UpdateRadioMenu( (MenuItem)sender );
            MemAlert = Mute.None;
        }

        /// <summary>
        /// Changes mute available memory alert property to Voice mute
        /// </summary>
        void MuteMemAlertMenuItemClick(object sender, EventArgs e)
        {
            UpdateRadioMenu( (MenuItem)sender );
            MemAlert = Mute.Voice;
        }

        /// <summary>
        /// Changes mute available memory alert property to Full mute
        /// </summary>
        void FullMuteMemAlertMenuItemClick(object sender, EventArgs e)
        {
            UpdateRadioMenu( (MenuItem)sender );
            MemAlert = Mute.Full;
        }

        /// <summary>
        /// Changes mute CPU load alert property to None
        /// </summary>
        private void NoMuteCpuAlertMenuItemClick(object sender, EventArgs e)
        {
            UpdateRadioMenu( (MenuItem)sender );
            CpuAlert = Mute.None;
        }


        /// <summary>
        /// Changes mute CPU load alert property to Voice mute
        /// </summary>
        void MuteCpuAlertMenuItemClick(object sender, EventArgs e)
        {
            UpdateRadioMenu( (MenuItem)sender );
            CpuAlert = Mute.Voice;
        }

        /// <summary>
        /// Changes mute CPU load alert property to Full mute
        /// </summary>
        void FullMuteCpuAlertMenuItemClick(object sender, EventArgs e)
        {
            UpdateRadioMenu( (MenuItem)sender );
            CpuAlert = Mute.Full;
        }

        /// <summary>
        /// Runs the windows Task Manager
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void TaskManagerMenuItemClick(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start( "taskmgr.exe" );
        }

        /// <summary>
        /// Puts the app in startup
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void StartupMenuItemClick(object sender, EventArgs e)
        {
            MenuItem m = (MenuItem)sender;
            m.Checked = !m.Checked;
            bool set = m.Checked;

            // If menu checked puts the shortcut of app to startup folder,
            // else deletes the shortcut
            if ( set ) {
                Common.PutInStartupFolder();
            }
            else {
                try {
                    System.IO.File.Delete(
                        Environment.GetFolderPath( Environment.SpecialFolder.Startup ) + "\\Jarvis.lnk" );
                }
                catch { }
            }
        }
        #endregion

        #region Initializers
        /// <summary>
        /// Runs initialization methods
        /// </summary>
        private void Initialization()
        {
            // This should be run first
            LoadConfig();
            // LoadIcons must be run before ShowNotificationIcon
            LoadIcons();
            // ShowNotificationIcon() must run after the LoadIcons() method.
            ShowNotificationIcon();
            CreateContextMenu();
            InitializeSynth();

            perfMonitor = new PerformanceMonitor();
        }

        /// <summary>
        /// Load default icons from PNG files
        /// </summary>
        private void LoadIcons()
        {
            var bmp = new Bitmap( Jarvis.Properties.Resources.heart_monitor_white );
            normalIcon = Icon.FromHandle( bmp.GetHicon() );
            bmp = new Bitmap( Jarvis.Properties.Resources.heart_monitor_red );
            alertIcon = Icon.FromHandle( bmp.GetHicon() );
            bmp.Dispose();
        }

        /// <summary>
        /// Create notification tray icon
        /// </summary>
        private void ShowNotificationIcon()
        {
            // Show notification tray icon
            notifyIcon = new NotifyIcon();
            notifyIcon.Icon = normalIcon;
            notifyIcon.Text = "You can see system monitor information here...";
            notifyIcon.Visible = true;
        }

        /// <summary>
        /// Create a context menu and assingn it to notfication tray icon
        /// </summary>
        private void CreateContextMenu()
        {
            // Create context menu and assing it to notification icon
            var progNameMenuItem = new MenuItem( String.Format( "Jarvis System Performance Monitor - v{0}",
                Assembly.GetExecutingAssembly().GetName().Version.ToString() ) );
            var spacerMenuItem = new MenuItem( "-" );
            var spacer2MenuItem = new MenuItem( "-" );
            var upTimeMenuItem = new MenuItem {
                Text = "Up Time will be shown here...",
                Name = "upTimeMenuItem"
            };
            var infoMenuItem = new MenuItem {
                Text = "Performance info will be shown here...",
                Name = "infoMenuItem"
            };
            #region Available Memory Alert Control Menus
            var memAlertControlMenuItem = new MenuItem( "Memory Alert" );
            var noMuteMemAlertMenuItem = new MenuItem {
                Text = "Don't mute memory alert",
                Name = "noMuteMemAlertMenuItem",
                RadioCheck = true,
                Checked = MemAlert == Mute.None
            };
            var muteMemAlertMenuItem = new MenuItem {
                Text = "Mute voice memory alert",
                Name = "muteMemAlertMenuItem",
                RadioCheck = true,
                Checked = MemAlert == Mute.Voice
            };
            var fullMuteMemAlertMenuItem = new MenuItem {
                Text = "Fully mute available memory messages",
                Name = "fullMuteMemAlertMenuItem",
                RadioCheck = true,
                Checked = MemAlert == Mute.Full
            };
            memAlertControlMenuItem.MenuItems.Add( noMuteMemAlertMenuItem );
            memAlertControlMenuItem.MenuItems.Add( muteMemAlertMenuItem );
            memAlertControlMenuItem.MenuItems.Add( fullMuteMemAlertMenuItem );
            #endregion
            #region CPU Load Alert Control Menus
            var cpuLoadAlertControlMenuItem = new MenuItem( "CPU Load Alert" );
            var noMuteCpuAlertMenuItem = new MenuItem {
                Text = "Don't mute CPU load alert",
                Name = "noMuteCpuAlertMenuItem",
                RadioCheck = true,
                Checked = CpuAlert == Mute.None
            };
            var muteCpuAlertMenuItem = new MenuItem {
                Text = "Mute CPU load alert",
                Name = "muteCpuAlertMenuItem",
                RadioCheck = true,
                Checked = CpuAlert == Mute.Voice
            };
            var fullMuteCpuAlertMenuItem = new MenuItem {
                Text = "Fully mute CPU load messages",
                Name = "fullMuteCpuAlertMenuItem",
                RadioCheck = true,
                Checked = CpuAlert == Mute.Full
            };
            cpuLoadAlertControlMenuItem.MenuItems.Add( noMuteCpuAlertMenuItem );
            cpuLoadAlertControlMenuItem.MenuItems.Add( muteCpuAlertMenuItem );
            cpuLoadAlertControlMenuItem.MenuItems.Add( fullMuteCpuAlertMenuItem );
            #endregion
            var startupMenuItem = new MenuItem( "Run at startup" );
            var spacer3Menuitem = new MenuItem( "-" );
            var taskManagerMenuItem = new MenuItem( "Run Task Manager" );
            var spacer4Menuitem = new MenuItem( "-" );
            var quitMenuItem = new MenuItem( "Quit" );
            var contextMenu = new ContextMenu();
            contextMenu.MenuItems.Add( progNameMenuItem );
            contextMenu.MenuItems.Add( spacerMenuItem );
            contextMenu.MenuItems.Add( upTimeMenuItem );
            contextMenu.MenuItems.Add( infoMenuItem );
            contextMenu.MenuItems.Add( spacer2MenuItem );
            contextMenu.MenuItems.Add( memAlertControlMenuItem );
            contextMenu.MenuItems.Add( cpuLoadAlertControlMenuItem );
            contextMenu.MenuItems.Add( startupMenuItem );
            contextMenu.MenuItems.Add( spacer3Menuitem );
            contextMenu.MenuItems.Add( taskManagerMenuItem );
            contextMenu.MenuItems.Add( spacer4Menuitem );
            contextMenu.MenuItems.Add( quitMenuItem );
            notifyIcon.ContextMenu = contextMenu;

            // If there is a shortcut of app in startup folder put check mark for the menu item
            startupMenuItem.Checked =
                System.IO.File.Exists(
                    Environment.GetFolderPath( Environment.SpecialFolder.Startup ) + "\\Jarvis.lnk" );

            #region Context Menu Events Wire Up
            // Wire up quit menu item to close app
            quitMenuItem.Click += QuitMenuItemClick;

            // Wire up mute menu items to change global memory mute variable
            noMuteMemAlertMenuItem.Click += NoMuteMemAlertMenuItemClick;
            muteMemAlertMenuItem.Click += MuteMemAlertMenuItemClick;
            fullMuteMemAlertMenuItem.Click += FullMuteMemAlertMenuItemClick;
            // Wire up mute menu items to change global CPU mute variable
            noMuteCpuAlertMenuItem.Click += NoMuteCpuAlertMenuItemClick;
            muteCpuAlertMenuItem.Click += MuteCpuAlertMenuItemClick;
            fullMuteCpuAlertMenuItem.Click += FullMuteCpuAlertMenuItemClick;

            // Up time menu item speaks the system up time
            upTimeMenuItem.Click += (s, e) => {
                Message( UpTimeMessage() );
            };

            // Wire up progName menu item to show an About message dialog
            progNameMenuItem.Click += (s, e) => {
                Greet();
            };

            // Wire up start up menu item to put the app in startup
            startupMenuItem.Click += StartupMenuItemClick;

            // Wire up task manager menu item to run Task Manager
            taskManagerMenuItem.Click += TaskManagerMenuItemClick;
            #endregion
        }

        /// <summary>
        /// Initilizes speech synthesizer and saves its voice gender
        /// </summary>
        private void InitializeSynth()
        {
            synth = new SpeechSynthesizer();
            // Saving voice gender for future use
            voiceGender = synth.Voice.Gender;
        }

        /// <summary>
        /// Load configuration from file
        /// </summary>
        private void LoadConfig()
        {
            config = new Configuration();
            MemAlert = config.MuteMemAlert;
            CpuAlert = config.MuteCpuAlert;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Shows and tells a welcome message;
        /// </summary>
        private void Greet()
        {
            //AssemblyName thisAssem = (typeof( Program ).Assembly).GetName();
            //Version ver = thisAssem.Version;
            string ver = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            string description = ((AssemblyDescriptionAttribute)Assembly.GetExecutingAssembly().
                GetCustomAttribute( typeof( AssemblyDescriptionAttribute ) )).Description;
            Message( String.Format( "Hello and welcome to Jarvis version {0}\n{1}",
                ver, description ) );
        }

        /// <summary>
        /// Opens the given url in Chrome (if it's installed).
        /// </summary>
        /// <param name="url"></param>
        private void OpenWebsite(string url)
        {
            var chrome = new Process();
            chrome.StartInfo.FileName = "chrome.exe";
            chrome.StartInfo.Arguments = url;
            chrome.Start();
        }

        /// <summary>
        /// Changes notification icon to alert (or normal)
        /// </summary>
        /// <param name="alert">
        /// If true (default) icon changes to red (alert) icon
        /// else to normal icon
        /// </param>
        private void ChangeNotifyIcon(bool alert = true)
        {
            notifyIcon.Icon = alert ? alertIcon : normalIcon;
        }

        /// <summary>
        /// Makes the up time message suitable for show or tell
        /// </summary>
        /// <returns></returns>
        private string UpTimeMessage()
        {
            return String.Format( "System is up for {0:0} days, {1} hours, {2} minutes and {3} seconds.",
                perfMonitor.UpTime.TotalDays, perfMonitor.UpTime.Hours, perfMonitor.UpTime.Minutes, perfMonitor.UpTime.Seconds );
        }

        /// <summary>
        /// Changes the state of radio check in all sibling menu items
        /// </summary>
        /// <param name="sender">This menu item will be checked</param>
        private static void UpdateRadioMenu(MenuItem sender)
        {
            foreach ( MenuItem item in sender.Parent.MenuItems ) {
                item.Checked = false;
            }
            sender.Checked = true;
        }

        /// <summary>
        /// Shows and tells CPU load alert message based on CpuAlert status
        /// </summary>
        private void CpuMessage()
        {
            switch ( CpuAlert ) {
                case Mute.None:
                    Message( CpuAlertMessage );
                    break;
                case Mute.Voice:
                    Message( CpuAlertMessage, mute: true );
                    break;
                case Mute.Full:
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Shows and tells CPU load alert message based on CpuAlert status.
        /// </summary>
        /// <param name="message">Sets the CpuAlertMessage property</param>
        private void CpuMessage(string message)
        {
            cpuAlertMessage = message;
            CpuMessage();
        }

        /// <summary>
        /// Shows and tells memory alert message based on MemAlert status
        /// </summary>
        private void MemoryMessage()
        {
            switch ( MemAlert ) {
                case Mute.None:
                    Message( MemoryAlertMessage );
                    break;
                case Mute.Voice:
                    Message( MemoryAlertMessage, mute: true );
                    break;
                case Mute.Full:
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Shows and tells memory alert message based on MemAlert status
        /// </summary>
        /// <param name="message">Sets the MemoryAlertMessage property</param>
        private void MemoryMessage(string message)
        {
            memoryAlertMessage = message;
            MemoryMessage();
        }


        /// <summary>
        /// Updates the configuration items at runtime
        /// </summary>
        private void UpdateConfig()
        {
            config.MuteMemAlert = MemAlert;
            config.MuteCpuAlert = CpuAlert;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Show the message in balloon tooltip at system tray
        /// and tell it to user with speech synthesizer
        /// </summary>
        /// <param name="message"></param>
        /// <param name="title"></param>
        public void Message(string message, string title = "", bool mute = false)
        {
            ShowBallonMessage( message, title );
            if ( !mute )
                synth.Speak( message );
        }

        /// <summary>
        /// Show the message in balloon tooltip at system tray
        /// </summary>
        /// <param name="message"></param>
        /// <param name="title"></param>
        public void ShowBallonMessage(string message, string title = "")
        {
            notifyIcon.BalloonTipIcon = ToolTipIcon.Info;
            notifyIcon.BalloonTipText = message;
            notifyIcon.BalloonTipTitle = title == "" ?
                "Jarvis System Performance Monitor" : title;

            notifyIcon.ShowBalloonTip( 1000 );
        }

        public void Start()
        {
            Greet();

            TellUpTime();

            systemPerformanceInfoWorker = new Thread( new ThreadStart( SystemPerformanceInfoThread ) );
            systemPerformanceInfoWorker.Start();
        }

        /// <summary>
        /// Tell and show user how much time thes system is up
        /// </summary>
        public void TellUpTime()
        {
            Message( UpTimeMessage() );
        }
        #endregion

        #region System Performance Info Thread
        /// <summary>
        /// Shows system performance info in tray icon tooltip
        /// and shows alarms at some situations
        /// </summary>
        private void SystemPerformanceInfoThread()
        {
            try {

                bool websiteOpened = false;
                bool availableMemoryAlerted = false;
                bool cpuAlerted = false;
                bool cpuAlert = false, memAlert = false;

                // Messages that will be selected randomly when CPU get hammered!
                List<string> cpuMaxedOutMessages = new List<string>();
                cpuMaxedOutMessages.Add( "WARNING: Holy crap, your CPU is about to catching fire!" );
                cpuMaxedOutMessages.Add( "WARNING: Oh my god, you should not run your CPU that hard!" );
                cpuMaxedOutMessages.Add( "WARNING: Stop it, it's burning!" );
                cpuMaxedOutMessages.Add( "WARNING: Your CPU is officially chasing squirrels!" );
                cpuMaxedOutMessages.Add( "RED ALERT! RED ALERT! RED ALERT!" );

                // The dice for selecting random message
                Random rand = new Random();

                while ( true ) {

                    // Get current perfomance counter values
                    int cpuPercentage = perfMonitor.CpuCount;
                    int availableMemory = perfMonitor.AvailableMemory;

                    // Every 1 second print the CPU load in percentage
                    // to the notification tooltip and context menu
                    string perfInfo = String.Format( "CPU Load: {0}%, Memory Available: {1} MB",
                         cpuPercentage, availableMemory );
                    notifyIcon.Text = perfInfo;
                    notifyIcon.ContextMenu.MenuItems["infoMenuItem"].Text = perfInfo;

                    // Shows system up time in context menu
                    notifyIcon.ContextMenu.MenuItems["upTimeMenuItem"].Text = UpTimeMessage();

                    #region Speak Values
                    // Speaks the cpu percentage when its above 80%
                    if ( cpuPercentage > 80 ) {
                        cpuAlert = true;
                        if ( cpuPercentage == 100 ) {
                            int rate = synth.Rate;
                            synth.Rate = 2;
                            if ( voiceGender == VoiceGender.Male ) {
                                synth.SelectVoiceByHints( VoiceGender.Female );
                            }
                            else
                                synth.SelectVoiceByHints( VoiceGender.Male );
                            CpuMessage( cpuMaxedOutMessages[rand.Next( 5 )] );
                            synth.Rate = rate;

                            if ( !websiteOpened ) {
                                websiteOpened = true;
                                OpenWebsite( "http://www.google.com/search?q=my+cpu+is+burning" );
                            }
                        }
                        else {
                            if ( !cpuAlerted ) {
                                cpuAlerted = true;
                                CpuMessage( String.Format( "The current CPU load is {0} percent.", cpuPercentage ) );
                            }
                        }
                        synth.SelectVoiceByHints( voiceGender );
                    }
                    else {
                        cpuAlert = false;
                        cpuAlerted = false;
                    }

                    // Speaks the available memory if it's under 1GB
                    if ( availableMemory < 1024 ) {
                        memAlert = true;
                        if ( !availableMemoryAlerted ) {
                            availableMemoryAlerted = true;
                            memoryAlertMessage =
                                String.Format( "The available memory is under 1GB. " +
                                                "You currently have {0} megabytes of memory available.",
                                                availableMemory );
                            MemoryMessage();
                        }
                    }
                    else {
                        availableMemoryAlerted = false;
                        memAlert = false;
                    }
                    #endregion

                    ChangeNotifyIcon( cpuAlert || memAlert );
                    Thread.Sleep( 1000 );
                } // end of while loop
            }
            catch ( ThreadAbortException tae ) {
                Dispose();
            }
        }
        #endregion

        #region Destructor
        /// <summary>
        /// Clears notification tray icon, disposes performance monitor
        /// and synthesizer and calls garbage collector
        /// </summary>
        public void Dispose()
        {
            UpdateConfig();
            config.Save();
            perfMonitor.Dispose();
            synth.Dispose();
            notifyIcon.Dispose();
            GC.SuppressFinalize( this );
            systemPerformanceInfoWorker.Abort();
        }
        #endregion
    }
}
