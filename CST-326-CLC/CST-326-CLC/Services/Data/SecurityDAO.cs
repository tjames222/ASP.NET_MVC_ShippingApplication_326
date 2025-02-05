﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using System.Configuration;
using CST_326_CLC.Models;
using System.Data.SqlClient;
using System.Web.Configuration;
using System.Diagnostics;
using System.Security.Cryptography;
using CST_326_CLC.Services.Business;
using Serilog;
using CST_326_CLC.Controllers;

namespace CST_326_CLC.Services.Data
{
    public class SecurityDAO
    {
        public bool CheckUsername(string username)
        {
            Log.Information("SecurityDAO: Checking username: {0} against the database", username);
            string query = "SELECT * FROM dbo.Users WHERE username = @Username";

            SqlConnection conn = new SqlConnection(WebConfigurationManager.ConnectionStrings["myConn"].ConnectionString);
            SqlCommand command = new SqlCommand(query, conn);

            try
            {
                command.Parameters.Add("@Username", SqlDbType.VarChar, 50).Value = username;
                conn.Open();

                SqlDataReader reader = command.ExecuteReader();

                if(reader.HasRows)
                {
                    Log.Information("SecurityDAO: Successfully found username: {0}", username);
                    reader.Close();
                    return true;
                }
            }
            catch(SqlException e)
            {
                Log.Information("SecurityDAO: There was an SQL excecption when checking username: {0}", username);
                Debug.WriteLine(String.Format("Error generated: {0} - {1}", e.GetType(), e.Message));
            }
            finally
            {
                conn.Close();
            }
            return false;
        }

        public bool CheckEmail(string email)
        {
            Log.Information("SecurityDAO: Checking user email: {0} against the database", email);

            string query = "SELECT * FROM dbo.Users WHERE email = @Email";
            SqlConnection conn = new SqlConnection(WebConfigurationManager.ConnectionStrings["myConn"].ConnectionString);
            SqlCommand command = new SqlCommand(query, conn);

            try
            {
                command.Parameters.Add("@Email", SqlDbType.VarChar, 50).Value = email;
                conn.Open();

                SqlDataReader reader = command.ExecuteReader();

                if(reader.HasRows)
                {
                    Log.Information("SecurityDAO: Successfully found user email: {0}", email);
                    reader.Close();
                    return true;
                }
            }
            catch (SqlException e)
            {
                Log.Information("SecurityDAO: There was an SQL excecption when checking for user email: {0}", email);
                Debug.WriteLine(String.Format("Error generated: {0} - {1}", e.GetType(), e.Message));
            }
            finally
            {
                conn.Close();
            }
            return false;
        }

