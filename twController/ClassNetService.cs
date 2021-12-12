using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using System.IO;

namespace twController
{
    /// <summary>
    /// the online service support class.
    /// all the functions to consume PST web server are encapsulated in this class. 
    /// The counterpart of ClassServiceAgent which uses commander.exe to access online services. This class employs 
    /// commander.jar to access online services.
    /// 
    /// </summary>
    class ClassNetService
    {
        static public string _gLoginUsername = string.Empty;
        static public string _gLoginKeyXml = string.Empty;

        //send report to server , operation ID = 4 

        /// <summary>
        /// send given report and log which belong to the given user to server
        /// Automatically cache report and log file when failed to send report. 
        ///  
        /// </summary>
        /// <param name="sURL">The service URL</param>
        /// <param name="sRptFile">Report file</param>
        /// <param name="sUser">User name</param>
        /// <param name="sLogZip"></param>
        /// <returns>0 - failure; 1 - success</returns>
        public static int SendReport(string sURL, string sRptFile, string sUser, string sLogZip)  
        {
            int nRet = 0;
            string app = "java.exe";
            try
            {
                envClass.getInstance().LogIt(string.Format("SendReport: ++ url={0}, report={1}, user={2}, log={3}", sURL, sRptFile, sUser, sLogZip));
                envClass.getInstance().LogIt(string.Format("Dump transaction: {0}", System.IO.File.ReadAllText(sRptFile)));
                //if (System.IO.File.Exists(sRptFile))
                //{
                //if (System.IO.File.Exists(app))
                {
                    //Trace.WriteLine("Send Report to server...");
                    Process mDosProcess = new Process();
                    mDosProcess.StartInfo.FileName = app;
                    mDosProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

                    mDosProcess.StartInfo.Arguments = string.Format("-jar commander.jar url=\"{0}\" xml=\"{1}\" username=\"{2}\" zippath=\"{3}\" op=\"4\"", sURL, sRptFile, sUser, sLogZip);
                    //mDosProcess.StartInfo.Arguments = string.Format("{0} {1} {2}", "true" , ClassLogin.IDUser, ClassLogin.IDStore);
                    mDosProcess.StartInfo.WorkingDirectory = System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
                    Trace.WriteLine(app + " is going to start with: " + mDosProcess.StartInfo.Arguments);

                    mDosProcess.Start();

                    mDosProcess.WaitForExit();
                    nRet = mDosProcess.ExitCode;
                    //Trace.WriteLine("Send transaction. Return code is " + nRet);

                }
                //else
                //{
                //Trace.WriteLine("Missing " + app);
                //}
                //}
                //else
                //{
                //Trace.WriteLine("Invalid report file");
                //}
                envClass.getInstance().LogIt(string.Format("SendReport: -- ret={0}", nRet));
            }
            catch (Exception) { }
            return nRet;

        }

        //return 1: info1 > info2
        //return 0: info1 == info2
        //return -1:info1 < info2
        public static int CompareFileVersion(FileVersionInfo info1, FileVersionInfo info2)
        {
            if (info1.FileMajorPart > info2.FileMajorPart)
            {
                return 1;
            }
            else if (info1.FileMajorPart < info2.FileMajorPart)
            {
                return -1;
            }
            else
            {// ==
                if (info1.FileMinorPart > info2.FileMinorPart)
                {
                    return 1;
                }
                else if (info1.FileMinorPart < info2.FileMinorPart)
                {
                    return -1;
                }
                else
                {// ==
                    if (info1.FileBuildPart > info2.FileBuildPart)
                    {
                        return 1;
                    }
                    else if (info1.FileBuildPart < info2.FileBuildPart)
                    {
                        return -1;
                    }
                    else
                    {
                        if (info1.FilePrivatePart > info2.FilePrivatePart)
                        {
                            return 1;
                        }
                        else if (info1.FilePrivatePart < info2.FilePrivatePart)
                        {
                            return -1;
                        }
                        else
                        {
                            return 0;
                        }

                    }

                }
            }
        }


        static public string XOREncrypt(string ori, byte bKey)
        {
            System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding();


            byte[] mybuff = encoding.GetBytes(ori);


            for (int i = 0; i < mybuff.Length; i++)
            {
                mybuff[i] = (byte)(mybuff[i] ^ bKey);
            }
            string encrypted = Convert.ToBase64String(mybuff);

            return encrypted;
        }

        public static int CheckPassword(string sLoc, string username, string password, string url)
        {
            int nRet = 0;
            string app = sLoc + "\\" + "Commander.exe";
            try
            {
                Trace.WriteLine("Check password ...");

                if (url.Length != 0 && username.Length != 0 && password.Length != 0)
                {
                    if (System.IO.File.Exists(app))
                    {
                        Trace.WriteLine("get number of remaining valid days of the current password...");
                        Process mDosProcess = new Process();

                        mDosProcess.StartInfo.FileName = app;
                        mDosProcess.StartInfo.Arguments = string.Format("url=\"{0}\" username=\"{1}\" password=\"{2}\" op=\"8\"", url, username, password);
                        mDosProcess.StartInfo.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory;
                        mDosProcess.Start();

                        mDosProcess.WaitForExit();
                        nRet = mDosProcess.ExitCode;
                    }
                    else
                    {
                        Trace.WriteLine("Missing " + app);
                    }
                }
                else
                {
                    Trace.WriteLine("Invalid url/username/password to login");
                }
            }
            catch (Exception) { }
            return nRet;
        }

