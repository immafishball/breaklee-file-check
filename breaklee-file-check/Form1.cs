using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Security.Policy;
using System.Text;
using System.Windows.Forms;

namespace breaklee_file_check
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        // Folder class to represent the Folder structure
        class Folder
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

        // File class to represent the File structure
        class Files
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

        private void button1_Click(object sender, EventArgs e)
        {
            // Create a DataTable to hold the folder and file data
            DataTable table = new DataTable();

            // Define the columns with appropriate types
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Value", typeof(string));
            table.Columns.Add("Start", typeof(string)); // Start position in hex
            table.Columns.Add("Size", typeof(string));  // Size in hex
            table.Columns.Add("Hash", typeof(string));  // Hash can remain as string

            string filePath = "xdata.dec";

            // Use a FileStream to read data from the file
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            using (BinaryReader reader = new BinaryReader(fs))
            {
                long currentPosition = reader.BaseStream.Position;

                // Read folder count from the start of the file
                int foldersCount = reader.ReadInt32(); // Read 4 bytes for folder count

                // Adding the folders count to the DataTable
                table.Rows.Add("int folders", foldersCount.ToString(), currentPosition.ToString("X") + "h", "4h", "N/A");

                currentPosition += 4; // Update position after reading folder count

                // Read folders
                Folder[] folders = new Folder[foldersCount];
                for (int i = 0; i < foldersCount; i++)
                {
                    folders[i] = new Folder(reader);
                    long folderStart = currentPosition; // Start position of this folder
                    table.Rows.Add($"struct Folder folder[{i}]", folders[i].FolderName(), folderStart.ToString("X") + "h", folders[i].Size.ToString("X") + "h"); // Size of folder name is 260 bytes
                    currentPosition += 260; // Update position after reading folder
                }

                // Read file count from the position after folders
                int filesCount = reader.ReadInt32(); // Read 4 bytes for file count

                // Adding the file count to the DataTable
                table.Rows.Add("int files", filesCount.ToString(), currentPosition.ToString("X") + "h", "4h", "N/A");

                currentPosition += 4; // Update position after reading file count

                // Read files
                Files[] files = new Files[filesCount];
                for (int i = 0; i < filesCount; i++)
                {
                    files[i] = new Files(reader);
                    long fileStart = currentPosition; // Start position of this file
                    table.Rows.Add($"struct File file[{i}]", files[i].FileNameRead(), fileStart.ToString("X") + "h", files[i].Size.ToString("X") + "h", files[i].Hash); // Size of file structure is 94 bytes (1 + 60 + 1 + 32 + 1)
                    currentPosition += 95; // Update position after reading file
                }
            }

            // Bind the DataTable to the DataGridView
            dataGridView1.DataSource = table;

            // Optional: Customize DataGridView appearance
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridView1.AllowUserToAddRows = false; // Disable adding new rows by user
        }

        private void RemoveEncFiles()
        {
            string filePath = "xdata.dec";

            // Read all data from the file
            byte[] fileData;
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                fileData = new byte[fs.Length];
                fs.Read(fileData, 0, fileData.Length);
            }

            using (BinaryReader reader = new BinaryReader(new MemoryStream(fileData)))
            {
                // Read the folder count
                int foldersCount = reader.ReadInt32();

                // Read folders (not modified, since they don't end with ".enc")
                Folder[] folders = new Folder[foldersCount];
                for (int i = 0; i < foldersCount; i++)
                {
                    folders[i] = new Folder(reader);
                }

                // Read the file count
                int filesCount = reader.ReadInt32();
                List<Files> files = new List<Files>();

                // Read files and filter those that end with ".enc"
                for (int i = 0; i < filesCount; i++)
                {
                    Files file = new Files(reader);
                    if (!file.Name.EndsWith(".enc") &&
                        !file.Name.EndsWith("ui.dts") &&
                        !file.Name.EndsWith("ui.dat") &&
                        !file.Name.EndsWith("logo.dds") &&
                        !file.Name.EndsWith("logo_company.dds"))
                    {
                        files.Add(file);
                    }
                }

                // Update the file count
                int updatedFileCount = files.Count;

                // Create a new memory stream to write the updated data
                using (MemoryStream ms = new MemoryStream())
                using (BinaryWriter writer = new BinaryWriter(ms))
                {
                    // Write the original folder count
                    writer.Write(foldersCount);

                    // Write folders back
                    foreach (var folder in folders)
                    {
                        writer.Write(Encoding.ASCII.GetBytes(folder.Name.PadRight(260, '\0')));
                    }

                    // Write the updated file count
                    writer.Write(updatedFileCount);

                    // Write the remaining files
                    foreach (var file in files)
                    {
                        writer.Write(file.FolderIndex);
                        writer.Write(Encoding.ASCII.GetBytes(file.Name.PadRight(60, '\0')));
                        writer.Write(file.Unk1);
                        writer.Write(Encoding.ASCII.GetBytes(file.Hash.PadRight(32, '\0')));
                        writer.Write(file.Unk2);
                    }

                    // Save the updated data back to the original file
                    File.WriteAllBytes(filePath, ms.ToArray());
                }

                // Show confirmation
                MessageBox.Show($"{filesCount - updatedFileCount} files have been removed. Updated count: {updatedFileCount}");

                // Update the DataGridView after removing the .enc files
                UpdateDataTableAfterRemoval(folders, files);
            }
        }
        private string CalculateMagicKey(string hash)
        {
            // Ensure the hash is valid and has an even length
            if (string.IsNullOrEmpty(hash) || hash.Length % 2 != 0)
            {
                throw new ArgumentException("Invalid hash. It must be a non-empty string with an even length.");
            }

            // Cut the hash in half
            int halfLength = hash.Length / 2;
            string firstHalf = hash.Substring(0, halfLength);

            // Extract odd-indexed characters from the first half
            StringBuilder magicKey = new StringBuilder();
            for (int i = 1; i < firstHalf.Length; i += 2) // Start at index 1 and step by 2
            {
                magicKey.Append(firstHalf[i]);
            }

            return magicKey.ToString();
        }

        private void UpdateDataTableAfterRemoval(Folder[] folders, List<Files> files)
        {
            // Create a DataTable to hold the folder and file data
            DataTable table = new DataTable();

            // Define the columns with appropriate types
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Value", typeof(string));
            table.Columns.Add("Start", typeof(string)); // Start position in hex
            table.Columns.Add("Size", typeof(string));  // Size in hex
            table.Columns.Add("Hash", typeof(string));  // Hash can remain as string

            long currentPosition = 4 + (folders.Length * 260); // Starting position after folder count and folders

            // Adding the folders count to the DataTable
            table.Rows.Add("int folders", folders.Length.ToString(), "0h", "4h", "N/A");

            // Read folders and add to the DataTable
            for (int i = 0; i < folders.Length; i++)
            {
                long folderStart = 4 + (i * 260); // Start position of this folder
                table.Rows.Add($"struct Folder folder[{i}]", folders[i].FolderName(), folderStart.ToString("X") + "h", folders[i].Size.ToString("X") + "h");
            }

            // Adding the file count to the DataTable
            table.Rows.Add("int files", files.Count.ToString(), currentPosition.ToString("X") + "h", "4h", "N/A");

            currentPosition += 4; // Update position after reading file count

            // Read files and add to the DataTable
            for (int i = 0; i < files.Count; i++)
            {
                long fileStart = currentPosition; // Start position of this file
                table.Rows.Add($"struct File file[{i}]", files[i].FileNameRead(), fileStart.ToString("X") + "h", files[i].Size.ToString("X") + "h", files[i].Hash);
                currentPosition += files[i].Size; // Update position after reading file
            }

            // Bind the updated DataTable to the DataGridView
            dataGridView1.DataSource = table;

            // Optional: Customize DataGridView appearance
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridView1.AllowUserToAddRows = false; // Disable adding new rows by user
        }

        private void button2_Click(object sender, EventArgs e)
        {
            RemoveEncFiles();
        }

        private string CalculateMagicKey()
        {
            // Variable to hold the MagicKey hash
            string magicKeyHash = string.Empty;

            // Iterate through the DataGridView to find the MagicKey entry
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                // Assuming the first column (index 0) contains the name
                if (row.Cells[1].Value.ToString() == "MagicKey")
                {
                    magicKeyHash = row.Cells[4].Value.ToString(); // Assuming the hash is in the 5th column (index 4)
                    break;
                }
            }

            // Check if we found the MagicKey hash
            if (string.IsNullOrEmpty(magicKeyHash))
            {
                MessageBox.Show("MagicKey hash not found.");
                return string.Empty;
            }

            // Cut the hash in half
            int halfLength = magicKeyHash.Length / 2;
            string firstHalf = magicKeyHash.Substring(0, halfLength + 2);

            // Extract the odd-indexed characters from the first half
            StringBuilder magicKey = new StringBuilder();
            for (int i = 0; i < firstHalf.Length; i += 2) // Start from index 1 to get odd indices
            {
                magicKey.Append(firstHalf[i]);
            }

            return magicKey.ToString();
        }


        private void button3_Click(object sender, EventArgs e)
        {
            string magicKey = CalculateMagicKey();
            if (!string.IsNullOrEmpty(magicKey))
            {
                MessageBox.Show($"ClientMagicKey: {magicKey}");
                Clipboard.SetText( magicKey );
            }
        }
    }
}