        public bool RegisterUser(PersonalUserModel user)
        {
            Log.Information("SecurityDAO: Registering new user to database");

            int retValue = 0;
            
            SqlConnection conn = new SqlConnection(WebConfigurationManager.ConnectionStrings["myConn"].ConnectionString);

            conn.Open();

            string query = "INSERT INTO dbo.Users(first_name, last_name, address, apartment_suite, city, state, zipcode, country, phone, email, username," +
                " password, isBusinessAccount, isAdmin) VALUES(@fName, @lName, @address, @apartment, @city, @state, @zip, @country, @phone, @email, @username," +
                " @password, @business, @admin)";

            SqlCommand command = new SqlCommand(query, conn);

            command.Parameters.Add(new SqlParameter("@fName", SqlDbType.VarChar, 25)).Value = user.firstName;
            command.Parameters.Add(new SqlParameter("@lName", SqlDbType.VarChar, 25)).Value = user.lastName;
            command.Parameters.Add(new SqlParameter("@address", SqlDbType.VarChar, 100)).Value = user.address;
            if (user.apartmentSuite != null)
            {
                command.Parameters.Add(new SqlParameter("@apartment", SqlDbType.VarChar, 25)).Value = user.apartmentSuite;
            }
            else
            {
                command.Parameters.Add(new SqlParameter("@apartment", SqlDbType.VarChar, 25)).Value = DBNull.Value;
            }
            command.Parameters.Add(new SqlParameter("@city", SqlDbType.VarChar, 50)).Value = user.city;
            command.Parameters.Add(new SqlParameter("@state", SqlDbType.VarChar, 25)).Value = user.state;
            command.Parameters.Add(new SqlParameter("@zip", SqlDbType.Int)).Value = user.zipCode;
            command.Parameters.Add(new SqlParameter("@country", SqlDbType.VarChar, 25)).Value = user.country;
            command.Parameters.Add(new SqlParameter("@phone", SqlDbType.VarChar, 15)).Value = user.phone;
            command.Parameters.Add(new SqlParameter("@email", SqlDbType.VarChar, 50)).Value = user.email;
            command.Parameters.Add(new SqlParameter("@username", SqlDbType.VarChar, 50)).Value = user.username;
            command.Parameters.Add(new SqlParameter("@password", SqlDbType.VarChar, 100)).Value = Hash(user.password);
            command.Parameters.Add(new SqlParameter("@business", SqlDbType.TinyInt)).Value = user.isBusinessAccount;
            command.Parameters.Add(new SqlParameter("@admin", SqlDbType.TinyInt)).Value = user.isAdmin;

            command.Prepare();

            retValue = command.ExecuteNonQuery();

            return Convert.ToBoolean(retValue);
        }

        public bool RegisterPersonal(PersonalRegistration model)
        {
            Log.Information("SecurityDAO: Registering new user to database");

            SqlConnection conn = new SqlConnection(WebConfigurationManager.ConnectionStrings["myConn"].ConnectionString);
            conn.Open();
            SqlCommand command = conn.CreateCommand();
            SqlTransaction transaction;
            transaction = conn.BeginTransaction("Registration");

            command.Connection = conn;
            command.Transaction = transaction;

            try
            {
                command.CommandText = "INSERT INTO dbo.Users(first_name, last_name, phone, email, username, password, isBusinessAccount, isAdmin) " +
                    "VALUES (@fName, @lName, @phone, @email, @username, @password, @business, @admin); SELECT SCOPE_IDENTITY();";
                command.Parameters.Add(new SqlParameter("@fName", SqlDbType.VarChar, 25)).Value = model.userModel.firstName;
                command.Parameters.Add(new SqlParameter("@lName", SqlDbType.VarChar, 25)).Value = model.userModel.lastName;
                command.Parameters.Add(new SqlParameter("@phone", SqlDbType.VarChar, 15)).Value = model.userModel.phone;
                command.Parameters.Add(new SqlParameter("@email", SqlDbType.VarChar, 50)).Value = model.userModel.email;
                command.Parameters.Add(new SqlParameter("@username", SqlDbType.VarChar, 50)).Value = model.userModel.username;
                command.Parameters.Add(new SqlParameter("@password", SqlDbType.VarChar, 100)).Value = Hash(model.userModel.password);
                command.Parameters.Add(new SqlParameter("@business", SqlDbType.TinyInt)).Value = model.userModel.isBusinessAccount;
                command.Parameters.Add(new SqlParameter("@admin", SqlDbType.TinyInt)).Value = model.userModel.isAdmin;
                int userID = Convert.ToInt32(command.ExecuteScalar());

                command.Parameters.Clear();

                command.CommandText = "INSERT INTO dbo.Address(USER_ID, ADDRESS, APT_SUITE, CITY, STATE, ZIP, COUNTRY) " +
                    "VALUES (@ID, @Address, @Suite, @City, @State, @Zip, @Country)";
                command.Parameters.Add(new SqlParameter("@ID", SqlDbType.Int)).Value = userID;
                command.Parameters.Add(new SqlParameter("@Address", SqlDbType.NVarChar, 100)).Value = model.addressModel.address;

                if (model.addressModel.aptSuite != null)
                {
                    command.Parameters.Add(new SqlParameter("@Suite", SqlDbType.NVarChar, 25)).Value = model.addressModel.aptSuite;
                }
                else
                {
                    command.Parameters.Add(new SqlParameter("@Suite", SqlDbType.NVarChar, 25)).Value = DBNull.Value;
                }
                command.Parameters.Add(new SqlParameter("@City", SqlDbType.NVarChar, 50)).Value = model.addressModel.city;
                command.Parameters.Add(new SqlParameter("@State", SqlDbType.NVarChar, 25)).Value = model.addressModel.state;
                command.Parameters.Add(new SqlParameter("@Zip", SqlDbType.Int)).Value = model.addressModel.zip;
                command.Parameters.Add(new SqlParameter("@Country", SqlDbType.NVarChar, 50)).Value = model.addressModel.country;
                command.ExecuteNonQuery();

                transaction.Commit();
                return true;
            }
            catch (SqlException e)
            {
                Debug.WriteLine(e.GetType());
                Debug.WriteLine(e.Message);
                try
                {
                    transaction.Rollback();
                    return false;
                }
                catch (SqlException e2)
                {
                    Debug.WriteLine(e2.GetType());
                    Debug.WriteLine(e2.Message);
                }
            }
            finally
            {
                conn.Close();
            }

            return false;
        }