        /// <summary>
        ///  send cached records which belongs to given user to server;
        ///  or send all cached records to server if the user name is not given
        /// </summary>
        /// <param name="sURL"></param>
        /// <param name="sUser"></param>
        /// <returns>0 - failure; 1 - success</returns>
        public static int SendOfflineReport(string sURL, string sUser)
        {
            int nRet = 0;
            string app = "java.exe";
            try
            {
            if (File.Exists(System.IO.Path.Combine(System.Environment.GetEnvironmentVariable("APSTHOME"), "commander.jar")))
            {
                //if (System.IO.File.Exists(app))
                {
                    Trace.WriteLine("Send offline Report to server...");
                    Process mDosProcess = new Process();
                    mDosProcess.StartInfo.FileName = app;
                    mDosProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

                    mDosProcess.StartInfo.Arguments = string.Format("-jar commander.jar url=\"{0}\" username=\"{1}\" op=\"4\"", sURL, sUser);
                    //mDosProcess.StartInfo.Arguments = string.Format("{0} {1} {2}", "true" , ClassLogin.IDUser, ClassLogin.IDStore);
                    mDosProcess.StartInfo.WorkingDirectory = System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
                    Trace.WriteLine(app + " is going to start with: " + mDosProcess.StartInfo.Arguments);

                    mDosProcess.Start();

                    mDosProcess.WaitForExit();
                    nRet = mDosProcess.ExitCode;
                    Trace.WriteLine("Send transaction. Return code is " + nRet);

                }
                //else
                //{
                //    Trace.WriteLine("Missing " + app);
                //}
            }
            //else
            //{
            //    Trace.WriteLine("Invalid report file");
            //}
            }
            catch (System.Exception )
            {

            }

            return nRet;

        }


        /// <summary>
        /// get number of cached records for user if the name is given, or get numbers of all cached records if user name if not given
        /// </summary>
        /// <param name="sUser">the user name</param>
        /// <returns>the number of records</returns>
        public static int getOfflineRecordNum(string sUser)
        {
            int nRet = 0;
            string app = "commander.exe";
            try
            {
            Trace.WriteLine("get amount of offline record ...");
            Process mDosProcess = new Process();
            mDosProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

            mDosProcess.StartInfo.FileName = app;
            mDosProcess.StartInfo.Arguments = string.Format("username=\"{0}\" op=\"10\"", sUser);
            //mDosProcess.StartInfo.Arguments = string.Format("{0} {1} {2}", "true" , ClassLogin.IDUser, ClassLogin.IDStore);
            mDosProcess.StartInfo.WorkingDirectory = System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            Trace.WriteLine(app + " is going to start with: " + mDosProcess.StartInfo.Arguments);

            mDosProcess.Start();

            mDosProcess.WaitForExit();
            nRet = mDosProcess.ExitCode;
            Trace.WriteLine("Send transaction. Return code is " + nRet);
            }
            catch (System.Exception ex)
            {

            }

            return nRet;

        }

        //check internet connection
        [DllImport("wininet.dll")]
        public extern static bool InternetGetConnectedState(out int connectionDescription, int reservedValue);
        public static bool IsConnected()
        {
            int I = 0;
            bool state = InternetGetConnectedState(out I, 0);
            return state;
        }


        /// <summary>
        /// validate user.
        /// When network and FD app server are functioning, the credential of user will be validated thru server
        /// If network or FD app server is not functioning, the request will otherwise go through cache DB to see whether the user is a returned user.
        /// </summary>
        /// <param name="username">user name</param>
        /// <param name="password">password</param>
        /// <param name="url">server address</param>
        /// <returns>1 - success; 0 - failure</returns>
        public static int UserLogin(string username, string password, string url)
        {

            int nRet = 0;

            string app = "commander.exe";
            try
            {
            if (url.Length != 0 && username.Length != 0 && password.Length != 0)
            {
                Trace.WriteLine("user log in...");
                Process mDosProcess = new Process();
                mDosProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

                mDosProcess.StartInfo.FileName = app;
                mDosProcess.StartInfo.Arguments = string.Format("url=\"{0}\" username=\"{1}\" password=\"{2}\" op=\"1\"", url, username, password);
                //mDosProcess.StartInfo.Arguments = string.Format("{0} {1} {2}", "true" , ClassLogin.IDUser, ClassLogin.IDStore);
                mDosProcess.StartInfo.WorkingDirectory = System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
                mDosProcess.Start();
                Trace.WriteLine(app + " is going to start with: " + mDosProcess.StartInfo.Arguments);

                mDosProcess.WaitForExit();
                nRet = mDosProcess.ExitCode;

            }
            else
            {
                Trace.WriteLine("Invalid url/username/password to login");
            }
            }
            catch (System.Exception ex)
            {

            }

            return nRet;
        }


