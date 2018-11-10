using System;
using System.Data;
using System.ServiceProcess;
using System.IO;
using System.Threading;


namespace ServiceBack
{
    public partial class ServiceStart : ServiceBase
    {

        int Idserver { get; set; }
        DateTime CurrentDateTime { get; } = DateTime.Now;

        System.Timers.Timer waitNewIdTaskFull, waitNewIdTaskDiff;
        System.Timers.Timer TMFULL, TMDIFF;

        FullBackupClass FBC;
        DiffBackupClass DBC;

        FullTask FTask;
        DiffTask DTask;

        BackuperDAL BDAL;

        DataTable t;
        public ServiceStart()
        {
            InitializeComponent();
            BDAL = new BackuperDAL();
        }       

        public void StartBackupBull()
        {
            int idtask = GetIdTask();

            if (idtask > 0)
            {
                GetTaskSettingAndGo(idtask);
            }
            else
            {
                waitNewIdTaskFull = new System.Timers.Timer();
                waitNewIdTaskFull.Interval = 60000;
                waitNewIdTaskFull.Elapsed += new System.Timers.ElapsedEventHandler(WaitnewidtaskfullElapsed);
                waitNewIdTaskFull.Enabled = true;
            }

        }

        private void WaitnewidtaskfullElapsed(object sender, EventArgs e)
        {
            waitNewIdTaskFull.Enabled = false;
            StartBackupBull();
        }



        public void StartBackupDiff()
        {

            int idtaskdiff = GetIdTaskDiff();

            if (idtaskdiff > 0)
            {
                GetTaskDiffSetting(idtaskdiff, GetPrevStartFull(GetIdTaskFullToDiffTask(idtaskdiff)));
            }
            else
            {
                waitNewIdTaskDiff = new System.Timers.Timer();
                waitNewIdTaskDiff.Interval = 60000;
                waitNewIdTaskDiff.Elapsed += new System.Timers.ElapsedEventHandler(WaitNewIdTaskDiffElapsed);
                waitNewIdTaskDiff.Enabled = true;
            }
        }

        private void WaitNewIdTaskDiffElapsed(object sender, EventArgs e)
        {
            waitNewIdTaskDiff.Enabled = false;
            StartBackupDiff();
        }




        public int GetIdTask()
        {

            try
            {
                int idtask, resultcompare;
                string getdays;
                TimeSpan tasktime;
                DateTime nextStartDatetime;
                do
                {                  
                    BDAL.OpenConnection();
                    t = BDAL.SelFullTask(Idserver);
                    BDAL.CloseConnection();

                    idtask = (int)t.Rows[0]["id_task"];
                    nextStartDatetime = (DateTime)t.Rows[0]["Next_Start"];
                    getdays = (string)t.Rows[0]["sel_day"];
                    tasktime = (TimeSpan)t.Rows[0]["task_time"];

                    resultcompare = DateTime.Compare(nextStartDatetime, CurrentDateTime);
                    if (resultcompare < 0)
                    {
                        UpdateNextDatetime(idtask, getdays, tasktime);
                    }

                } while (resultcompare < 0);


                return idtask;
            }
            catch
            {
                return 0;
            }
        }


        public void UpdateNextDatetime(int idtask, string getdays, TimeSpan tasktime)
        {
            BDAL.OpenConnection();
            BDAL.EditFullTaskNextStart(idtask, GetNextTime(getdays, tasktime) + tasktime);
            BDAL.CloseConnection();
        }

        public void UpdateNextDatetimeDiff(int idtaskdiff, string getdays, TimeSpan tasktime)
        {
            BDAL.OpenConnection();
            BDAL.EditDiffTaskNextStart(idtaskdiff, GetNextTime(getdays, tasktime) + tasktime);
            BDAL.CloseConnection();
        }


