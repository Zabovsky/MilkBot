using MilkBot.TelegramMarkup;
using System.Data;
using System.Data.SQLite;
using System.Text.Json;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace MilkBot
{
    public static class DataAccess
    {
        private static readonly string _connectionString = "Data Source=milk.db;Version=3;Pooling=False;";
        private static readonly object _dbLock = new(); // для синхронизации

        static DataAccess()
        {
            InitializeDatabase();
        }

        public static void InitializeDatabase()
        {
            try
            {
                lock (_dbLock)
                {
                    using (var connection = new SQLiteConnection(_connectionString))
                    {
                        connection.Open();
                        string createTableQuery = @"
                            CREATE TABLE IF NOT EXISTS Transactions (
                                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                UserId TEXT,
                                UserName TEXT,
                                TransactionType TEXT,
                                Amount REAL,
                                TransactionDate DATETIME DEFAULT CURRENT_TIMESTAMP
                            )";
                        using (var command = new SQLiteCommand(createTableQuery, connection))
                        {
                            command.ExecuteNonQuery();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogError("Ошибка при инициализации базы данных", ex);
            }
        }
        public static DataTable GetAllUsers()
        {
            var dt = new DataTable();
            try
            {
                lock (_dbLock)
                {
                    using (var connection = new SQLiteConnection(_connectionString))
                    {
                        connection.Open();
                        string query = @"
                    SELECT 
                        UserId, 
                        UserName,
                        SUM(CASE WHEN TransactionType = 'BUY' THEN Amount ELSE 0 END) AS TotalBought
                    FROM Transactions
                    GROUP BY UserId, UserName
                    ORDER BY TotalBought DESC";
                        using (var command = new SQLiteCommand(query, connection))
                        {
                            SQLiteDataAdapter adapter = new SQLiteDataAdapter(command);
                            adapter.Fill(dt);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogError("Ошибка при получении списка пользователей", ex);
            }
            return dt;
        }
        public static bool CancelLastBuy(string userId)
        {
            try
            {
                lock (_dbLock)
                {
                    using (var connection = new SQLiteConnection(_connectionString))
                    {
                        connection.Open();

                        // Ищем последнюю покупку (BUY), не старше часа
                        string selectQuery = @"
                    SELECT Id, TransactionDate
                    FROM Transactions
                    WHERE UserId = @UserId AND TransactionType = 'BUY'
                    ORDER BY TransactionDate DESC
                    LIMIT 1";

                        using (var selectCmd = new SQLiteCommand(selectQuery, connection))
                        {
                            selectCmd.Parameters.AddWithValue("@UserId", userId);
                            using (var reader = selectCmd.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    int id = Convert.ToInt32(reader["Id"]);
                                    DateTime time = Convert.ToDateTime(reader["TransactionDate"]);

                                    if ((DateTime.Now - time).TotalMinutes <= 60)
                                    {
                                        // Удаляем транзакцию
                                        string deleteQuery = "DELETE FROM Transactions WHERE Id = @Id";
                                        using (var deleteCmd = new SQLiteCommand(deleteQuery, connection))
                                        {
                                            deleteCmd.Parameters.AddWithValue("@Id", id);
                                            deleteCmd.ExecuteNonQuery();
                                            return true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogError("Ошибка при отмене последней покупки", ex);
            }

            return false;
        }
        public static bool ReplaceTodayAmount(string userId, decimal newAmount)
        {
            try
            {
                lock (_dbLock)
                {
                    using (var connection = new SQLiteConnection(_connectionString))
                    {
                        connection.Open();

                        using (var tran = connection.BeginTransaction())
                        {
                            // Удаляем все сегодняшние покупки пользователя
                            string deleteQuery = @"
                        DELETE FROM Transactions 
                        WHERE UserId = @UserId 
                          AND TransactionType = 'BUY'
                          AND date(TransactionDate) = @Date";

                            using (var deleteCmd = new SQLiteCommand(deleteQuery, connection, tran))
                            {
                                deleteCmd.Parameters.AddWithValue("@UserId", userId);
                                deleteCmd.Parameters.AddWithValue("@Date", DateTime.Today.ToString("yyyy-MM-dd"));
                                deleteCmd.ExecuteNonQuery();
                            }

                            // Добавляем новую покупку
                            string insertQuery = @"
                        INSERT INTO Transactions (UserId, UserName, TransactionType, Amount)
                        VALUES (@UserId, @UserName, 'BUY', @Amount)";
                            string userName = GetLatestUserName(userId, connection);

                            using (var insertCmd = new SQLiteCommand(insertQuery, connection, tran))
                            {
                                insertCmd.Parameters.AddWithValue("@UserId", userId);
                                insertCmd.Parameters.AddWithValue("@UserName", userName ?? "Имя не найдено");
                                insertCmd.Parameters.AddWithValue("@Amount", newAmount);
                                insertCmd.ExecuteNonQuery();
                            }

                            tran.Commit();
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogError("Ошибка при редактировании количества молока", ex);
                return false;
            }
        }

        private static string GetLatestUserName(string userId, SQLiteConnection connection)
        {
            string name = null;
            string query = "SELECT UserName FROM Transactions WHERE UserId = @UserId ORDER BY TransactionDate DESC LIMIT 1";
            using (var cmd = new SQLiteCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@UserId", userId);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                        name = reader["UserName"]?.ToString();
                }
            }
            return name;
        }

        public static void AddTransaction(string userId, string userName, string transactionType, decimal amount, DateTime? date = null)
        {
            try
            {
                lock (_dbLock)
                {
                    using (var connection = new SQLiteConnection(_connectionString))
                    {
                        connection.Open();
                        string insertQuery = @"
                        INSERT INTO Transactions (UserId, UserName, TransactionType, Amount, TransactionDate)
                        VALUES (@UserId, @UserName, @TransactionType, @Amount, @Date)
                        ";
                        using (var command = new SQLiteCommand(insertQuery, connection))
                        {
                            command.Parameters.AddWithValue("@UserId", userId);
                            command.Parameters.AddWithValue("@UserName", userName);
                            command.Parameters.AddWithValue("@TransactionType", transactionType);
                            command.Parameters.AddWithValue("@Amount", amount);
                            command.Parameters.AddWithValue("@Date", date);
                            command.ExecuteNonQuery();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogError("Ошибка при добавлении транзакции", ex);
            }
        }

        public static DataTable GetDailySummary(DateTime date)
        {
            var dt = new DataTable();
            try
            {
                lock (_dbLock)
                {
                    using (var connection = new SQLiteConnection(_connectionString))
                    {
                        connection.Open();
                        string query = @"
                            SELECT UserId, UserName, SUM(Amount) AS TotalBought
                            FROM Transactions
                            WHERE TransactionType = 'BUY'
                              AND date(TransactionDate) = @Date
                            GROUP BY UserId, UserName";
                        using (var command = new SQLiteCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@Date", date.ToString("yyyy-MM-dd"));
                            SQLiteDataAdapter adapter = new SQLiteDataAdapter(command);
                            adapter.Fill(dt);
                        }
                    }
                }

                if (dt.Rows.Count > 0)
                {
                    decimal overallTotal = 0;
                    foreach (DataRow row in dt.Rows)
                        overallTotal += Convert.ToDecimal(row["TotalBought"]);

                    DataRow totalRow = dt.NewRow();
                    totalRow["UserId"] = "";
                    totalRow["UserName"] = "ВСЕГО";
                    totalRow["TotalBought"] = overallTotal;
                    dt.Rows.Add(totalRow);
                }
            }
            catch (Exception ex)
            {
                LogError("Ошибка при получении дневной статистики", ex);
            }
            return dt;
        }


        public static DataTable GetPeriodSummary(DateTime startDate, DateTime endDate)
        {
            var dt = new DataTable();
            try
            {
                lock (_dbLock)
                {
                    using (var connection = new SQLiteConnection(_connectionString))
                    {
                        connection.Open();
                        string query = @"
                            SELECT 
                                UserId, 
                                UserName,
                                SUM(Amount) AS TotalBought
                            FROM Transactions
                            WHERE TransactionType = 'BUY'
                              AND date(TransactionDate) >= @StartDate 
                              AND date(TransactionDate) <= @EndDate
                            GROUP BY UserId, UserName";
                        using (var command = new SQLiteCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@StartDate", startDate.ToString("yyyy-MM-dd"));
                            command.Parameters.AddWithValue("@EndDate", endDate.ToString("yyyy-MM-dd"));
                            SQLiteDataAdapter adapter = new SQLiteDataAdapter(command);
                            adapter.Fill(dt);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogError("Ошибка при получении периодической статистики", ex);
            }
            return dt;
        }

        private static void LogError(string context, Exception ex)
        {
            try
            {
                File.AppendAllText("db_errors.log",
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {context}: {ex.Message}\n{ex.StackTrace}\n\n");
            }
            catch { /* игнорируем, если не удалось логировать */ }
        }
    }
}
