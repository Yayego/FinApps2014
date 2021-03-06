using Mono.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;

namespace FinApps2014.Wearable.App.Database {

    public class StepEntryDatabase {

        #region Static Members
        private static object locker = new object();
        #endregion

        #region Members
        public SqliteConnection connection;
        public string path;
        #endregion

        #region Constructors
        public StepEntryDatabase(string dbPath) {
            var output = "";
            path = dbPath;
            // create the tables
            bool exists = File.Exists(dbPath);

            if (!exists) {
                using (connection = new SqliteConnection("Data Source=" + dbPath)) {

                    connection.Open();
                    var commands = new[] {
						"CREATE TABLE [Items] (_id INTEGER PRIMARY KEY ASC, Steps BIGINT, Date NTEXT);"
					};
                    foreach (var command in commands) {
                        using (var c = connection.CreateCommand()) {
                            c.CommandText = command;
                            var i = c.ExecuteNonQuery();
                        }
                    }
                    connection.Close();
                }
            } else {
                // already exists, do nothing. 
            }
            Console.WriteLine(output);
        }
        #endregion

        #region Methods
        /// <summary>Convert from DataReader to Task object</summary>
        private StepEntry FromReader(SqliteDataReader r) {
            var t = new StepEntry();
            t.ID = Convert.ToInt32(r["_id"]);
            t.Steps = Convert.ToInt64(r["Steps"]);
            var date = r["Date"].ToString();
            var culture = CultureInfo.CreateSpecificCulture("en-US");
            var styles = DateTimeStyles.None;
            DateTime dateOut;
            if (!DateTime.TryParse(date, culture, styles, out dateOut)) {
                //back compat, but will never come in here really.
                DateTime.TryParse(date, out dateOut);
            }
            t.Date = dateOut;
            return t;
        }

        public IEnumerable<StepEntry> GetItems(int count) {
            var tl = new List<StepEntry>();

            lock (locker) {
                using (connection = new SqliteConnection("Data Source=" + path)) {
                    connection.Open();
                    using (var contents = connection.CreateCommand()) {
                        if (count == 0)
                            contents.CommandText = "SELECT [_id], [Steps], [Date] from [Items]";
                        else
                            contents.CommandText = "SELECT [_id], [Steps], [Date] from [Items] ORDER BY _id DESC LIMIT " + count;

                        var r = contents.ExecuteReader();
                        while (r.Read()) {
                            tl.Add(FromReader(r));
                        }
                        r.Close();
                    }
                    connection.Close();
                }
            }
            return tl;
        }

        public StepEntry GetItem(DateTime date) {
            var t = new StepEntry();
            lock (locker) {
                using (connection = new SqliteConnection("Data Source=" + path)) {
                    connection.Open();
                    using (var command = connection.CreateCommand()) {
                        command.CommandText = "SELECT [_id], [Steps], [Date] from [Items] WHERE [Date] = ?";
                        var culture = CultureInfo.CreateSpecificCulture("en-US");
                        command.Parameters.Add(new SqliteParameter(DbType.String) { Value = date.ToString("MM/dd/yyyy", culture) });
                        var r = command.ExecuteReader();
                        while (r.Read()) {
                            t = FromReader(r);
                            break;
                        }
                        r.Close();
                    }
                    connection.Close();
                }
            }
            return t;
        }

        public int SaveItem(StepEntry item) {
            int r;
            lock (locker) {
                if (item.ID != 0) {
                    using (connection = new SqliteConnection("Data Source=" + path)) {
                        connection.Open();
                        using (var command = connection.CreateCommand()) {
                            command.CommandText = "UPDATE [Items] SET [Steps] = ?, [Date] = ? WHERE [_id] = ?;";
                            command.Parameters.Add(new SqliteParameter(DbType.Int64) { Value = item.Steps });
                            var culture = CultureInfo.CreateSpecificCulture("en-US");
                            command.Parameters.Add(new SqliteParameter(DbType.String) { Value = item.Date.ToString("MM/dd/yyyy", culture) });
                            command.Parameters.Add(new SqliteParameter(DbType.Int32) { Value = item.ID });
                            r = command.ExecuteNonQuery();
                        }
                        connection.Close();
                    }
                    return r;
                } else {
                    using (connection = new SqliteConnection("Data Source=" + path)) {
                        connection.Open();
                        using (var command = connection.CreateCommand()) {
                            command.CommandText = "INSERT INTO [Items] ([Steps], [Date]) VALUES (? ,?)";
                            command.Parameters.Add(new SqliteParameter(DbType.Int64) { Value = item.Steps });
                            var culture = CultureInfo.CreateSpecificCulture("en-US");
                            command.Parameters.Add(new SqliteParameter(DbType.String) { Value = item.Date.ToString("MM/dd/yyyy", culture) });
                            r = command.ExecuteNonQuery();
                        }
                        connection.Close();
                    }
                    return r;
                }

            }
        }

        public int DeleteItem(int id) {
            lock (locker) {
                int r;
                using (connection = new SqliteConnection("Data Source=" + path)) {
                    connection.Open();
                    using (var command = connection.CreateCommand()) {
                        command.CommandText = "DELETE FROM [Items] WHERE [_id] = ?;";
                        command.Parameters.Add(new SqliteParameter(DbType.Int32) { Value = id });
                        r = command.ExecuteNonQuery();
                    }
                    connection.Close();
                }
                return r;
            }
        }
        #endregion

    }
}