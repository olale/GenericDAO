using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.SqlServer.Management.Smo;
using System.Configuration;
using FluentAssertions;
using System.IO;
using System.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using System.Data;
using System.Transactions;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Text;
using System.Threading;
using Core;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace Tests
{
    public class ProgramSetting
    {
        public ProgramSetting()
        {

        }

        public string Data
        {
            get;
            set;
        }

    }
    [TestClass]
    public class IntegrationTestBase : IDisposable
    {
        public static string BackupFile
        {
            get;
            set;
        }
        protected internal TransactionScope TransactionScope
        {
            get;
            set;
        }
        protected internal static string DataBaseName
        {
            get
            {
                return ConfigurationManager.AppSettings["Database"];
            }
        }

        private static SqlConnection GetConnection()
        {
            return new SqlConnection("Server=localhost;Database=" +
                                     DataBaseName +
                                     ";Trusted_Connection=True;");
        }

        private static RelocateFile GetRelocatedFile(Server server,
                                                     Restore restoreAction,
                                                     int index,
                                                     bool IsData)
        {
            var RelocatedFile = new RelocateFile();
            DataRow destinationServerLogFile = restoreAction.ReadFileList(server).Rows[index];
            string physicalFileName = IsData ? server.DefaultFile : server.DefaultLog;
            RelocatedFile.LogicalFileName = (string)destinationServerLogFile[0];
            RelocatedFile.PhysicalFileName = String.Format("{0}{1}{2}{3}",
                                                           physicalFileName,
                                                           Path.DirectorySeparatorChar,
                                                           DataBaseName,
                                                           (IsData ? ".mdf" : ".ldf"));
            return RelocatedFile;
        }

        private static IEnumerable<string> MigrationScripts
        {
            get;
            set;
        }
        private static string TFSServerURI
        {
            get
            {
                return "http://tctfs02:8080/tfs/defaultcollection";
            }
        }

        private static string WorkspaceName
        {
            get
            {
                return Environment.MachineName;
            }
        }

        public static string DataProjectName
        {
            get
            {
                return "Project";
            }
        }

        private static string LocalProjectDir(string projectName)
        {
                var projects = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(new Uri(TFSServerURI));
                projects.Authenticate();

                var workspace = projects
                    .GetService<VersionControlServer>()
                    .GetWorkspace(WorkspaceName, Environment.UserName);
                IEnumerable<string> localFolders = workspace.Folders
                                    .Select(f => f.LocalItem)
                                    .SelectMany(f => Directory.GetDirectories(f, "*", SearchOption.AllDirectories));
                var localProjectFolder = localFolders
                    .FirstOrDefault(f => f.EndsWith(projectName));
                return localProjectFolder;

        }

        private static string DBUpgradeInstallationDir
        {
            get
            {
                return string.Format(@"{0}/TCEL Projects/Database/Installation/Server Installation/Db/UpgrDB/", LocalProjectDir(DataProjectName));
            }
        }

        private static string StoredProcedureDirectory
        {
            get
            {
                return string.Format(@"{0}/TCEL Projects/Database/Data/StoredProcedures/", LocalProjectDir(DataProjectName));
            }
        }

        private static string ProjectName
        {
            get
            {
                return "GenericDAO";
            }
        }

        private static string TestProjectDir
        {
            get
            {
                return string.Format(@"{0}/Tests/", LocalProjectDir(ProjectName));
            }
        }

        /// <summary>
        /// For isolated tests of single SP:s that have been updated, this method can replace the old SP with a new one, given the filename convention dbo.SPNAME.PRC
        /// </summary>
        /// <param name="storedProcedureName"></param>
        protected internal static void UpdateSingleSP(string storedProcedureName)
        {
            var server = new Server();
            var db = server.Databases[DataBaseName];
            RunSqlScript(db, String.Format("{0}dbo.{1}.PRC", StoredProcedureDirectory, storedProcedureName));
        }

        /// <summary>
        /// Parse the RunUpgrade.bat file for SQL scripts to run during upgrade
        /// </summary>
        /// <returns></returns>
        private static IEnumerable<string> ParseUpgradeBatchFile()
        {
            var upgradeFileReader = new StreamReader(DBUpgradeInstallationDir +
                                                     "RunUpgr.bat");
            var upgradeScriptString = upgradeFileReader.ReadToEnd();
            var sqlcmdRegexp = new Regex(@"sqlcmd\s+.+?-i(?<FileName>.+?\.sql)");
            var sqlFileNameMatches = sqlcmdRegexp.Matches(upgradeScriptString);
            return sqlFileNameMatches.Cast<Match>().Select(m => m.Groups["FileName"].Value);
        }

        public IntegrationTestBase()
        {
            // If some migrations are run in single test methods, we cannot initialize a transaction scope for all tests here .
            // Initialize a transaction scope either after a migration is run in a test, or initialize a DB in a static constructor/setup method
            TransactionScope = new TransactionScope();
        }

        private static void RunSqlScript(Database db, string sqlScript)
        {
            var sqlString = new StreamReader(sqlScript).ReadToEnd();
            try
            {
                db.ExecuteNonQuery(sqlString, ExecutionTypes.ContinueOnError);
            }
            catch (FailedOperationException ex)
            {
                throw ex.InnerException;
            }
        }

        /// <summary>
        /// Run migration scripts valid for the current version of the DB
        /// </summary>
        protected internal static void RunMigrationScriptsUsingSMO()
        {
            var server = new Server();
            var db = server.Databases[DataBaseName];
            // Setting ID 40 = version number
            GenericDAO.AddPrefix<ProgramSetting>("ps");
            var setting = GenericDAO<ProgramSetting>.Get("ps_GetProgramSetting", new
            {
                SettingId = 40
            }).First();
            var dbVersion = setting.Data; // Retrieve the current DB version
            var versionRegexp = new Regex(@"(\d+)\.(\d+)\.(\d+)");
            foreach (var script in MigrationScripts)
            {
                var versionedScript = versionRegexp.IsMatch(script);

                // Skip migration scripts for older databases
                if (!versionedScript || (versionedScript && dbVersion.CompareTo(versionRegexp.Match(script).Value) <= 0))
                {
                    RunSqlScript(db, String.Format("{0}{1}", DBUpgradeInstallationDir, script));
                }
            }
        }

        static void ExecuteCommand(string dir,
                                   string command)
        {
            int ExitCode;
            ProcessStartInfo ProcessInfo;
            using (Process process = new Process())
            {
                ProcessInfo = new ProcessStartInfo("cmd.exe",
                                                   String.Format("/c \"{0}{1}\"",
                                                                 dir,
                                                                 command));
                ProcessInfo.CreateNoWindow = true;
                ProcessInfo.UseShellExecute = false;
                // *** Redirect the output ***
                ProcessInfo.RedirectStandardError = true;
                ProcessInfo.RedirectStandardOutput = false;
                ProcessInfo.WorkingDirectory = dir;

                // *** Read the streams ***
                StringBuilder errorStringBuilder = new StringBuilder();
                using (AutoResetEvent errorWaitHandle = new AutoResetEvent(false))
                {
                    process.ErrorDataReceived += (sender, e) =>
                    {
                        if (e.Data == null)
                        {
                            errorWaitHandle.Set();
                        }
                        else
                        {
                            errorStringBuilder.AppendLine(e.Data);
                        }
                    };
                }
                process.StartInfo = ProcessInfo;
                process.Start();
                process.BeginErrorReadLine();
                process.WaitForExit();

                ExitCode = process.ExitCode;
                string error = errorStringBuilder.ToString();
                //Console.WriteLine("output>> "+(String.IsNullOrEmpty(output)?"(none)":output));
                if (!string.IsNullOrEmpty(error))
                {
                    throw new Exception(error);
                }
            }
        }


        private static void RunMigrationScriptsFromBatchFile()
        {
            ExecuteCommand(Path.GetFullPath(DBUpgradeInstallationDir),
                           "RunUpgr.bat");
        }

        protected internal static void Init(string backupfileName)
        {
            BackupFile = backupfileName;
            Server server = new Server();


            var db = new Database(server, DataBaseName);
            if (!server.Databases.Contains(DataBaseName))
            {
                db.Create();
            }
            else
            {
                server.KillDatabase(DataBaseName);

            }
            Restore restoreAction = new Restore()
            {
                Action = RestoreActionType.Database,
                Database = DataBaseName,
                ReplaceDatabase = true
            };

            BackupDeviceItem backupFileItem = new BackupDeviceItem(Path.GetFullPath(String.Format("{0}/backups/{1}", TestProjectDir, BackupFile)),
                                                                   DeviceType.File);
            restoreAction.Devices.Add(backupFileItem);
            var DataFile = GetRelocatedFile(server,
                                            restoreAction,
                                            0,
                                            true);

            var LogFile = GetRelocatedFile(server,
                                           restoreAction,
                                           1,
                                           false);
            restoreAction.RelocateFiles.Add(DataFile);
            restoreAction.RelocateFiles.Add(LogFile);

            restoreAction.Complete += UpdateUser;
            restoreAction.PercentComplete += PercentComplete;

            restoreAction.SqlRestore(server);
            db.SetOnline();
            restoreAction.SqlVerify(server).Should().BeTrue();
        }

        private static void PercentComplete(object sender,
                                            PercentCompleteEventArgs e)
        {
            Console.WriteLine(String.Format("{0} % restore of DB {1} complete",
                                            e.Percent,
                                            DataBaseName));
        }

        private static void RunSQL(string sqlString,
                                   SqlConnection connection = null)
        {
            using (connection = connection ?? GetConnection())
            {
                SqlCommand cmd = new SqlCommand()
                {
                    CommandText = sqlString,
                    CommandType = CommandType.Text,
                    Connection = connection
                };
                connection.Open();
                cmd.ExecuteNonQuery();
            }
            connection.Close();
        }

        private static void UpdateUser(object obj,
                                       ServerMessageEventArgs e)
        {
            RunSQL("EXEC sp_change_users_login 'Update_One', 'TcDoctor', 'TcDoctor'");
        }



        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
                if (TransactionScope != null)
                {
                    TransactionScope.Dispose();
                    TransactionScope = null;
                }
        }

        ~IntegrationTestBase()
        {
            Dispose(false);
        }

        protected internal static string GetFilesDir()
        {
            return String.Format("{0}{1}..{1}..{1}files",
                                 AppDomain.CurrentDomain.BaseDirectory,
                                 Path.DirectorySeparatorChar);
        }
        protected static FileStream GetFileStream(string fileName)
        {
            return File.Open(String.Format("{0}{1}{2}",
                                           GetFilesDir(),
                                           Path.DirectorySeparatorChar,
                                           fileName),
                             FileMode.Open,
                             FileAccess.Read);
        }

        protected FileStream GetFileStream()
        {
            return GetFileStream(FileName);
        }

        public string FileName
        {
            get;
            set;
        }

        [AssemblyInitialize]
        public static void InitDB(TestContext context)
        {
            Init("TCL_Personec_172.bak");
            MigrationScripts = ParseUpgradeBatchFile();
            RunMigrationScriptsUsingSMO();
        }
    }
}