        public bool RegisterBusinessNEW(BusinessLoginModel login, BusinessRegistration businessRegistration)
        {
            Log.Information("SecurityDAO: Register new business user to the database");
            SqlConnection conn = new SqlConnection(WebConfigurationManager.ConnectionStrings["myConn"].ConnectionString);
            conn.Open();
            SqlCommand command = conn.CreateCommand();
            SqlTransaction transaction;
            transaction = conn.BeginTransaction("BusinessRegistration");

            command.Connection = conn;
            command.Transaction = transaction;

            try
            {
                command.CommandText = "INSERT INTO dbo.Users(first_name, last_name, phone, email, username, password, isBusinessAccount, isAdmin, company_name," +
                    " securityQuestion, securityAnswer) VALUES (@fName, @lName, @phone, @email, @username, @password, @business, @admin, @company, @securityQuestion," +
                    " @securityAnswer); SELECT SCOPE_IDENTITY();";

                command.Parameters.Add(new SqlParameter("@fName", SqlDbType.VarChar, 25)).Value = businessRegistration.businessModel.firstName;
                command.Parameters.Add(new SqlParameter("@lName", SqlDbType.VarChar, 25)).Value = businessRegistration.businessModel.lastName;
                command.Parameters.Add(new SqlParameter("@phone", SqlDbType.VarChar, 15)).Value = businessRegistration.businessModel.phone;
                command.Parameters.Add(new SqlParameter("@email", SqlDbType.VarChar, 50)).Value = businessRegistration.businessModel.companyEmail;
                command.Parameters.Add(new SqlParameter("@username", SqlDbType.VarChar, 50)).Value = login.username;
                command.Parameters.Add(new SqlParameter("@password", SqlDbType.VarChar, 100)).Value = Hash(login.password);
                command.Parameters.Add(new SqlParameter("@business", SqlDbType.TinyInt)).Value = businessRegistration.businessModel.isBusinessAccount;
                command.Parameters.Add(new SqlParameter("@admin", SqlDbType.TinyInt)).Value = businessRegistration.businessModel.isAdmin;
                command.Parameters.Add(new SqlParameter("@company", SqlDbType.VarChar, 50)).Value = businessRegistration.businessModel.companyName;
                command.Parameters.Add(new SqlParameter("@securityQuestion", SqlDbType.VarChar, 100)).Value = login.securityQuestion;
                command.Parameters.Add(new SqlParameter("@securityAnswer", SqlDbType.VarChar, 100)).Value = login.securityAnswer;
                int userID = Convert.ToInt32(command.ExecuteScalar());

                command.Parameters.Clear();

                command.CommandText = "INSERT INTO dbo.Address(USER_ID, ADDRESS, APT_SUITE, CITY, STATE, ZIP, COUNTRY) " +
                    "VALUES (@ID, @Address, @Suite, @City, @State, @Zip, @Country)";
                command.Parameters.Add(new SqlParameter("@ID", SqlDbType.Int)).Value = userID;
                command.Parameters.Add(new SqlParameter("@Address", SqlDbType.NVarChar, 100)).Value = businessRegistration.addressModel.address;

                if (businessRegistration.addressModel.aptSuite != null)
                {
                    command.Parameters.Add(new SqlParameter("@Suite", SqlDbType.NVarChar, 25)).Value = businessRegistration.addressModel.aptSuite;
                }
                else
                {
                    command.Parameters.Add(new SqlParameter("@Suite", SqlDbType.NVarChar, 25)).Value = DBNull.Value;
                }
                command.Parameters.Add(new SqlParameter("@City", SqlDbType.NVarChar, 50)).Value = businessRegistration.addressModel.city;
                command.Parameters.Add(new SqlParameter("@State", SqlDbType.NVarChar, 25)).Value = businessRegistration.addressModel.state;
                command.Parameters.Add(new SqlParameter("@Zip", SqlDbType.Int)).Value = businessRegistration.addressModel.zip;
                command.Parameters.Add(new SqlParameter("@Country", SqlDbType.NVarChar, 50)).Value = businessRegistration.addressModel.country;
                command.ExecuteNonQuery();

                transaction.Commit();
                return true;

            }
            catch (SqlException e)
            {
                Debug.WriteLine(e.GetType());
                Debug.WriteLine(e.Message);
                try
                {
                    transaction.Rollback();
                    return false;
                }
                catch (SqlException e2)
                {
                    Debug.WriteLine(e2.GetType());
                    Debug.WriteLine(e2.Message);
                }
            }
            finally
            {
                conn.Close();
            }

            return false;
        }

