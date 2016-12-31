using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis
{
    class Configuration
    {
        #region Constants
        const string ConfigFilePath = "config.json";
        #endregion

        #region Types
        public enum Mute :
            byte { None, Voice, Full = 255 };
        #endregion

        #region Config Items
        public Mute MuteMemAlert;
        public Mute MuteCpuAlert;
        #endregion

        #region Fields
        private JsonFile jsonFile;
        #endregion

        #region Constructor
        /// <summary>
        /// Load json configuration items from file to properties
        /// </summary>
        public Configuration()
        {
            // Load config file
            jsonFile = new JsonFile( ConfigFilePath );

            // Load items from file and initialize if they're null
            JsonFile.Item muteMemAlertItem = jsonFile.Load( "MuteMemAlert" );
            muteMemAlertItem.Value = muteMemAlertItem.Value ?? Mute.None.ToString();
            JsonFile.Item muteCpuAlertItem = jsonFile.Load( "MuteCpuAlert" );
            muteCpuAlertItem.Value = muteCpuAlertItem.Value ?? Mute.None.ToString();

            // Load values to runtime properties
            Enum.TryParse( muteMemAlertItem.Value, out MuteMemAlert );
            Enum.TryParse( muteCpuAlertItem.Value, out MuteCpuAlert );
        }
        #endregion

        #region Methods
        /// <summary>
        /// Save changes to json file on hard disk
        /// </summary>
        public void Save()
        {
            JsonFile.Item item = new JsonFile.Item();

            // Update memory mute status
            item.Name = "MuteMemAlert";
            item.Value = MuteMemAlert.ToString();
            jsonFile.Change( item );

            // Update cpu mute status
            item.Name = "MuteCpuAlert";
            item.Value = MuteCpuAlert.ToString();
            jsonFile.Change( item );

            // Save to file
            jsonFile.Save();
        }
        #endregion
    }
}
