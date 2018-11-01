using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;

namespace dapper_issue_1151
{
    class Program
    {
        static void Main(string[] args)
        {
            string _CONN_STRING = "Server=localhost;Uid=root;Password=pass;Database=test";
            using (var conn = new MySql.Data.MySqlClient.MySqlConnection(_CONN_STRING))
            {
                string sql = @"
                    SELECT O.OrderId, O.DateTime, D.Product
                    FROM Orders O
                    INNER JOIN Detail D ON D.OrderId = O.OrderId
                ";
                var result1 = conn.Query<Order, Detail, Order>(sql, (order, detail) => { order.Details.Add(detail.Product); return order; }, splitOn: "Product");
                Dump(result1);

                var orderById = new Dictionary<int, Order>();
                var result2 = conn.Query<Order, Detail, Order>(sql,
                    (order, detail) =>
                    {
                        if (!orderById.TryGetValue(order.OrderId, out var o))
                        {
                            orderById[order.OrderId] = (o = order);
                        }
                        o.Details.Add(detail.Product); return o;
                    }, splitOn: "Product")
                    .Distinct();
                Dump(result2);
            }
        }

        private static void Dump(object result1)
        {
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(result1, Newtonsoft.Json.Formatting.Indented));
        }
    }

    internal class Detail
    {
        public int OrderId { get; set; }
        public string Product { get; set; }
    }

    internal class Order
    {
        public int OrderId { get; set; }
        public DateTime DateTime { get; set; }
        public List<String> Details { get; set; } = new List<string>();
    }
}
