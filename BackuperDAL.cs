using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Configuration;
using System.Data;

namespace ServiceBack
{
    class BackuperDAL
    {
        private SqlConnection sqlCn = null;
        private string sqlStr = null;

        public BackuperDAL()
        {
            sqlStr = ConfigurationManager.ConnectionStrings["cnStrBackuper"].ConnectionString;
        }

        public void OpenConnection()
        {
            sqlCn = new SqlConnection();
            sqlCn.ConnectionString = sqlStr;
            sqlCn.Open();
        }

        public void CloseConnection()
        {
            sqlCn.Close();
        }

        public void AddFullTask(FullTask fulltask)
        {
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = this.sqlCn;
                cmd.CommandText = "add_full_copy";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@task_name", SqlDbType.NVarChar, 50).Value = fulltask.Taskname;
                cmd.Parameters.Add("@source", SqlDbType.NVarChar, 250).Value = fulltask.Source;
                cmd.Parameters.Add("@Dest", SqlDbType.NVarChar, 250).Value = fulltask.Dest;
                cmd.Parameters.Add("@Sel_day", SqlDbType.NVarChar, 50).Value = fulltask.Selday;
                cmd.Parameters.Add("@task_time", SqlDbType.Time).Value = fulltask.Time.ToShortTimeString();
                cmd.Parameters.Add("@id_server", SqlDbType.Int).Value = fulltask.Idserver;
                cmd.Parameters.Add("@next_start", SqlDbType.DateTime).Value = fulltask.Nextstart;
                cmd.Parameters.Add("@time_live", SqlDbType.Int).Value = fulltask.Timelive;
                cmd.Parameters.Add("@extension", SqlDbType.NVarChar, 50).Value = fulltask.Extension;
                cmd.Parameters.Add("@password", SqlDbType.NVarChar, 50).Value = fulltask.Password;
                cmd.Parameters.Add("@date_add", SqlDbType.Date).Value = fulltask.Dateadd;
                cmd.Parameters.Add("@exeption", SqlDbType.NVarChar, 1000).Value = fulltask.Exeption;
                cmd.Parameters.Add("@ftp", SqlDbType.Int).Value = fulltask.Ftp;
                cmd.Parameters.Add("@shadow", SqlDbType.Int).Value = fulltask.Shadow;
                cmd.ExecuteNonQuery();


            }
        }