        //public bool RegisterBusiness(BusinessModel model, string securityQuestion, string securityAnswer)
        //{
        //    int retValue = 0;

        //    SqlConnection conn = new SqlConnection(WebConfigurationManager.ConnectionStrings["myConn"].ConnectionString);

        //    conn.Open();

        //    string query = "INSERT INTO dbo.Users(first_name, last_name, address, apartment_suite, city, state, zipcode, country, phone, email, username," +
        //        " password, isBusinessAccount, isAdmin, securityQuestion, securityAnswer) VALUES(@fName, @lName, @address, @apartment, " +
        //        "@city, @state, @zip, @country, @phone, @email, @username, @password, @business, @admin, @question, @answer)";

        //    SqlCommand command = new SqlCommand(query, conn);

        //    command.Parameters.Add(new SqlParameter("@fName", SqlDbType.VarChar, 25)).Value = model.firstName;
        //    command.Parameters.Add(new SqlParameter("@lName", SqlDbType.VarChar, 25)).Value = model.lastName;
        //    command.Parameters.Add(new SqlParameter("@address", SqlDbType.VarChar, 100)).Value = model.companyAddress;
        //    if (model.suite != null)
        //    {
        //        command.Parameters.Add(new SqlParameter("@apartment", SqlDbType.VarChar, 25)).Value = model.suite;
        //    }
        //    else
        //    {
        //        command.Parameters.Add(new SqlParameter("@apartment", SqlDbType.VarChar, 25)).Value = DBNull.Value;
        //    }
        //    command.Parameters.Add(new SqlParameter("@city", SqlDbType.VarChar, 50)).Value = model.city;
        //    command.Parameters.Add(new SqlParameter("@state", SqlDbType.VarChar, 25)).Value = model.state;
        //    command.Parameters.Add(new SqlParameter("@zip", SqlDbType.Int)).Value = model.zipCode;
        //    command.Parameters.Add(new SqlParameter("@country", SqlDbType.VarChar, 25)).Value = model.country;
        //    command.Parameters.Add(new SqlParameter("@phone", SqlDbType.VarChar, 15)).Value = model.phone;
        //    command.Parameters.Add(new SqlParameter("@email", SqlDbType.VarChar, 50)).Value = model.companyEmail;
        //    command.Parameters.Add(new SqlParameter("@username", SqlDbType.VarChar, 50)).Value = model.username;
        //    command.Parameters.Add(new SqlParameter("@password", SqlDbType.VarChar, 100)).Value = Hash(model.password);
        //    command.Parameters.Add(new SqlParameter("@business", SqlDbType.TinyInt)).Value = model.isBusinessAccount;
        //    command.Parameters.Add(new SqlParameter("@admin", SqlDbType.TinyInt)).Value = model.isAdmin;
        //    command.Parameters.Add(new SqlParameter("@question", SqlDbType.VarChar, 100)).Value = securityQuestion;
        //    command.Parameters.Add(new SqlParameter("@answer", SqlDbType.VarChar, 100)).Value = securityAnswer;