        /// <summary>
        /// user event
        /// </summary>
        /// <param name="sURL"></param>
        /// <param name="sOpID"></param>
        /// <param name="sSrcModel"></param>
        /// <param name="sTgtModel"></param>
        /// <param name="sDescription"></param>
        /// <param name="sAttach"></param>
        /// <param name="sCate"></param>
        /// <param name="sStatus"></param>
        /// <returns></returns>
        public static int APSTLoginEvent(string sURL, string sOpID, string sSrcModel, string sTgtModel, string sDescription, string sAttach, string sCate, string sStatus)
        {
            int nRet = 0;
            string app = "commander.exe";
            try
            {
            DateTime dt = DateTime.Now;
            string datePatt = @"yyyy-MM-dd HH:mm:ss";

            string sTime = dt.ToString(datePatt);
            string sStation = System.Environment.MachineName;

            IPAddress[] localIPs = Dns.GetHostAddresses(Dns.GetHostName());
            //take first one
            string sIP = localIPs[0].ToString();


            Trace.WriteLine("[APSTLoginEvent] Log event ...");
            Process mDosProcess = new Process();
            mDosProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            mDosProcess.StartInfo.FileName = app;
            mDosProcess.StartInfo.Arguments = string.Format("url=\"{0}\" username=\"{1}\" timestamp=\"{2}\" stationname=\"{3}\" hostip=\"{4}\" smodel=\"{5}\" dmodel=\"{6}\" desc=\"{7}\" attach=\"{8}\" category=\"{9}\" status=\"{10}\" op=\"5\"",
                sURL,
                sOpID,
                sTime,
                sStation,
                sIP,
                sSrcModel,
                sTgtModel,
                sDescription,
                sAttach,
                sCate,
                sStatus);
            mDosProcess.StartInfo.WorkingDirectory = System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            mDosProcess.Start();
            Trace.WriteLine(app + " is going to start with : " + mDosProcess.StartInfo.Arguments);

            mDosProcess.WaitForExit();
            nRet = mDosProcess.ExitCode;

            }
            catch (System.Exception ex)
            {

            }
            return nRet;
        }

        /// <summary>
        /// send pending ESN to server. All the fields can be null but ESN.
        /// </summary>
        /// <param name="sURL"></param>
        /// <param name="sESN"></param>
        /// <param name="sMSL"></param>
        /// <param name="sPhoneID"></param>
        /// <param name="sOpID"></param>
        /// <returns></returns>
        public static int SendPendingMSL(string sURL, string sESN, string sMSL, string sPhoneID, string sOpID)
        {
            int nRet = 0;
            string app = "commander.exe";
            try
            {
            string datePatt = @"yyyy-MM-dd HH:mm:ss";

            string sTime = DateTime.Now.ToString(datePatt);

            string sStation = System.Environment.MachineName;

            Trace.WriteLine("[SendPendingMSL] Send MSL ...");
            Process mDosProcess = new Process();
            
            mDosProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            mDosProcess.StartInfo.FileName = app;
            mDosProcess.StartInfo.Arguments = string.Format("url=\"{0}\" esn=\"{1}\" msl=\"{2}\" stationname=\"{3}\" model=\"{4}\" operatorid=\"{5}\" timestamp=\"{6}\" op=\"3\"",
                sURL,
                sESN,
                sMSL,
                sStation, //workstation name
                sPhoneID,
                sOpID,
                sTime);
            mDosProcess.StartInfo.WorkingDirectory = System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            mDosProcess.Start();
            Trace.WriteLine(app + " is going to start with : " + mDosProcess.StartInfo.Arguments);

            mDosProcess.WaitForExit();
            nRet = mDosProcess.ExitCode;
            }
            catch (System.Exception ex)
            {

            }

            return nRet;
        }


        /// <summary>
        /// change password. refer to document for specific criterion of validating password
        /// </summary>
        /// <param name="sURL">server interface address</param>
        /// <param name="sUser">user name</param>
        /// <param name="oldpw">existing password</param>
        /// <param name="newpw">new password</param>
        /// <returns></returns>
        public static int changePassword(string sURL, string sUser, string oldpw, string newpw)
        {
            int nRet = 0;
            string app = "commander.exe";
            try
            {
            Trace.WriteLine("change password for user -" + sUser);
            Process mDosProcess = new Process();
            mDosProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

            mDosProcess.StartInfo.FileName = app;
            mDosProcess.StartInfo.Arguments = string.Format("url=\"{0}\" username=\"{1}\" oldpassword=\"{2}\" newpassword=\"{3}\" op=\"9\"", sURL, sUser, oldpw, newpw);
            mDosProcess.StartInfo.WorkingDirectory = System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            Trace.WriteLine(app + " is going to start with: " + mDosProcess.StartInfo.Arguments);

            mDosProcess.Start();

            mDosProcess.WaitForExit();
            nRet = mDosProcess.ExitCode;
            Trace.WriteLine("change password done. Return code is " + nRet.ToString());
            }
            catch (System.Exception ex)
            {

            }

            return nRet;

        }

    }
}