        public void EditFullTask(FullTask fulltask)
        {
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = this.sqlCn;
                cmd.CommandText = "edit_full_copy_next";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@id_task", SqlDbType.Int).Value = fulltask.Idtask;
                cmd.Parameters.Add("@task_name", SqlDbType.NVarChar, 50).Value = fulltask.Taskname;
                cmd.Parameters.Add("@source", SqlDbType.NVarChar, 250).Value = fulltask.Source;
                cmd.Parameters.Add("@Dest", SqlDbType.NVarChar, 250).Value = fulltask.Dest;
                cmd.Parameters.Add("@Sel_day", SqlDbType.NVarChar, 50).Value = fulltask.Selday;
                cmd.Parameters.Add("@task_time", SqlDbType.Time).Value = fulltask.Time.ToShortTimeString();
                cmd.Parameters.Add("@next_start", SqlDbType.DateTime).Value = fulltask.Nextstart;
                cmd.Parameters.Add("@time_live", SqlDbType.Int).Value = fulltask.Timelive;
                cmd.Parameters.Add("@extension", SqlDbType.NVarChar, 50).Value = fulltask.Extension;
                cmd.Parameters.Add("@password", SqlDbType.NVarChar, 50).Value = fulltask.Password;
                cmd.Parameters.Add("@exeption", SqlDbType.NVarChar, 1000).Value = fulltask.Exeption;
                cmd.Parameters.Add("@ftp", SqlDbType.Int).Value = fulltask.Ftp;
                cmd.Parameters.Add("@shadow", SqlDbType.Int).Value = fulltask.Shadow;
                cmd.ExecuteNonQuery();


            }
        }

        public DataTable SelFullTask(int idserver)
        {
            DataTable dTable = new DataTable();
            using (SqlCommand cmd = new SqlCommand())
            {
                SqlDataAdapter sqlAdapter = new SqlDataAdapter();
                cmd.Connection = this.sqlCn;
                cmd.CommandText = "Select * from task where (id_server=@id_server)and(status=1) order by (next_start)";
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.Add("@id_server", SqlDbType.Int).Value = idserver;
                sqlAdapter.SelectCommand = cmd;
                sqlAdapter.Fill(dTable);
            }
            return dTable;
        }

        public DataTable SelCurrentTask(int idtask)
        {
            DataTable dTable = new DataTable();
            using (SqlCommand cmd = new SqlCommand())
            {
                SqlDataAdapter sqlAdapter = new SqlDataAdapter();
                cmd.Connection = this.sqlCn;
                cmd.CommandText = "Select * from task where id_task=@id_task";
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.Add("@id_task", SqlDbType.Int).Value = idtask;
                sqlAdapter.SelectCommand = cmd;
                sqlAdapter.Fill(dTable);
            }
            return dTable;
        }

        public DataTable SelDiffTask(int idfultask)
        {
            DataTable dTable = new DataTable();
            using (SqlCommand cmd = new SqlCommand())
            {
                SqlDataAdapter sqlAdapter = new SqlDataAdapter();
                cmd.Connection = this.sqlCn;
                cmd.CommandText = "Select * from task_diff where (id_task_full=@id_task_full)and(status=1) order by (next_start)";
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.Add("@id_task_full", SqlDbType.Int).Value = idfultask;
                sqlAdapter.SelectCommand = cmd;
                sqlAdapter.Fill(dTable);
            }
            return dTable;
        }

        public DataTable SelCurrentDiffTask(int idtask)
        {
            DataTable dTable = new DataTable();
            using (SqlCommand cmd = new SqlCommand())
            {
                SqlDataAdapter sqlAdapter = new SqlDataAdapter();
                cmd.Connection = this.sqlCn;
                cmd.CommandText = "Select * from task_diff where id_task_diff=@id_task_diff";
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.Add("@id_task_diff", SqlDbType.Int).Value = idtask;
                sqlAdapter.SelectCommand = cmd;
                sqlAdapter.Fill(dTable);
            }
            return dTable;
        }

        public DataTable IdintificatedClient(int idserver)
        {
            DataTable dTable = new DataTable();
            using (SqlCommand cmd = new SqlCommand())
            {
                SqlDataAdapter sqlAdapter = new SqlDataAdapter();
                cmd.Connection = this.sqlCn;
                cmd.CommandText = "Select id_client from View_Org_Server where id_server=@id_server";
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.Add("@id_server", SqlDbType.Int).Value = idserver;
                sqlAdapter.SelectCommand = cmd;
                sqlAdapter.Fill(dTable);
            }
            return dTable;
        }

        public DataTable SelOrg(int idserver)
        {
            DataTable dTable = new DataTable();
            using (SqlCommand cmd = new SqlCommand())
            {
                SqlDataAdapter sqlAdapter = new SqlDataAdapter();
                cmd.Connection = this.sqlCn;
                cmd.CommandText = "Select * from View_Org_Server where (id_server=@id_server) and(status=1)";
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.Add("@id_server", SqlDbType.Int).Value = idserver;
                sqlAdapter.SelectCommand = cmd;
                sqlAdapter.Fill(dTable);
            }
            return dTable;
        }

        public DataTable SelCountDiff(int idtask)
        {
            DataTable dTable = new DataTable();
            using (SqlCommand cmd = new SqlCommand())
            {
                SqlDataAdapter sqlAdapter = new SqlDataAdapter();
                cmd.Connection = this.sqlCn;
                cmd.CommandText = "Sel_Count_diff";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@id_task", SqlDbType.Int).Value = idtask;
                sqlAdapter.SelectCommand = cmd;
                sqlAdapter.Fill(dTable);
            }
            return dTable;
        }

        public void EditFullTaskNextStart(int idtask,DateTime nextstart)
        {
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = this.sqlCn;
                cmd.CommandText = "edit_full_for_next_start";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@id_task", SqlDbType.Int).Value = idtask;
                cmd.Parameters.Add("@next_start",SqlDbType.DateTime).Value = nextstart;
                cmd.ExecuteNonQuery();
            }
        }

        public void EditDiffTaskNextStart(int idtask, DateTime nextstart)
        {
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = this.sqlCn;
                cmd.CommandText = "edit_diff_for_next_start";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@id_task_diff", SqlDbType.Int).Value = idtask;
                cmd.Parameters.Add("@next_start", SqlDbType.DateTime).Value = nextstart;
                cmd.ExecuteNonQuery();
            }
        }

        public void EditFullTaskSost(int idtask,int idsost, DateTime prevstart)
        {
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = this.sqlCn;
                cmd.CommandText = "edit_full_for_sost";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@id_task", SqlDbType.Int).Value = idtask;
                cmd.Parameters.Add("@id_sost", SqlDbType.Int).Value = idsost;
                cmd.Parameters.Add("@prev_start", SqlDbType.DateTime).Value = prevstart;
                cmd.ExecuteNonQuery();
            }
        }

        public void EditDiffTaskSost(int idtask, int idsost, DateTime prevstart)
        {
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = this.sqlCn;
                cmd.CommandText = "edit_full_for_sost";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@id_task", SqlDbType.Int).Value = idtask;
                cmd.Parameters.Add("@id_sost", SqlDbType.Int).Value = idsost;
                cmd.Parameters.Add("@prev_start", SqlDbType.DateTime).Value = prevstart;
                cmd.ExecuteNonQuery();
            }
        }

        public void AddFullLog(string msg,DateTime date,int idtask,int success)
        {
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = this.sqlCn;
                cmd.CommandText = "add_full_log";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@task_log", SqlDbType.NVarChar, 500).Value = msg;
                cmd.Parameters.Add("@datelog", SqlDbType.DateTime).Value = date;
                cmd.Parameters.Add("@id_task", SqlDbType.Int).Value = idtask;
                cmd.Parameters.Add("@success", SqlDbType.Int).Value = success;
                cmd.ExecuteNonQuery();
            }
        }

        public void AddDiffLog(string msg, DateTime date, int idtask, int success)
        {
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = this.sqlCn;
                cmd.CommandText = "add_diff_log";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@task_log", SqlDbType.NVarChar, 500).Value = msg;
                cmd.Parameters.Add("@datelog", SqlDbType.DateTime).Value = date;
                cmd.Parameters.Add("@id_task", SqlDbType.Int).Value = idtask;
                cmd.Parameters.Add("@success", SqlDbType.Int).Value = success;
                cmd.ExecuteNonQuery();
            }
        }
    }
}
