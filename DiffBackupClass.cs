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
    class DiffBackupClass
    {
        BackuperDAL BDAL;
        DiffTask DTask;
        DateTime CurrentDate { get; } = DateTime.Now;
        public DiffBackupClass(DiffTask dtask)
        {
            BDAL = new BackuperDAL();
            DTask = dtask;

        }

        DataTable t;


        public void UpdateSosttask(int idsost)
        {
            try
            {
                BDAL.OpenConnection();
                BDAL.EditDiffTaskSost(DTask.Idtask, idsost, DTask.Nextstart);
                BDAL.CloseConnection();
            }
            catch
            {

            }
        }

        public void DeleteDimeLive()
        {
            string[] files = null;
            TimeSpan TS = new TimeSpan(DTask.Timelive * 7, 0, 0, 0);
            DateTime datedelete = CurrentDate - TS;
            files = Directory.GetFiles(DTask.Destroot);

            foreach (string file in files)
            {
                try
                {


                    FileInfo fi = new FileInfo(file);

                    if ((fi.LastWriteTime < datedelete) && (fi.Name.Contains(DTask.Taskname)))
                    {
                        fi.Delete();
                    }
                }
                catch (FileNotFoundException ex)
                {
                    BDAL.OpenConnection();
                    BDAL.AddDiffLog(ex.ToString(), CurrentDate, DTask.Idtask, 0);
                    BDAL.CloseConnection();
                }
            }
        }


        //edit 05.04.2017
        public void StartDiffBackup()
        {
            UpdateSosttask(2);
            IVssBackupComponents backup = null;
            string namedriver = "";
            Boolean successtask = true;
            try
            {


                if (DTask.Shadow == 1)
                {
                    string[] massdrives = { "A:\\", "B:\\", "D:\\", "E:\\", "I:\\", "K:\\", "L:\\", "M:\\", "N:\\", "O:\\", "P:\\" };
                    string havedriver = "";
                    DriveInfo[] drives = DriveInfo.GetDrives();
                    foreach (DriveInfo d in drives)
                    {
                        havedriver = havedriver + d.Name;
                    }
                    foreach (string strdriver in massdrives)
                    {
                        if (!havedriver.Contains(strdriver))
                        {
                            namedriver = strdriver;
                        }
                    }

                    FileInfo MyFileInfo = new FileInfo(DTask.Source);
                    String _Volume = MyFileInfo.Directory.Root.Name;

                    IVssImplementation _vssImplementation = VssUtils.LoadImplementation();
                    backup = _vssImplementation.CreateVssBackupComponents();
                    backup.InitializeForBackup(null);
 
                    backup.GatherWriterMetadata();

                    backup.SetContext(VssVolumeSnapshotAttributes.Persistent | VssVolumeSnapshotAttributes.NoAutoRelease);
                    backup.SetBackupState(false, true, Alphaleonis.Win32.Vss.VssBackupType.Full, false);

                    Guid MyGuid01 = backup.StartSnapshotSet();
                    Guid MyGuid02 = backup.AddToSnapshotSet(_Volume, Guid.Empty);

                    backup.PrepareForBackup();
                    backup.DoSnapshotSet();

                    backup.ExposeSnapshot(MyGuid02, null, VssVolumeSnapshotAttributes.ExposedLocally, namedriver.Remove(2, 1));

                    DTask.Source = DTask.Source.Replace(_Volume, namedriver);

                }

                StringWriter SW = new StringWriter();
                StringWriter Skip_file_log = new StringWriter();
                string time_today = DateTime.Now.Date.ToShortDateString();
                ZipFile zf = new ZipFile(DTask.Dest + "\\" + DTask.Taskname + "_" + time_today.Replace(".", "-") + "." + DTask.Extension);
                zf.ProvisionalAlternateEncoding = Encoding.GetEncoding("cp866");

                if (DTask.Password != "no")
                {
                    zf.Password = DTask.Password;
                }
                zf.UseZip64WhenSaving = Zip64Option.AsNecessary;
                zf.ZipErrorAction = ZipErrorAction.Skip;
                zf.StatusMessageTextWriter = Skip_file_log;

                string[] massexeption;
                string[] separator = { Environment.NewLine };
                massexeption = DTask.Exeption.Split(separator, StringSplitOptions.RemoveEmptyEntries);
                Stack<string> dirs = new Stack<string>(20);
                string root = DTask.Exeption;

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
                        successtask = false;
                        continue;
                    }
                    catch (DirectoryNotFoundException e)
                    {
                        SW.WriteLine("Не найден файл или каталог: " + currentDir);
                        successtask = false;
                        continue;
                    }
                    catch (PathTooLongException e)
                    {
                        SW.WriteLine("Слишком длинный путь или имя файла: " + currentDir);
                        successtask = false;
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
                        successtask = false;
                        continue;
                    }

                    catch (DirectoryNotFoundException e)
                    {
                        SW.WriteLine(e.Message);
                        successtask = false;
                        continue;
                    }

                    if (DTask.Exeption == "no")
                    {
                        foreach (string file in files)
                        {
                            try
                            {
                                FileInfo fi = new FileInfo(file);
                                if (fi.LastWriteTime > DTask.DateBeginBackup)
                                {
                                    zf.AddFile(file, Path.GetDirectoryName(file).Replace(DTask.Source, string.Empty));
                                }
                            }
                            catch (FileNotFoundException e)
                            {

                                SW.WriteLine("Не найден файл: " + file);
                                successtask = false;
                                continue;
                            }

                            catch (PathTooLongException e)
                            {
                                SW.WriteLine("Слишком длинный путь или имя файла: " + file);
                                successtask = false;
                                continue;

                            }

                        }
                    }
                    else
                    {
                        int excludeflag = 0;
                        foreach (string file in files)
                        {
                            try
                            {
                                excludeflag = 0;
                                foreach (var exep in massexeption)
                                {
                                    if (Path.GetFullPath(file).Contains(exep))
                                    {
                                        excludeflag = 1;
                                        break;
                                    }
                                }
                                if (excludeflag == 0)
                                {
                                    FileInfo fi = new FileInfo(file);
                                    if (fi.LastWriteTime > DTask.DateBeginBackup)
                                    {
                                        zf.AddFile(file, Path.GetDirectoryName(file).Replace(DTask.Source, string.Empty));
                                    }
                                }


                            }
                            catch (FileNotFoundException e)
                            {

                                SW.WriteLine("Не найден файл: " + file);
                                successtask = false;
                                continue;
                            }

                            catch (PathTooLongException e)
                            {
                                SW.WriteLine("Слишком длинный путь или имя файла: " + file);
                                successtask = false;
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
                if (successtask)
                {
                    UpdateSosttask(4);
                }
                else
                {
                    UpdateSosttask(5);
                }


                if (DTask.Shadow == 1)
                {
                    foreach (VssSnapshotProperties prop in backup.QuerySnapshots())
                    {
                        if (prop.ExposedName == namedriver)
                        {
                            backup.DeleteSnapshot(prop.SnapshotId, true);
                        }
                    }
                    backup = null;
                }

                string strLine;
                int CountSkip = 0;
                StringReader stringreader = new StringReader(SW.ToString());
                while (true)
                {

                    strLine = stringreader.ReadLine();
                    if (strLine != null)
                    {
                        CountSkip++;
                        BDAL.OpenConnection();
                        BDAL.AddDiffLog(strLine, CurrentDate, DTask.Idtask, 0);
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
                            CountSkip++;
                            BDAL.OpenConnection();
                            BDAL.AddDiffLog(strLine2, CurrentDate, DTask.Idtask, 0);
                            BDAL.CloseConnection();
                        }
                    }
                    else
                    {
                        break;
                    }
                }


                if (CountSkip > 0)
                {
                    UpdateSosttask(5);
                }

                string msg= "Завершено успешно";

                BDAL.OpenConnection();
                BDAL.AddDiffLog(msg, CurrentDate, DTask.Idtask, 1);
                BDAL.CloseConnection();
                DeleteDimeLive();


                if (DTask.Ftp == 1)
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
                        client.PutFile(TimeoutFTP, zf.Name.Substring(zf.Name.IndexOf(DTask.Taskname)), zf.Name);
                        sr.Close();
                        client.Disconnect(TimeoutFTP);

                        msg= "Копирование архива на FTP сервер завершено успешно";

                        BDAL.OpenConnection();
                        BDAL.AddDiffLog(msg, CurrentDate, DTask.Idtask, 1);
                        BDAL.CloseConnection();
                        UpdateSosttask(4);
                    }
                    catch (Exception ex)
                    {

                        BDAL.OpenConnection();
                        BDAL.AddDiffLog(ex.ToString(), CurrentDate, DTask.Idtask, 0);
                        BDAL.CloseConnection();
                        UpdateSosttask(3);
                    }
                }


            }
            catch (Exception ex)
            {
                Add_Diff_Log_Error(ex.Message);
                try
                {
                    if (DTask.Shadow == 1)
                    {
                        foreach (VssSnapshotProperties prop in backup.QuerySnapshots())
                        {
                            if (prop.ExposedName == namedriver)
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
                    LongPath.WriteLine(e.Message);
                    continue;
                }
                catch (DirectoryNotFoundException e)
                {
                    LongPath.WriteLine(e.Message);
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



        public void Add_Diff_Log_Error(string ex)
        {
            try
            {
                BDAL.OpenConnection();
                BDAL.AddDiffLog(ex, CurrentDate, DTask.Idtask, 0);
                BDAL.CloseConnection();
                UpdateSosttask(3);
            }
            catch
            {

            }
        }
    }
}
