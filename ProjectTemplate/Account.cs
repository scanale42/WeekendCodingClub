using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ProjectTemplate
{
    public class Account
    {
        public int id;
        public string firstName;
        public string lastName;
        public string userName;
        public string emailAddress;
        public string password;
        public string secQuestion;
        public string secAnswer;
        public string active;
        public string admin;
        public string lockedOut;
        public string invalidAttempts;
    }
}