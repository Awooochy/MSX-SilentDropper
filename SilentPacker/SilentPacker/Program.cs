using System;
using System.IO;
using System.Text;
using System.Windows.Forms;

class Program
{
    [STAThread] // Required for OpenFileDialog to work
    static void Main()
    {
        OpenFileDialog openFileDialog = new OpenFileDialog();
        openFileDialog.Title = "Select binary file";

        if (openFileDialog.ShowDialog() == DialogResult.OK)
        {
            string inputFile = openFileDialog.FileName;

            // Check if the file exists
            if (!File.Exists(inputFile))
            {
                Console.WriteLine("File not found.");
                return;
            }

            // Read the binary file into a byte array
            byte[] fileBytes = File.ReadAllBytes(inputFile);

            // Convert the byte array to a base64 string
            string base64String = Convert.ToBase64String(fileBytes);

            // Generate a random XOR key with only letters and numbers
            string xorKey = GenerateRandomKey();

            // Encode the base64 string with XOR
            byte[] encodedBytes = XorEncode(Encoding.UTF8.GetBytes(base64String), Encoding.UTF8.GetBytes(xorKey));

            // Get the input file name without extension
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(inputFile);

            // Output the XOR key to a text file
            string keyFilePath = $"{fileNameWithoutExtension}_key.txt";
            File.WriteAllText(keyFilePath, xorKey);

            // Output the encoded data to a .awo file
            string encodedFilePath = $"{fileNameWithoutExtension}.awo";
            File.WriteAllBytes(encodedFilePath, encodedBytes);

            Console.WriteLine("Encoding completed. Key saved to {0}, Encoded data saved to {1}", keyFilePath, encodedFilePath);
        }
    }

    static byte[] XorEncode(byte[] data, byte[] key)
    {
        byte[] encodedData = new byte[data.Length];

        for (int i = 0; i < data.Length; i++)
        {
            encodedData[i] = (byte)(data[i] ^ key[i % key.Length]);
        }

        return encodedData;
    }

    static string GenerateRandomKey()
    {
        const string validChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        StringBuilder keyBuilder = new StringBuilder();
        Random random = new Random();

        for (int i = 0; i < 32; i++) // You can adjust the length of the key as needed
        {
            keyBuilder.Append(validChars[random.Next(validChars.Length)]);
        }

        return keyBuilder.ToString();
    }
}
