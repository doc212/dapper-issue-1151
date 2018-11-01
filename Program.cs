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
                /*
                db content:
                table Orders:
                +---------+---------------------+
                | OrderId | DateTime            |
                +---------+---------------------+
                |       1 | 2018-11-01 02:08:44 |
                |       2 | 2018-11-01 02:08:47 |
                +---------+---------------------+
                table Detail
                +----+---------+------------+
                | Id | OrderId | Product    |
                +----+---------+------------+
                |  1 |       1 | detail 1.1 |
                |  2 |       1 | detall 1.2 |
                |  3 |       2 | detail 2   |
                +----+---------+------------+

                 */
                string sql = @"
                    SELECT O.OrderId, O.DateTime, D.Product
                    FROM Orders O
                    INNER JOIN Detail D ON D.OrderId = O.OrderId
                ";
                /*
                returns this:
                +---------+---------------------+------------+
                | OrderId | DateTime            | Product    |
                +---------+---------------------+------------+
                |       1 | 2018-11-01 02:08:44 | detail 1.1 |
                |       1 | 2018-11-01 02:08:44 | detall 1.2 |
                |       2 | 2018-11-01 02:08:47 | detail 2   |
                +---------+---------------------+------------+
                 */

                 //first "simple" way
                var result1 = conn.Query<Order, Detail, Order>(sql, (order, detail) => { order.Details.Add(detail.Product); return order; }, splitOn: "Product");
                Dump(result1);
                /*
                [
                    {
                        "OrderId": 1,
                        "DateTime": "2018-11-01T02:08:44Z",
                        "Details": [
                        "detail 1.1"
                        ]
                    },
                    {
                        "OrderId": 1,
                        "DateTime": "2018-11-01T02:08:44Z",
                        "Details": [
                        "detall 1.2"
                        ]
                    },
                    {
                        "OrderId": 2,
                        "DateTime": "2018-11-01T02:08:47Z",
                        "Details": [
                        "detail 2"
                        ]
                    }
                ]
                 */

                //2nd way: requires a dict to track orders by id
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
                /*
                [
                    {
                        "OrderId": 1,
                        "DateTime": "2018-11-01T02:08:44Z",
                        "Details": [
                        "detail 1.1",
                        "detall 1.2"
                        ]
                    },
                    {
                        "OrderId": 2,
                        "DateTime": "2018-11-01T02:08:47Z",
                        "Details": [
                        "detail 2"
                        ]
                    }
                ]
                 */
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
