using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace breaklee_file_check.Class
{
    internal class Files
    {
        public byte FolderIndex { get; set; }
        public string Name { get; set; }
        public byte Unk1 { get; set; }
        public string Hash { get; set; }
        public byte Unk2 { get; set; }
        public int Size => 1 + 60 + 1 + 32 + 1; // folder_index + name(60) + unk1 + hash(32) + unk2

        // Constructor to read from BinaryReader
        public Files(BinaryReader reader)
        {
            FolderIndex = reader.ReadByte();          // 1 byte for folder_index
            byte[] nameBytes = reader.ReadBytes(60);  // 60 bytes for the name
            Name = Encoding.ASCII.GetString(nameBytes).TrimEnd('\0');
            Unk1 = reader.ReadByte();                 // 1 byte for unk1
            byte[] hashBytes = reader.ReadBytes(32);  // 32 bytes for the hash
            Hash = Encoding.ASCII.GetString(hashBytes).TrimEnd('\0');
            Unk2 = reader.ReadByte();                 // 1 byte for unk2
        }

        public string FileNameRead()
        {
            return FolderIndex == 0 ? "MagicKey" : Name;
        }

        public void FileHashWrite(string hashValue)
        {
            Hash = hashValue;
        }
    }
}
