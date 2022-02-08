using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;
using MySql.Data;
using MySql.Data.MySqlClient;
using System.Data;

namespace ProjectTemplate
{
	[WebService(Namespace = "http://tempuri.org/")]
	[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
	[System.ComponentModel.ToolboxItem(false)]
	[System.Web.Script.Services.ScriptService]

	public class ProjectServices : System.Web.Services.WebService
	{
		////////////////////////////////////////////////////////////////////////
		///replace the values of these variables with your database credentials
		////////////////////////////////////////////////////////////////////////
		private string dbID = "cis440template";
		private string dbPass = "!!Cis440";
		private string dbName = "cis440template";
		////////////////////////////////////////////////////////////////////////
		
		////////////////////////////////////////////////////////////////////////
		///call this method anywhere that you need the connection string!
		////////////////////////////////////////////////////////////////////////
		private string getConString() {
			return "SERVER=107.180.1.16; PORT=3306; DATABASE=" + dbName+"; UID=" + dbID + "; PASSWORD=" + dbPass;
		}
		////////////////////////////////////////////////////////////////////////



		/////////////////////////////////////////////////////////////////////////
		//don't forget to include this decoration above each method that you want
		//to be exposed as a web service!
		[WebMethod(EnableSession = true)]
		/////////////////////////////////////////////////////////////////////////
		public string TestConnection()
		{
			try
			{
				string testQuery = "select * from test";

				////////////////////////////////////////////////////////////////////////
				///here's an example of using the getConString method!
				////////////////////////////////////////////////////////////////////////
				MySqlConnection con = new MySqlConnection(getConString());
				////////////////////////////////////////////////////////////////////////

				MySqlCommand cmd = new MySqlCommand(testQuery, con);
				MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
				DataTable table = new DataTable();
				adapter.Fill(table);
				return "Success!";
			}
			catch (Exception e)
			{
				return "Something went wrong, please check your credentials and db name and try again.  Error: "+e.Message;
			}
		}


        [WebMethod(EnableSession = true)] //NOTICE: gotta enable session on each individual method
        public bool LogOn(string uid, string pass)
        {
            //LOGIC: pass the parameters into the database to see if an account
            //with these credentials exist.  If it does, then return true.  If
            //it doesn't, then return false

            //we return this flag to tell them if they logged in or not
            bool success = false;

            //our connection string comes from our web.config file like we talked about earlier
            string sqlConnectString = System.Configuration.ConfigurationManager.ConnectionStrings["myDB"].ConnectionString;
            //here's our query.  A basic select with nothing fancy.  Note the parameters that begin with @
            string sqlSelect = "SELECT id, admin, approved FROM Accounts WHERE userName=@idValue and pwd=@passValue";

            //set up our connection object to be ready to use our connection string
            MySqlConnection sqlConnection = new MySqlConnection(sqlConnectString);
            //set up our command object to use our connection, and our query
            MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);

            //tell our command to replace the @parameters with real values
            //we decode them because they came to us via the web so they were encoded
            //for transmission (funky characters escaped, mostly)
            sqlCommand.Parameters.AddWithValue("@idValue", HttpUtility.UrlDecode(uid));
            sqlCommand.Parameters.AddWithValue("@passValue", HttpUtility.UrlDecode(pass));

            //a data adapter acts like a bridge between our command object and 
            //the data we are trying to get back and put in a table object
            MySqlDataAdapter sqlDa = new MySqlDataAdapter(sqlCommand);
            //here's the table we want to fill with the results from our query
            DataTable sqlDt = new DataTable();
            //here we go filling it!
            sqlDa.Fill(sqlDt);
            //check to see if any rows were returned.  If they were, it means it's 
            //a legit account
            if (sqlDt.Rows.Count > 0)     
            {
                //if we found an account, store the id and admin status in the session
                //so we can check those values later on other method calls to see if they 
                //are 1) logged in at all, and 2) and admin or not. Also checking to see if use is approved.
                int approved = Convert.ToInt32(sqlDt.Rows[0]["approved"]);
                if (approved == 1){ 
                    Session["id"] = sqlDt.Rows[0]["id"];
                    Session["admin"] = sqlDt.Rows[0]["admin"];
                    success = true;
                }
            }
            //return the result!
            return success;
        }

        [WebMethod(EnableSession = true)]
        public bool LogOff()
        {
            //if they log off, then we remove the session.  That way, if they access
            //again later they have to log back on in order for their ID to be back
            //in the session!
            Session.Abandon();
            return true;
        }

        //Posting a new suggestion
        [WebMethod(EnableSession = true)]
        public void NewSuggestion(string desc, string category)
        {
            int userID = Convert.ToInt32(Session["id"]);
            string sqlConnectString = System.Configuration.ConfigurationManager.ConnectionStrings["myDB"].ConnectionString;
            string sqlSelect = "insert into suggestions (`desc`, submitter, approved, status, category) " +
            "values(@descValue, @userID, 0, 0, @category); SELECT LAST_INSERT_ID();";

            MySqlConnection sqlConnection = new MySqlConnection(sqlConnectString);
            MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);

            sqlCommand.Parameters.AddWithValue("@descValue", HttpUtility.UrlDecode(desc));
            sqlCommand.Parameters.AddWithValue("@userID", userID);
            sqlCommand.Parameters.AddWithValue("@category", category);

            sqlConnection.Open();
           try
            {
                int postID = Convert.ToInt32(sqlCommand.ExecuteScalar());
            }
            catch (Exception e)
            {
            }
            sqlConnection.Close();
        }

