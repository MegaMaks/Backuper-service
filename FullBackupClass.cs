using Alphaleonis.Win32.Vss;
using BytesRoad.Net.Ftp;
using Ionic.Zip;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace ServiceBack
{
    class FullBackupClass
    {
        BackuperDAL BDAL;
        FullTask FTask;
        DateTime CurrentDate { get; } = DateTime.Now;

        public FullBackupClass(FullTask ftask)
        {
            BDAL = new BackuperDAL();
            FTask = new FullTask();
            FTask = ftask;
      }



        public void UpdateSosttask(int idsost)
        {
            try
            {
                BDAL.OpenConnection();
                BDAL.EditFullTaskSost(FTask.Idtask, idsost, FTask.Nextstart);
                BDAL.CloseConnection();                
            }
            catch
            {

            }
        }


        public void DeleteCopyOfTime()
        {
            string[] files = null;
            TimeSpan TS = new TimeSpan(FTask.Timelive * 7, 0, 0, 0);
            DateTime datedelete = DateTime.Now - TS;
            files = Directory.GetFiles(FTask.Dest);

            foreach (string file in files)
            {
                try
                {


                    FileInfo fi = new FileInfo(file);
                    if ((fi.LastWriteTime < datedelete) && (fi.Name.Contains(FTask.Taskname)))
                    {
                        fi.Delete();
                    }
                }
                catch (FileNotFoundException e)
                {
                    try
                    {
                        BDAL.OpenConnection();
                        BDAL.AddFullLog(e.ToString(),CurrentDate,FTask.Idtask,0);
                        BDAL.CloseConnection();
                        continue;
                    }
                    catch
                    {

                    }
                }
            }
        }


        public StringWriter CheckLongPath(string root)
        {
            StringWriter LongPath = new StringWriter();
            Stack<string> dirs = new Stack<string>(20);

            if (!Directory.Exists(root))
            {
                throw new ArgumentException();
            }
            dirs.Push(root);

            while (dirs.Count > 0)
            {
                string currentDir = dirs.Pop();
                string[] subDirs;
                try
                {
                    subDirs = Directory.GetDirectories(currentDir);
                }

                catch (UnauthorizedAccessException e)
                {
                    LongPath.WriteLine("Нет доступа к файлу или каталогу: " + currentDir);
                    continue;
                }
                catch (DirectoryNotFoundException e)
                {
                    LongPath.WriteLine("Не найден файл или каталог: " + currentDir);
                    continue;
                }
                catch (PathTooLongException e)
                {
                    LongPath.WriteLine("Слишком длинный путь или имя файла: " + currentDir);
                    continue;
                }


                string[] files = null;
                try
                {
                    files = Directory.GetFiles(currentDir);
                }

                catch (UnauthorizedAccessException e)
                {

                    LongPath.WriteLine(e.Message);
                    continue;
                }

                catch (DirectoryNotFoundException e)
                {
                    LongPath.WriteLine(e.Message);
                    continue;
                }
                foreach (string file in files)
                {
                    try
                    {

                        FileInfo fi = new FileInfo(file);
                        fi.LastWriteTimeUtc.ToShortDateString();

                    }
                    catch (FileNotFoundException e)
                    {

                        LongPath.WriteLine(e.Message);
                        continue;
                    }

                    catch (PathTooLongException e)
                    {
                        LongPath.WriteLine("Слишком длинный путь или имя файла: " + file);
                        continue;

                    }

                }


                foreach (string str in subDirs)
                {
                    dirs.Push(str);
                }
            }
            return LongPath;

        }

        public static string Decrypt(string cipherText, string password, string salt = "Kosher", string hashAlgorithm = "SHA1", int passwordIterations = 2, string initialVector = "OFRna73m*aze01xY", int keySize = 256)
        {
            if (string.IsNullOrEmpty(cipherText))
                return "";

            byte[] initialVectorBytes = Encoding.ASCII.GetBytes(initialVector);
            byte[] saltValueBytes = Encoding.ASCII.GetBytes(salt);
            byte[] cipherTextBytes = Convert.FromBase64String(cipherText);

            PasswordDeriveBytes derivedPassword = new PasswordDeriveBytes(password, saltValueBytes, hashAlgorithm, passwordIterations);
            byte[] keyBytes = derivedPassword.GetBytes(keySize / 8);

            RijndaelManaged symmetricKey = new RijndaelManaged();
            symmetricKey.Mode = CipherMode.CBC;

            byte[] plainTextBytes = new byte[cipherTextBytes.Length];
            int byteCount = 0;

            using (ICryptoTransform decryptor = symmetricKey.CreateDecryptor(keyBytes, initialVectorBytes))
            {
                using (MemoryStream memStream = new MemoryStream(cipherTextBytes))
                {
                    using (CryptoStream cryptoStream = new CryptoStream(memStream, decryptor, CryptoStreamMode.Read))
                    {
                        byteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);
                        memStream.Close();
                        cryptoStream.Close();
                    }
                }
            }

            symmetricKey.Clear();
            return Encoding.UTF8.GetString(plainTextBytes, 0, byteCount);
        }

        public void StartFullBackup()
        {
            UpdateSosttask(2);
            IVssBackupComponents backup = null;
            string nameDriver = "";
            Boolean successTask = true;
            try
            {


                if (FTask.Shadow == 1)
                {
                    string[] mass_drives = { "A:\\", "B:\\", "D:\\", "E:\\", "I:\\", "K:\\", "L:\\", "M:\\", "N:\\", "O:\\", "P:\\" };
                    string have_driver = "";
                    DriveInfo[] drives = DriveInfo.GetDrives();
                    foreach (DriveInfo d in drives)
                    {
                        have_driver = have_driver + d.Name;
                    }
                    foreach (string str_driver in mass_drives)
                    {
                        if (!have_driver.Contains(str_driver))
                        {
                            nameDriver = str_driver;
                        }
                    }

                    FileInfo MyFileInfo = new FileInfo(FTask.Source);
                    String _Volume = MyFileInfo.Directory.Root.Name;

                    IVssImplementation _vssImplementation = VssUtils.LoadImplementation();
                    backup = _vssImplementation.CreateVssBackupComponents();
                    backup.InitializeForBackup(null);

                    backup.GatherWriterMetadata();

                    // VSS step 3: VSS Configuration
                    backup.SetContext(VssVolumeSnapshotAttributes.Persistent | VssVolumeSnapshotAttributes.NoAutoRelease);
                    backup.SetBackupState(false, true, Alphaleonis.Win32.Vss.VssBackupType.Full, false);

                    Guid MyGuid01 = backup.StartSnapshotSet();
                    Guid MyGuid02 = backup.AddToSnapshotSet(_Volume, Guid.Empty);

                    backup.PrepareForBackup();
                    // VSS step 6: Create a Snapshot For each volume in the "Snapshot Set"
                    backup.DoSnapshotSet();

                    backup.ExposeSnapshot(MyGuid02, null, VssVolumeSnapshotAttributes.ExposedLocally, nameDriver.Remove(2, 1));

                    FTask.Source = FTask.Source.Replace(_Volume, nameDriver);

                }
                StringWriter SW = new StringWriter();
                StringWriter Skip_file_log = new StringWriter();
                string time_today = DateTime.Now.Date.ToShortDateString();
                ZipFile zf = new ZipFile(FTask.Dest + "\\" + FTask.Taskname + "_" + time_today.Replace(".", "-") + "." + FTask.Extension);
                zf.ProvisionalAlternateEncoding = Encoding.GetEncoding("cp866");

                if (FTask.Password != "no")
                {
                    zf.Password = FTask.Password;
                }
                zf.UseZip64WhenSaving = Zip64Option.AsNecessary;
                zf.ZipErrorAction = ZipErrorAction.Skip;
                zf.StatusMessageTextWriter = Skip_file_log;


                //var files = Directory.GetFiles(source, "*", SearchOption.AllDirectories).Where(m => (LongPath.ToString().Contains(Delimon.Win32.IO.Path.GetNormalPath(m)) != true) && (exeption.Contains(Delimon.Win32.IO.Path.GetNormalPath(m)) != true)).ToArray();
                string[] mass_exeption;
                string[] separator = { Environment.NewLine };
                mass_exeption = FTask.Exeption.Split(separator, StringSplitOptions.RemoveEmptyEntries);
                Stack<string> dirs = new Stack<string>(20);
                string root = FTask.Source;

                if (!Directory.Exists(root))
                {
                    throw new ArgumentException();
                }
                dirs.Push(root);


                while (dirs.Count > 0)
                {
                    string currentDir = dirs.Pop();
                    string[] subDirs;
                    try
                    {
                        subDirs = Directory.GetDirectories(currentDir);
                    }

                    catch (UnauthorizedAccessException e)
                    {
                        SW.WriteLine("Нет доступа к файлу или каталогу: " + currentDir);
                        successTask = false;
                        continue;
                    }
                    catch (DirectoryNotFoundException e)
                    {
                        SW.WriteLine("Не найден файл или каталог: " + currentDir);
                        successTask = false;
                        continue;
                    }
                    catch (PathTooLongException e)
                    {
                        SW.WriteLine("Слишком длинный путь или имя файла: " + currentDir);
                        successTask = false;
                        continue;
                    }


                    string[] files = null;
                    try
                    {
                        files = Directory.GetFiles(currentDir);
                    }

                    catch (UnauthorizedAccessException e)
                    {

                        SW.WriteLine(e.Message);
                        successTask = false;
                        continue;
                    }

                    catch (DirectoryNotFoundException e)
                    {
                        SW.WriteLine(e.Message);
                        successTask = false;
                        continue;
                    }

                    if (FTask.Exeption == "no")
                    {
                        foreach (string file in files)
                        {
                            try
                            {
                                zf.AddFile(file, Path.GetDirectoryName(file).Replace(FTask.Source, string.Empty));
                            }
                            catch (FileNotFoundException e)
                            {

                                SW.WriteLine("Не найден файл: " + file);
                                successTask = false;
                                continue;
                            }


                            catch (PathTooLongException e)
                            {
                                SW.WriteLine("Слишком длинный путь или имя файла: " + file);
                                successTask = false;
                                continue;

                            }

                        }
                    }
                    else
                    {
                        int exclude_flag = 0;
                        foreach (string file in files)
                        {
                            try
                            {
                                exclude_flag = 0;
                                foreach (var exep in mass_exeption)
                                {
                                    if (Path.GetFullPath(file).Contains(exep))
                                    {
                                        exclude_flag = 1;
                                        break;
                                    }
                                }
                                if (exclude_flag == 0)
                                {
                                    zf.AddFile(file, Path.GetDirectoryName(file).Replace(FTask.Source, string.Empty));
                                }


                            }
                            catch (FileNotFoundException e)
                            {

                                SW.WriteLine("Не найден файл: " + file);
                                successTask = false;
                                continue;
                            }

                            catch (PathTooLongException e)
                            {
                                SW.WriteLine("Слишком длинный путь или имя файла: " + file);
                                successTask = false;
                                continue;

                            }

                        }
                    }


                    foreach (string str in subDirs)
                    {
                        dirs.Push(str);
                    }
                }

                zf.Save();
                zf.Dispose();
                if (successTask)
                {
                    UpdateSosttask(4);
                }
                else
                {
                    UpdateSosttask(5);
                }

                if (FTask.Shadow == 1)
                {
                    foreach (VssSnapshotProperties prop in backup.QuerySnapshots())
                    {
                        if (prop.ExposedName == nameDriver)
                        {
                            backup.DeleteSnapshot(prop.SnapshotId, true);
                        }
                    }
                    backup = null;
                }


                //Добавлено 3.04.2017
                string strLine;
                int countSkip = 0;
                StringReader stringreader = new StringReader(SW.ToString());
                while (true)
                {

                    strLine = stringreader.ReadLine();
                    if (strLine != null)
                    {
                        countSkip++;
                        BDAL.OpenConnection();
                        BDAL.AddFullLog(strLine, CurrentDate, FTask.Idtask, 0);
                        BDAL.CloseConnection();
                    }
                    else
                    {
                        break;
                    }
                }

                string strLine2;
                StringReader stringreader2 = new StringReader(Skip_file_log.ToString());
                while (true)
                {

                    strLine2 = stringreader2.ReadLine();
                    if (strLine2 != null)
                    {
                        if (strLine2.Contains("Skipping"))
                        {
                            countSkip++;
                            BDAL.OpenConnection();
                            BDAL.AddFullLog(strLine2, CurrentDate, FTask.Idtask, 0);
                            BDAL.CloseConnection();
                        }
                    }
                    else
                    {
                        break;
                    }
                }


                if (countSkip > 0)
                {
                    UpdateSosttask(5);
                }
                string msg = "Завершено успешно";
                BDAL.OpenConnection();
                BDAL.AddFullLog(msg, CurrentDate, FTask.Idtask, 1);
                BDAL.CloseConnection();
                DeleteCopyOfTime();

                if (FTask.Ftp == 1)
                {
                    try
                    {
                        UpdateSosttask(2);
                        FtpClient client = new FtpClient();
                        StreamReader sr = File.OpenText(AppDomain.CurrentDomain.BaseDirectory + "ftp_sett.ini");
                        client.PassiveMode = true; //Включаем пассивный режим.
                        int TimeoutFTP = 30000; //Таймаут.
                        string FTP_SERVER = sr.ReadLine();
                        int FTP_PORT = Convert.ToInt32(sr.ReadLine());
                        string FTP_USER = sr.ReadLine();
                        string FTP_PASSWORD = Decrypt(sr.ReadLine(), "OFRna73");
                        string FTP_FOLDER = sr.ReadLine();


                        client.Connect(TimeoutFTP, FTP_SERVER, FTP_PORT);
                        client.Login(TimeoutFTP, FTP_USER, FTP_PASSWORD);
                        if (FTP_FOLDER != "not_folder")
                        {
                            client.ChangeDirectory(TimeoutFTP, FTP_FOLDER);
                        }
                        client.PutFile(TimeoutFTP, zf.Name.Substring(zf.Name.IndexOf(FTask.Taskname)), zf.Name);
                        sr.Close();
                        client.Disconnect(TimeoutFTP);

                        msg= "Копирование архива на FTP сервер завершено успешно";

                        BDAL.OpenConnection();
                        BDAL.AddFullLog(msg, CurrentDate, FTask.Idtask, 1);
                        BDAL.CloseConnection();
                        UpdateSosttask(4);
                    }
                    catch (Exception ex)
                    {
                        BDAL.OpenConnection();
                        BDAL.AddFullLog(ex.ToString(), CurrentDate, FTask.Idtask, 0);
                        BDAL.CloseConnection();
                        UpdateSosttask(3);
                    }
                }


            }
            catch (Exception ex)
            {
                try
                {
                    BDAL.OpenConnection();
                    BDAL.AddFullLog(ex.ToString(), CurrentDate, FTask.Idtask, 0);
                    BDAL.CloseConnection();
                    UpdateSosttask(3);

                    try
                    {
                        if (FTask.Shadow == 1)
                        {
                            foreach (VssSnapshotProperties prop in backup.QuerySnapshots())
                            {
                                if (prop.ExposedName == nameDriver)
                                {
                                    backup.DeleteSnapshot(prop.SnapshotId, true);
                                }
                            }
                            backup = null;
                        }
                    }
                    catch
                    {

                    }

                }
                catch
                {

                }
            }

        }
    }
}