        public DateTime GetNextTime(string days, TimeSpan timestart)
        {


            int curday = Convert.ToInt32(CurrentDay());
            int minday = 10;

            for (int i = 0; i < days.Length; i++)
            {

                if (Convert.ToInt32(days[i].ToString()) == curday)
                {
                    if (CurrentDateTime.TimeOfDay < timestart)
                    {
                        minday = 0;
                    }
                    else
                    {
                        if (minday > Convert.ToInt32(days[i].ToString()) + 7 - curday)
                        {
                            minday = Convert.ToInt32(days[i].ToString()) + 7 - curday;
                        }
                    }

                }
                else
                {
                    if (Convert.ToInt32(days[i].ToString()) > curday)
                    {
                        if (minday > Convert.ToInt32(days[i].ToString()) - curday)
                        {
                            minday = Convert.ToInt32(days[i].ToString()) - curday;
                        }
                    }
                    else
                    {
                        if (minday > Convert.ToInt32(days[i].ToString()) + 7 - curday)
                        {
                            minday = Convert.ToInt32(days[i].ToString()) + 7 - curday;
                        }
                    }
                }
            }

            DateTime today = CurrentDateTime.Date;
            TimeSpan duration = new System.TimeSpan(minday, 0, 0, 0, 0);
            today = today.Add(duration);

            return today;
        }

        public string CurrentDay()
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

        public void GetTaskSettingAndGo(int idtask)
        {
            t = new DataTable();
            FTask = new FullTask();
            BDAL.OpenConnection();
            t = BDAL.SelCurrentTask(idtask);
            BDAL.CloseConnection();

            FTask.Source= (string)t.Rows[0]["source"];
            FTask.Dest= (string)t.Rows[0]["dest"];
            FTask.Taskname = (string)t.Rows[0]["task_name"];
            FTask.Selday = (string)t.Rows[0]["sel_day"];
            FTask.Time = (DateTime)t.Rows[0]["task_time"];
            FTask.Nextstart = (DateTime)t.Rows[0]["Next_Start"];
            FTask.Timelive = (int)t.Rows[0]["time_live"];
            ////add 05.04.2017
            FTask.Extension = (string)t.Rows[0]["extension"];
            FTask.Password= (string)t.Rows[0]["password"];
            FTask.Exeption = (string)t.Rows[0]["exeption"];
            FTask.Ftp = (int)t.Rows[0]["ftp"];

            FBC = new FullBackupClass(FTask);
            TimeSpan rangenexttask = FTask.Nextstart - CurrentDateTime;
            TMFULL = new System.Timers.Timer();
            TMFULL.Interval = Convert.ToInt32(rangenexttask.TotalMilliseconds-10000);
            TMFULL.Elapsed += delegate (object sender2, System.Timers.ElapsedEventArgs e2)
            {
                TMFULL_Elapsed(sender2, e2, FTask.Nextstart);
            };

            TMFULL.Enabled = true;
        }

        private void TMFULL_Elapsed(object sender, EventArgs e,DateTime timeStartFullBackup)
        {
            TMFULL.Enabled = false;
            if (timeStartFullBackup > DateTime.Now)
            {
                Thread.Sleep(Convert.ToInt32((timeStartFullBackup - DateTime.Now).TotalMilliseconds));
            }
            Thread FullBackThread = new Thread(new ThreadStart(FBC.StartFullBackup));
            FullBackThread.IsBackground = true;
            FullBackThread.Start();
            Thread.Sleep(10000);
            StartBackupBull();

        }