        //Grabbing all of the suggestions that have not been approved yet. 
        [WebMethod(EnableSession = true)]
        public Suggestion[] GetUnapprovedSuggestions()
        {
            if (Convert.ToInt32(Session["admin"]) == 1)
            {
                DataTable sqlDt = new DataTable("suggestions");

                string sqlConnectString = System.Configuration.ConfigurationManager.ConnectionStrings["myDB"].ConnectionString;
                string sqlSelect = "select id, `desc`, submitter, category from suggestions where approved=0 order by id";

                MySqlConnection sqlConnection = new MySqlConnection(sqlConnectString);
                MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);

                MySqlDataAdapter sqlDa = new MySqlDataAdapter(sqlCommand);
                sqlDa.Fill(sqlDt);

                List<Suggestion> suggestions = new List<Suggestion>();
                for (int i = 0; i < sqlDt.Rows.Count; i++)
                {

                    suggestions.Add(new Suggestion
                    {
                        id = Convert.ToInt32(sqlDt.Rows[i]["id"]),
                        desc = sqlDt.Rows[i]["desc"].ToString(),
                        submitter = sqlDt.Rows[i]["submitter"].ToString(),
                        category = sqlDt.Rows[i]["category"].ToString()
                    });
                }
  
                //convert the list of suggestionsto an array and return!
                return suggestions.ToArray();
            }
            else
            {
                //if they're not logged in, return an empty array
                return new Suggestion[0];
            }
        }

        //Grabbing all of the suggestions that have been approved. 
        [WebMethod(EnableSession = true)]
        public Suggestion[] LoadSuggestions()
        {
            DataTable sqlDt = new DataTable("suggestions");

            string sqlConnectString = System.Configuration.ConfigurationManager.ConnectionStrings["myDB"].ConnectionString;
            string sqlSelect = "select id, `desc`, submitter, category from suggestions where approved=1 order by id";

            MySqlConnection sqlConnection = new MySqlConnection(sqlConnectString);
            MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);

            MySqlDataAdapter sqlDa = new MySqlDataAdapter(sqlCommand);
            sqlDa.Fill(sqlDt);

            List<Suggestion> suggestions = new List<Suggestion>();
            for (int i = 0; i < sqlDt.Rows.Count; i++)
            {

                suggestions.Add(new Suggestion
                {
                    id = Convert.ToInt32(sqlDt.Rows[i]["id"]),
                    desc = sqlDt.Rows[i]["desc"].ToString(),
                    submitter = sqlDt.Rows[i]["submitter"].ToString(),
                    category = sqlDt.Rows[i]["category"].ToString()
                });
            }

            //convert the list of suggestionsto an array and return!
            return suggestions.ToArray();
        }

        //Delete a suggestion
        [WebMethod(EnableSession = true)]
        public void DenySuggestion(string id)
        {
            if (Convert.ToInt32(Session["admin"]) == 1)
            {
                string sqlConnectString = System.Configuration.ConfigurationManager.ConnectionStrings["myDB"].ConnectionString;
                //this is a simple update, with parameters to pass in values
                string sqlSelect = "delete from suggestions where id=@idValue";

                MySqlConnection sqlConnection = new MySqlConnection(sqlConnectString);
                MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);

                sqlCommand.Parameters.AddWithValue("@idValue", HttpUtility.UrlDecode(id));

                sqlConnection.Open();
                try
                {
                    sqlCommand.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                }
                sqlConnection.Close();
            }
        }

        //Suggestion Approved
        [WebMethod(EnableSession = true)]
        public void ApproveSuggestion(string id)
        {
            if (Convert.ToInt32(Session["admin"]) == 1)
            {
                string sqlConnectString = System.Configuration.ConfigurationManager.ConnectionStrings["myDB"].ConnectionString;
                //this is a simple update, with parameters to pass in values
                string sqlSelect = "update suggestions set approved=1 where id=@idValue";

                MySqlConnection sqlConnection = new MySqlConnection(sqlConnectString);
                MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);

                sqlCommand.Parameters.AddWithValue("@idValue", HttpUtility.UrlDecode(id));

                sqlConnection.Open();
                try
                {
                    sqlCommand.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                }
                sqlConnection.Close();
            }
        }


        //Grabbing all of the Accounts that have not been approved yet. 
        [WebMethod(EnableSession = true)]
        public Account[] GetUnapprovedAccounts()
        {
            if (Convert.ToInt32(Session["admin"]) == 1)
            {
                DataTable sqlDt = new DataTable("accounts");

                string sqlConnectString = System.Configuration.ConfigurationManager.ConnectionStrings["myDB"].ConnectionString;
                string sqlSelect = "select id, firstName, lastName, userName, emailAddress from Accounts where approved=0 order by id";

                MySqlConnection sqlConnection = new MySqlConnection(sqlConnectString);
                MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);

                MySqlDataAdapter sqlDa = new MySqlDataAdapter(sqlCommand);
                sqlDa.Fill(sqlDt);

                List<Account> account = new List<Account>();
                for (int i = 0; i < sqlDt.Rows.Count; i++)
                {

                    account.Add(new Account
                    {
                        id = Convert.ToInt32(sqlDt.Rows[i]["id"]),
                        firstName = sqlDt.Rows[i]["firstName"].ToString(),
                        lastName = sqlDt.Rows[i]["lastName"].ToString(),
                        userName = sqlDt.Rows[i]["userName"].ToString(),
                        emailAddress = sqlDt.Rows[i]["emailAddress"].ToString()
                    });
                }

                //convert the list of suggestionsto an array and return!
                return account.ToArray();
            }
            else
            {
                //if they're not logged in, return an empty array
                return new Account[0];
            }
        }


        //Deny an Account
        [WebMethod(EnableSession = true)]
        public void DenyAccount(string id)
        {
            if (Convert.ToInt32(Session["admin"]) == 1)
            {
                string sqlConnectString = System.Configuration.ConfigurationManager.ConnectionStrings["myDB"].ConnectionString;
                //this is a simple update, with parameters to pass in values
                string sqlSelect = "delete from Accounts where id=@idValue";

                MySqlConnection sqlConnection = new MySqlConnection(sqlConnectString);
                MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);

                sqlCommand.Parameters.AddWithValue("@idValue", HttpUtility.UrlDecode(id));

                sqlConnection.Open();
                try
                {
                    sqlCommand.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                }
                sqlConnection.Close();
            }
        }

        //Approve an Account
        [WebMethod(EnableSession = true)]
        public void ApproveAccount(string id)
        {
            if (Convert.ToInt32(Session["admin"]) == 1)
            {
                string sqlConnectString = System.Configuration.ConfigurationManager.ConnectionStrings["myDB"].ConnectionString;
                //this is a simple update, with parameters to pass in values
                string sqlSelect = "update Accounts set approved=1 where id=@idValue";

                MySqlConnection sqlConnection = new MySqlConnection(sqlConnectString);
                MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);

                sqlCommand.Parameters.AddWithValue("@idValue", HttpUtility.UrlDecode(id));

                sqlConnection.Open();
                try
                {
                    sqlCommand.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                }
                sqlConnection.Close();
            }
        }

        [WebMethod(EnableSession = true)]
        public bool IsAdmin()
        {
            bool success = false;
            if (Convert.ToInt32(Session["admin"]) == 1)
                success = true;
            else
                success = false;
            return success;
        }

        //Create a new account request
        [WebMethod(EnableSession = true)]
        public void NewAccountRequest(string firstName, string lastName, string userName, string emailAddress, string pwd)
        {
            string sqlConnectString = System.Configuration.ConfigurationManager.ConnectionStrings["myDB"].ConnectionString;
            //the only thing fancy about this query is SELECT LAST_INSERT_ID() at the end.  All that
            //does is tell mySql server to return the primary key of the last inserted row.
            string sqlSelect = "insert into Accounts (firstName, lastName, userName, pwd, emailAddress, 0, 0, 0) " +
                "values(@firstNameValue, @lastNameValue, @userNameValue, @pwdValue, @emailAddressValue); SELECT LAST_INSERT_ID();";

            MySqlConnection sqlConnection = new MySqlConnection(sqlConnectString);
            MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);

            sqlCommand.Parameters.AddWithValue("@firstNameValue", HttpUtility.UrlDecode(firstName));
            sqlCommand.Parameters.AddWithValue("@lastNameValue", HttpUtility.UrlDecode(lastName));
            sqlCommand.Parameters.AddWithValue("@userNameValue", HttpUtility.UrlDecode(userName));
            sqlCommand.Parameters.AddWithValue("@pwdValue", HttpUtility.UrlDecode(pwd));
            sqlCommand.Parameters.AddWithValue("@emailAddressValue", HttpUtility.UrlDecode(emailAddress));


            //this time, we're not using a data adapter to fill a data table.  We're just
            //opening the connection, telling our command to "executescalar" which says basically
            //execute the query and just hand me back the number the query returns (the ID, remember?).
            //don't forget to close the connection!
            sqlConnection.Open();
            //we're using a try/catch so that if the query errors out we can handle it gracefully
            //by closing the connection and moving on
            try
            {
                int accountID = Convert.ToInt32(sqlCommand.ExecuteScalar());
                //here, you could use this accountID for additional queries regarding
                //the requested account.  Really this is just an example to show you
                //a query where you get the primary key of the inserted row back from
                //the database!
            }
            catch (Exception e)
            {
            }
            sqlConnection.Close();
        }

    }
}
