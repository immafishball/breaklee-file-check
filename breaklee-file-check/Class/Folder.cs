using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace breaklee_file_check.Class
{
    internal class Folder
    {
        public string Name { get; set; }

        // Constructor to read from BinaryReader
        public Folder(BinaryReader reader)
        {
            byte[] nameBytes = reader.ReadBytes(260); // 260 bytes for the name
            Name = Encoding.ASCII.GetString(nameBytes).TrimEnd('\0');
        }

        public string FolderName()
        {
            return Name;
        }

        // Size of the folder structure in bytes
        public int Size => 260; // Only name field
    }
}
