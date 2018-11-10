using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace ServiceBack
{
    public class FullTask
    {
        StreamWriter sw;
        public Boolean StatusFullTask { get; set; }
        public int Idtask { get; set; }
        public int Status { get; set; }
        public int Idserver { get; set; }
        public int Idsost { get; set; }
        public int Timelive { get; set; }
        public int Ftp { get; set; }
        public int Shadow { get; set; }

        private string taskname;
        public string Taskname
        {
            get
            {
                return taskname;
            }
            set
            {
                if (value == "")
                {
                    sw.WriteLine("Имя задачи неможет быть пустым");
                    StatusFullTask = false;
                }
                else taskname = value;

            }
        }
        private string sourse;
        public string Source
        {
            get
            {
                return sourse;
            }
            set
            {
                if (Directory.Exists(value))
                {
                    sourse = value;
                }
                else
                {
                    sw.WriteLine("Неправильно указан каталог который необходимо резервировать");
                    StatusFullTask = false;
                }
            }
        }

        private string dest;
        public string Dest
        {
            get
            {
                return dest;
            }
            set
            {
                if (Directory.Exists(value))
                {
                    dest = value;
                }
                else
                {
                    sw.WriteLine("Неправильно указан каталог для резервных копий");
                    StatusFullTask = false;
                }
            }
        }

        private string selday;
        public string Selday
        {
            get
            {
                return selday;
            }
            set
            {
                if (value == "")
                {
                    sw.WriteLine("Не выбраны дни недели, когда будет выполнятся резервирование");
                    StatusFullTask = false;
                }
                else selday = value;

            }
        }
        private string singlecopy;
        public string SingleCopy
        {
            get
            {
                return singlecopy;
            }
            set
            {
                if (value != null)
                {
                    singlecopy = value;
                }
            }

        }
        public string Extension { get; set; }
        public string Password { get; set; }
        public string Exeption { get; set; }

        public DateTime Prevstart { get; set; }
        public DateTime Nextstart { get; set; }
        public DateTime Time { get; set; }
        public DateTime Dateadd { get; set; }

        public FullTask()
        {
            StatusFullTask = false;
        }
        public FullTask(int idserver,string timelive,int ftp,int shadow,string taskname,string sourse,string dest,string selday,string extension,string password,
            string exeption,string time,string singlecopy)
        {
            sw = File.CreateText(@"ChekTask.txt");
            StatusFullTask = true;
            Status = 1;
            Idserver = idserver;
            Idsost = 1;
            Ftp = ftp;
            Shadow = shadow;
            Taskname = taskname;
            Source = sourse;
            Dest = dest;
            Selday = selday;
            Extension = extension;
            Password = password;
            Exeption = exeption;
            Dateadd = DateTime.Now;
            SingleCopy = singlecopy;
            Timelive=CheckTimeLive(timelive);
            Time=CheckTime(time);
            CheckTimeNAS(Dest);

            if (singlecopy == null)
            {
                TimeSpan TS = TimeSpan.Parse(time);
                Nextstart = GetNextTimeTask(Selday, TS) + TS;
            }
            else
            {
                Nextstart = Convert.ToDateTime(SingleCopy);
            }
            

            //Scanning files can take a long time, so you need to make sure that the rest of the checks are successful.
            if (StatusFullTask)
            {
                CheckErrorSourseFileandFolders(Source);
            }

            sw.Close();
            if(StatusFullTask==false)
            {
                Process.Start("notepad.exe", "ChekTask.txt");
            }
            

        }

        public FullTask(int idtask,int idsost, int idserver, string timelive, int ftp, int shadow, string taskname, string sourse, string dest, string selday, string extension, string password,
            string exeption, string time, string singlecopy) : this(idserver,timelive,ftp,shadow,taskname,sourse,dest,selday,extension,password,exeption,time,singlecopy)
        {
            Idtask = idtask;
            Idsost=idsost;
        }



        private void CheckTimeNAS(string root)
        {
            File.Create(root + "\\check_time.txt").Close();
            DateTime Date_Storage = File.GetCreationTime(root + "\\check_time.txt");
            File.Delete(root + "\\check_time.txt");
            if (Date_Storage.Date == DateTime.Now.Date)
            {

            }
            else
            {
                sw.WriteLine("Дата на сервере не совпадает с датой, которая установлена на сетевом хранилище");
                StatusFullTask = false;
            }
        }

        public int CheckTimeLive(string timelive)
        {
            int validtimelive;
            if (int.TryParse(timelive, out validtimelive))
            {
                return validtimelive;
                 

            }
            else
            {
                sw.WriteLine("Не корректно указано время жизни архива");
                StatusFullTask = false;
                return 0;
            }
        }

        public DateTime CheckTime(string time)
        {
                int hour, min;
                //int hour = Convert.ToInt32(time.Substring(0, 2));
                //int min = Convert.ToInt32(time.Substring(3, 2));
                int.TryParse(time.Substring(0, 2), out hour);
                int.TryParse(time.Substring(3, 2), out min);

                if ((hour < 24) && (min < 60))
                {

                    return Convert.ToDateTime(time);
                }
                else
                {
                    sw.WriteLine("Неправильно указано время");
                    StatusFullTask = false;
                    return DateTime.Now;//Here the date may be any
                }
        }

        



        public DateTime GetNextTimeTask(string days, TimeSpan time_start)
        {



            int cur_day = Convert.ToInt32(currentday());
            int min_day = 10;

            for (int i = 0; i < days.Length; i++)
            {

                if (Convert.ToInt32(days[i].ToString()) == cur_day)
                {
                    if (DateTime.Now.TimeOfDay < time_start)
                    {
                        min_day = 0;
                    }
                    else
                    {
                        if (min_day > Convert.ToInt32(days[i].ToString()) + 7 - cur_day)
                        {
                            min_day = Convert.ToInt32(days[i].ToString()) + 7 - cur_day;
                        }
                    }

                }
                else
                {
                    if (Convert.ToInt32(days[i].ToString()) > cur_day)
                    {
                        if (min_day > Convert.ToInt32(days[i].ToString()) - cur_day)
                        {
                            min_day = Convert.ToInt32(days[i].ToString()) - cur_day;
                        }
                    }
                    else
                    {
                        if (min_day > Convert.ToInt32(days[i].ToString()) + 7 - cur_day)
                        {
                            min_day = Convert.ToInt32(days[i].ToString()) + 7 - cur_day;
                        }
                    }
                }
            }




            DateTime today = System.DateTime.Now.Date;
            TimeSpan duration = new System.TimeSpan(min_day, 0, 0, 0, 0);
            today = today.Add(duration);
            
            return today;
        }

        private string currentday()
        {
            string currentday = "";
            switch (DateTime.Now.DayOfWeek.ToString())
            {
                case "Monday":
                    currentday = "1";
                    break;
                case "Tuesday":
                    currentday = "2";
                    break;
                case "Wednesday":
                    currentday = "3";
                    break;
                case "Thursday":
                    currentday = "4";
                    break;
                case "Friday":
                    currentday = "5";
                    break;
                case "Saturday":
                    currentday = "6";
                    break;
                case "Sunday":
                    currentday = "7";
                    break;
            }
            return currentday;
        }

        private void CheckErrorSourseFileandFolders(string root)
        {

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
                    sw.WriteLine(e.Message);
                    StatusFullTask = false;
                    continue;
                }
                catch (DirectoryNotFoundException e)
                {
                    sw.WriteLine(e.Message);
                    StatusFullTask = false;
                    continue;
                }
                catch (PathTooLongException e)
                {
                    
                    sw.WriteLine("Слишком длинный путь или имя файла: " + currentDir);
                    StatusFullTask = false;
                    continue;
                }


                string[] files = null;
                try
                {
                    files = Directory.GetFiles(currentDir);
                }

                catch (UnauthorizedAccessException e)
                {

                    sw.WriteLine(e.Message);
                    StatusFullTask = false;
                    continue;
                }

                catch (DirectoryNotFoundException e)
                {
                    sw.WriteLine(e.Message);
                    StatusFullTask = false;
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
                        sw.WriteLine(e.Message);
                        StatusFullTask = false;
                        continue;
                    }

                    catch (PathTooLongException e)
                    {
                        sw.WriteLine("Слишком длинный путь или имя файла: " + file);
                        StatusFullTask = false;
                        continue;

                    }
                    catch (ArgumentOutOfRangeException e)
                    {
                        sw.WriteLine("Отсутсвует дата создания/измения файла: " + file);
                        StatusFullTask = false;
                        continue;

                    }

                }

                foreach (string str in subDirs)
                {
                    dirs.Push(str);
                }
            }
        }

    }
}
