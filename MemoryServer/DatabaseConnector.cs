using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace MemoryServer
{
    class DatabaseConnector
    {

        string connectionString;
        public DatabaseConnector()
        {            
            connectionString = "Data Source = (LocalDB)\\MSSQLLocalDB; AttachDbFilename=" + Directory.GetCurrentDirectory() + "\\MemoryDatabase.mdf; Integrated Security = True";            
        }
       ~DatabaseConnector()
        {
        }

        public void editUserPassword(string login, string newPassword)
        {
            //string connectionString = "Data Source=(LocalDB)\\MSSQLLocalDB;AttachDbFilename=C:\\Users\\Piotrek\\source\\repos\\MemoryServer3\\MemoryServer\\MemoryDatabase.mdf;Integrated Security=True";
            string queryString = "UPDATE dbo.Player SET Password = @Password WHERE Login=@Login";

            if (newPassword.Length > 15 && newPassword.Length < 5)
            {
                return;
            }

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand update = new SqlCommand(queryString, conn))
                {
                    update.Parameters.AddWithValue("@Login", login);
                    update.Parameters.AddWithValue("@Password", newPassword);
                    conn.Open();
                    update.ExecuteNonQuery();
                }
            }
        }

        public bool checkUserData(string login, string password)
        {
            //string connectionString = "Data Source=(LocalDB)\\MSSQLLocalDB;AttachDbFilename=C:\\Users\\Piotrek\\source\\repos\\MemoryServer3\\MemoryServer\\MemoryDatabase.mdf;Integrated Security=True";

            SqlConnection conn = new SqlConnection(connectionString);
            SqlDataAdapter sda = new SqlDataAdapter("SELECT COUNT(*) FROM dbo.Player WHERE Login= '" + login + "' AND Password= '" + password + "'", conn);
            DataTable dt = new DataTable(); 
            sda.Fill(dt);
            if (dt.Rows[0][0].ToString() == "1")
            {
                return true;
            }
            else return false;
        }

        public bool registerUser(string login, string password)
        {
            //string connectionString = "Data Source=(LocalDB)\\MSSQLLocalDB;AttachDbFilename=C:\\Users\\Piotrek\\source\\repos\\MemoryServer3\\MemoryServer\\MemoryDatabase.mdf;Integrated Security=True";

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string queryString = "INSERT INTO dbo.Player (Id, Login, Password) VALUES (@Id, @Login, @Password);";
                string loginCheck = "SELECT COUNT(*) from dbo.Player WHERE Login= @Login";

                if(login.Length > 15 && login.Length <3)
                {
                    return false;
                }
                if(password.Length > 15 && password.Length < 5)
                {
                    return false;
                }

                using (SqlCommand check = new SqlCommand(loginCheck, conn))
                {
                    check.Parameters.AddWithValue("@Login", login);
                    conn.Open();
                    int result = (int)check.ExecuteScalar();
                    if (result == 0)
                    {
                        using (SqlCommand register = new SqlCommand(queryString, conn))
                        {
                            try
                            {
                                Guid guid = Guid.NewGuid();
                                string id = guid.ToString();
                                register.Parameters.AddWithValue("@Login", login);
                                register.Parameters.AddWithValue("@Password", password);
                                register.Parameters.AddWithValue("@Id", id);
                                register.ExecuteNonQuery();
                                System.Console.WriteLine("registered " + login + " " + id);
                                return true;
                            }
                            catch (Exception ex)
                            {
                                Console.Write(ex);
                                return false;
                            }
                        }
                    }
                    else
                    {
                        System.Console.WriteLine("login already exists");
                        return false;
                    }
                }

            }
        }

        public bool deleteUser(string login, string password)
        {

            if (checkUserData(login, password) == true)
            {
                string removeString = "DELETE FROM dbo.Player WHERE Login= @Login";
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    using (SqlCommand delete = new SqlCommand(removeString, conn))
                    {
                        try
                        {
                            conn.Open();
                            delete.Parameters.AddWithValue("@Login", login);
                            delete.ExecuteNonQuery();
                            System.Console.WriteLine("User " + login + " has been succesfully deleted");
                            return true;
                        }
                        catch (Exception exc)
                        {
                            Console.Write(exc);
                            return false;
                        }
                    }

                }

            }
            else return false;
        }
    }
}