        public void GetTaskDiffSetting(int idtaskdiff, DateTime beginbackup)
        {

            t = new DataTable();
            DTask = new DiffTask();
            BDAL.OpenConnection();
            t = BDAL.SelCurrentDiffTask(idtaskdiff);
            BDAL.CloseConnection();

            DTask.Source = (string)t.Rows[0]["source"];
            DTask.Dest = (string)t.Rows[0]["dest"];
            DTask.Taskname = (string)t.Rows[0]["task_name"];
            DTask.Selday = (string)t.Rows[0]["sel_day"];
            DTask.Time = (DateTime)t.Rows[0]["task_time"];
            DTask.Nextstart = (DateTime)t.Rows[0]["Next_Start"];
            DTask.Timelive = (int)t.Rows[0]["time_live"];
            ////add 05.04.2017
            DTask.Extension = (string)t.Rows[0]["extension"];
            DTask.Password = (string)t.Rows[0]["password"];
            DTask.Exeption = (string)t.Rows[0]["exeption"];
            DTask.Ftp = (int)t.Rows[0]["ftp"];

            DBC = new DiffBackupClass(DTask);
            TimeSpan RangeNextTask;
            RangeNextTask = DTask.Nextstart - CurrentDateTime;
            TMDIFF = new System.Timers.Timer();
            TMDIFF.Interval = Convert.ToInt32(RangeNextTask.TotalMilliseconds-10000);
            TMDIFF.Elapsed += delegate (object sender, System.Timers.ElapsedEventArgs e)
              {
                  TMDIFF_Elapsed(sender, e, DTask.Nextstart);
              };
            TMDIFF.Enabled = true;
        }

        private void TMDIFF_Elapsed(object sender, EventArgs e,DateTime timeStartDiffBackup)
        {
            TMDIFF.Enabled = false;
            if (timeStartDiffBackup > DateTime.Now)
            {
                Thread.Sleep(Convert.ToInt32((timeStartDiffBackup - DateTime.Now).TotalMilliseconds));
            }
            Thread DiffBackThread = new Thread(new ThreadStart(DBC.StartDiffBackup));
            DiffBackThread.IsBackground = true;
            DiffBackThread.Start();            
            Thread.Sleep(10000);
            StartBackupDiff();

        }



        //public int id_task_full_for_diff_task;
        public int GetIdTaskDiff()
        {

            try
            {
                int idtaskdiff, resultcompare;
                string getdays;
                t = new DataTable();
                TimeSpan tasktime;
                DateTime nextStartDatetime;
                do
                {
                    BDAL.OpenConnection();
                    t = BDAL.SelDiffTask(Idserver);
                    BDAL.CloseConnection();

                    idtaskdiff = (int)t.Rows[0]["id_task"];
                    nextStartDatetime = (DateTime)t.Rows[0]["Next_Start"];
                    getdays = (string)t.Rows[0]["sel_day"];
                    tasktime = (TimeSpan)t.Rows[0]["task_time"];

                    resultcompare = DateTime.Compare(nextStartDatetime, CurrentDateTime);
                    if (resultcompare < 0)
                    {
                        UpdateNextDatetimeDiff(idtaskdiff, getdays, tasktime);
                    }

                } while (resultcompare < 0);
                return idtaskdiff;
            }
            catch
            {
                return 0;
            }
        }

        public int GetIdTaskFullToDiffTask(int idtaskdiff)
        {
            t = new DataTable();
            BDAL.OpenConnection();
            t = BDAL.SelCurrentDiffTask(Idserver);
            BDAL.CloseConnection();
            int idtaskfull = (int)t.Rows[0]["id_task_full"];
            return idtaskfull;
        }

        public DateTime GetPrevStartFull(int idtaskfull)
        {
            DateTime PrevStart;
            t = new DataTable();
            BDAL.OpenConnection();
            t = BDAL.SelCurrentDiffTask(Idserver);
            BDAL.CloseConnection();
            try
            {
                PrevStart = (DateTime)t.Rows[0]["prev_start"];
            }
            catch
            {
                PrevStart = DateTime.Today;
            }
            return PrevStart;
        }



        protected override void OnStart(string[] args)
        {
            if (File.Exists(@"C:\Windows\RezervCop.ini"))
            {
                StreamReader sr = File.OpenText(@"C:\Windows\RezervCop.ini");
                Idserver = Convert.ToInt32(sr.ReadLine());

            }
            StartBackupBull();
            StartBackupDiff();
        }

        protected override void OnStop()
        {
            try
            {
                TMDIFF.Stop();
                TMDIFF.Dispose();
            }
            catch
            {
 
            }
            try
            {
                TMFULL.Stop();
                TMFULL.Dispose();
            }
            catch
            {
 
            }
        }
    }



}
