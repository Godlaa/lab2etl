using Microsoft.Data.Sqlite;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lab2ETL
{
    class DataTransformer
    {
        private string sqliteConnString;
        private string pgConnString;

        public DataTransformer(string pgConnString)
        {
            this.pgConnString = pgConnString;
        }

        public void CreateSqliteTable()
        {
            string dbPath = @"D:\sqliteDB\orders_denormalized.db";
            string directory = Path.GetDirectoryName(dbPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            using (var sqliteConn = new SqliteConnection(sqliteConnString))
            {
                sqliteConn.Open();
                string ddl = File.ReadAllText("D:\\Lab2ETL\\DDL\\orders_denormalized.sql");
                var command = new SqliteCommand(ddl, sqliteConn);
                command.ExecuteNonQuery();
                sqliteConn.Close();
            }
        }

        public void CreatePostgresTable()
        {
            string ddlPath = Path.Combine("DDL", "normalized.sql");

            using (var pgConn = new NpgsqlConnection(pgConnString))
            {
                pgConn.Open();
                string ddl = File.ReadAllText(ddlPath);
                var command = new NpgsqlCommand(ddl, pgConn);
                command.ExecuteNonQuery();
                pgConn.Close();
            }
        }

        public void FillSqliteTable()
        {
            using (var sqliteConn = new SqliteConnection(sqliteConnString))
            {
                sqliteConn.Open();
                string dml = File.ReadAllText("D:\\Lab2ETL\\DML\\fill_denormalized.sql");
                var command = new SqliteCommand(dml, sqliteConn);
                command.ExecuteNonQuery();
                sqliteConn.Close();
            }
        }


        public async Task<List<Dictionary<string, object>>> ExtractDataAsync(int communicatorType)
        {
            Communicator communicator = communicatorType switch
            {
                (int)Communicator.Type.Socket => new SocketCommunicator(8080),
                (int)Communicator.Type.Queue => new QueueCommunicator("89.169.36.129", "orders"),
                _ => throw new ArgumentException("Неподдерживаемый тип коммуникатора")
            };

            return await communicator.GetMessage();
        }

        public async Task TransformAndLoadAsync(int communicatorType)
        {
            List<Dictionary<string, object>> records = await ExtractDataAsync(communicatorType);
            Console.ReadKey();
            if (records.Count == 0) throw new Exception("Нет данных для обработки!");

            Dictionary<string, int> customersCache = new Dictionary<string, int>();
            Dictionary<string, int> categoriesCache = new Dictionary<string, int>();
            Dictionary<string, int> productsCache = new Dictionary<string, int>();
            Dictionary<int, int> ordersCache = new Dictionary<int, int>();

            using (var pgConn = new NpgsqlConnection(pgConnString))
            {
                pgConn.Open();
                using (var transaction = pgConn.BeginTransaction())
                {
                    foreach (Dictionary<string, object> record in records)
                    {
                        DateTime orderDate = DateTime.Parse(record["order_date"].ToString());
                        string customerName = record["customer_name"].ToString();
                        string customerPhone = record["customer_phone"].ToString();
                        string productName = record["product_name"].ToString();
                        string productCategory = record["product_category"].ToString();
                        decimal productPrice = Convert.ToDecimal(record["product_price"]);
                        int quantity = Convert.ToInt32(record["quantity"]);

                        int custId;
                        if (!customersCache.TryGetValue(customerName, out custId))
                        {
                            using (var cmd = new NpgsqlCommand("INSERT INTO customers (customer_name, customer_phone) VALUES (@name, @phone) RETURNING customer_id", pgConn))
                            {
                                cmd.Parameters.AddWithValue("name", customerName);
                                cmd.Parameters.AddWithValue("phone", customerPhone);
                                custId = (int)cmd.ExecuteScalar();
                                customersCache[customerName] = custId;
                            }
                        }

                        int catId;
                        if (!categoriesCache.TryGetValue(productCategory, out catId))
                        {
                            using (var cmd = new NpgsqlCommand("INSERT INTO categories (category_name) VALUES (@category) RETURNING category_id", pgConn))
                            {
                                cmd.Parameters.AddWithValue("category", productCategory);
                                catId = (int)cmd.ExecuteScalar();
                                categoriesCache[productCategory] = catId;
                            }
                        }

                        int prodId;
                        if (!productsCache.TryGetValue(productName, out prodId))
                        {
                            using (var cmd = new NpgsqlCommand("INSERT INTO products (product_name, category_id, product_price) VALUES (@name, @catId, @price) RETURNING product_id", pgConn))
                            {
                                cmd.Parameters.AddWithValue("name", productName);
                                cmd.Parameters.AddWithValue("catId", catId);
                                cmd.Parameters.AddWithValue("price", productPrice);
                                prodId = (int)cmd.ExecuteScalar();
                                productsCache[productName] = prodId;
                            }
                        }

                        int newOrderId;
                        using (var cmd = new NpgsqlCommand("INSERT INTO orders (customer_id, order_date) VALUES (@custId, @orderDate) RETURNING order_id", pgConn))
                        {
                            cmd.Parameters.AddWithValue("custId", custId);
                            cmd.Parameters.AddWithValue("orderDate", orderDate);
                            newOrderId = (int)cmd.ExecuteScalar();
                        }

                        using (var cmd = new NpgsqlCommand("INSERT INTO order_details (order_id, product_id, quantity) VALUES (@orderId, @prodId, @quantity)", pgConn))
                        {
                            cmd.Parameters.AddWithValue("orderId", newOrderId);
                            cmd.Parameters.AddWithValue("prodId", prodId);
                            cmd.Parameters.AddWithValue("quantity", quantity);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    transaction.Commit();
                }
            }
            Console.WriteLine("Данные успешно перенесены из SQLite в PostgreSQL!");
        }
    }
}