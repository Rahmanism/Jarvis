using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jarvis
{
    /// <summary>
    /// Perform some operations on Json items, i.e: save, load, etc.
    /// </summary>
    class JsonFile
    {
        #region Types
        /// <summary>
        /// Every item has a name and a value
        /// </summary>
        public struct Item
        {
            public string Name;
            public string Value;
        }
        #endregion

        #region Fields
        /// <summary>
        /// List of all items
        /// </summary>
        private List<Item> Items;

        /// <summary>
        /// Path of Json file
        /// </summary>
        private string filePath { get; set; }
        #endregion

        #region Constructor
        /// <summary>
        /// Constructor: Set the value of filePath using the parameter,
        /// create the file if doesn't exist,
        /// load items from file
        /// </summary>
        /// <param name="path">Path of file that contains items
        /// and changes will be saved in it.</param>
        public JsonFile(string path)
        {
            filePath = path;
            try {
                // Create file if it doesn't exist already
                if ( !File.Exists( filePath ) )
                    File.CreateText( filePath );

                // Read json items to the list
                Items = JsonConvert.
                    DeserializeObject<List<Item>>
                    ( File.ReadAllText( filePath ) );
            }
            catch {
                throw;
            }
        }
        #endregion

        #region Methods
        /// <summary>
        /// Return an item from Json file
        /// </summary>
        /// <param name="name">Search the name to find the corresponding item</param>
        /// <returns>The found item, or and item with null values</returns>
        public Item Load(string name)
        {
            var item = new Item();
            // If there is any item in the list then search
            if ( Items != null && Items.Count > 0 )
                item = Items.FirstOrDefault( i => i.Name == name );
            return item;
        }

        /// <summary>
        /// Change (or add) an item to the Items list
        /// </summary>
        /// <param name="item">The item if already exists gets update,
        /// otherwise will be added to the items list</param>
        public void Change(Item item)
        {
            // Check if the item already exists in the list
            int index;
            if ( Items == null ) {
                Items = new List<Item>();
                index = -1;
            }
            else
                index = Items.IndexOf( Items.FirstOrDefault( i => i.Name == item.Name ) );
            if ( index == -1 ) { // items does not exists
                Items.Add( item );
            }
            else {
                Items[index] = item;
            }
        }

        /// <summary>
        /// Save all the changes to the file
        /// </summary>
        /// <returns>Returns true if save to file operation be successful</returns>
        public bool Save()
        {
            if ( Items == null )
                return false;
            try {
                File.WriteAllText( filePath, JsonConvert.SerializeObject( Items ) );
            }
            catch {
                throw;
            }

            return true;
        }
        #endregion
    }
}