        //    command.Prepare();
        //    retValue = command.ExecuteNonQuery();
        //    return Convert.ToBoolean(retValue);
        //}

        public static string Hash(string password)
        {
            Log.Information("SecurityDAO: Hashing password");
            byte[] salt;
            new RNGCryptoServiceProvider().GetBytes(salt = new byte[20]);
            var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000);
            byte[] hash = pbkdf2.GetBytes(20);
            byte[] hashBytes = new byte[40];
            Array.Copy(salt, 0, hashBytes, 0, 20);
            Array.Copy(hash, 0, hashBytes, 20, 20);
            string hashPass = Convert.ToBase64String(hashBytes);
            return hashPass;
        }

        public bool VerifyHash(string hashPass, string password)
        {
            Log.Information("SecurityDAO: Verifing hashed password");
            byte[] hashBytes = Convert.FromBase64String(hashPass);
            byte[] salt = new byte[20];
            Array.Copy(hashBytes, 0, salt, 0, 20);
            var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000);
            byte[] hash = pbkdf2.GetBytes(20);

            for (int i = 0; i < 20; i++)
            {
                if (hashBytes[i + 20] != hash[i])
                {
                    return false;
                    throw new UnauthorizedAccessException();
                }
            }
            Log.Information("SecurityDAO: Hashed password verified.");
            return true;
        }

        public bool AuthenticateUser(LoginModel user)
        {
            Log.Information("SecurityDAO: Authenticating user against database");

            string query = "SELECT * FROM dbo.Users WHERE username = @Username";
            bool autenticatedUser = false;

            SqlConnection conn = new SqlConnection(WebConfigurationManager.ConnectionStrings["myConn"].ConnectionString);
            SqlCommand command = new SqlCommand(query, conn);

            try
            {
                command.Parameters.Add("@Username", SqlDbType.VarChar, 50).Value = user.username;

                conn.Open();

                SqlDataReader reader = command.ExecuteReader();

                if(reader.HasRows)
                {
                    while(reader.Read())
                    {
                        if(!VerifyHash(reader.GetString(12), user.password))
                        {
                            autenticatedUser = false;
                            break;
                        }
                        else
                        {
                            UserModel loggedUser = new UserModel();
                            loggedUser.userID = reader.GetInt32(0);
                            loggedUser.firstName = reader.GetString(1);
                            loggedUser.lastName = reader.GetString(2);
                            loggedUser.phone = reader.GetString(9);
                            loggedUser.email = reader.GetString(10);
                            loggedUser.username = reader.GetString(11);
                            int business = (int)reader.GetSqlByte(13);
                            int admin = (int)reader.GetSqlByte(14);
                            loggedUser.isBusinessAccount = Convert.ToBoolean(business);
                            loggedUser.isAdmin = Convert.ToBoolean(admin);
                            UserManagement.Instance._loggedUser = loggedUser;

                            autenticatedUser = true;
                        }
                    }
                }
            }
            catch (SqlException e)
            {
                Debug.WriteLine(String.Format("Error generated: {0} - {1}", e.GetType(), e.Message));
            }
            finally
            {
                conn.Close();
            }
            return autenticatedUser;
        }
    }
}