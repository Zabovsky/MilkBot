using System;
using System.Data;
using System.Data.SQLite;

namespace MilkBot
{
    public static class DataAccess
    {
        private static readonly string _connectionString = "Data Source=milk.db;Version=3;";

        static DataAccess()
        {
            InitializeDatabase();
        }

        // Теперь таблица содержит столбец UserName для сохранения имени пользователя
        public static void InitializeDatabase()
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

        // Добавляем транзакцию с сохранением UserId и UserName
        public static void AddTransaction(string userId, string userName, string transactionType, decimal amount)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                string insertQuery = @"
                    INSERT INTO Transactions (UserId, UserName, TransactionType, Amount)
                    VALUES (@UserId, @UserName, @TransactionType, @Amount)";
                using (var command = new SQLiteCommand(insertQuery, connection))
                {
                    command.Parameters.AddWithValue("@UserId", userId);
                    command.Parameters.AddWithValue("@UserName", userName);
                    command.Parameters.AddWithValue("@TransactionType", transactionType);
                    command.Parameters.AddWithValue("@Amount", amount);
                    command.ExecuteNonQuery();
                }
            }
        }

        // Получение сводки по покупкам (только BUY) за выбранный день
        public static DataTable GetDailySummary(DateTime date)
        {
            DataTable dt = new DataTable();
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
            // Вычисляем общий итог и добавляем дополнительную строку с суммой
            if (dt.Rows.Count > 0)
            {
                decimal overallTotal = 0;
                foreach (DataRow row in dt.Rows)
                {
                    overallTotal += Convert.ToDecimal(row["TotalBought"]);
                }
                DataRow totalRow = dt.NewRow();
                totalRow["UserId"] = "";
                totalRow["UserName"] = "ВСЕГО";
                totalRow["TotalBought"] = overallTotal;
                dt.Rows.Add(totalRow);
            }
            return dt;
        }

        // Метод для получения сводки за произвольный период (неделя, месяц, год) остается без изменений,
        // но при необходимости его можно дополнить аналогичным образом.
        public static DataTable GetPeriodSummary(DateTime startDate, DateTime endDate)
        {
            DataTable dt = new DataTable();
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
            return dt;
        }
    }
}
