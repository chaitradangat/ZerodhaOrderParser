using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using System.IO;
using System.Data;


namespace ZerodhaOrderParser.RunUI
{
    class Program
    {
        static void Main(string[] args)
        {
            OrderParser orderParser = new OrderParser();

            List<OrderDataSimplified> results = new List<OrderDataSimplified>();

            string filepath = Console.ReadLine();

            var orderDatas = orderParser.ParseOrders(filepath);

            foreach (var orderData in orderDatas)
            {
                var consolidatedOrders = orderParser.ConsolidateOrders(orderData.Value);

                results.AddRange(consolidatedOrders);
            }


            foreach (var result in results)
            {
                File.WriteAllLines("output.txt", new string[] { result.EquityName + "\t" + result.Quantity + "\t" + result.BuyPrice + "\t" + result.SellPrice + "\t" });

                Console.WriteLine(result.EquityName + "\t" + result.Quantity + "\t" + result.BuyPrice + "\t" + result.SellPrice + "\t");
            }


            Console.ReadLine();
        }
    }


    public class OrderParser
    {
        public OrderParser()
        {

        }

        public Dictionary<string, List<OrderData>> ParseOrders(string orderFilePath)
        {
            try
            {
                List<OrderData> parsedData = new List<OrderData>();


                if (!File.Exists(orderFilePath))
                {
                    throw new Exception();
                }


                var fileData = new List<string>(File.ReadAllLines(orderFilePath));
                fileData.RemoveAt(0);


                parsedData =

                 (
                 from fdata in fileData
                 let fdatas = fdata.Split(',')
                 select new OrderData
                 {
                     OrderTime = DateTime.Parse(fdatas[0]),
                     OrderType = fdatas[1] == "BUY" ? OrderTyp.BUY : OrderTyp.SELL,
                     EquityName = fdatas[2],
                     OrderCategory = fdatas[3],
                     Quantity = int.Parse(fdatas[4]),
                     OrderPrice = float.Parse(fdatas[5]),
                     OrderStatus = fdatas[6]
                 }
                 ).ToList();


                parsedData.RemoveAll(x => x.OrderStatus != "COMPLETE");

                parsedData.RemoveAll(x => x.OrderCategory != "MIS");

                var segrregatedData =
                //(Dictionary<string, List<OrderData>>)
                (
                from pdata in parsedData

                group pdata by pdata.EquityName into g

                select new { Value = g.Key, EquityData = g.ToList() }
                ).ToDictionary(x => x.Value, x => x.EquityData);

                return segrregatedData;

            }
            catch (Exception)
            {
                return null;
            }
        }

        //Matching logic required lotsa perspiration 
        public List<OrderDataSimplified> ConsolidateOrders(List<OrderData> orderDatas)
        {
            var buyOrders = orderDatas.Where(x => x.OrderType == OrderTyp.BUY).OrderBy(x => x.OrderTime).ToList();

            var sellOrders = orderDatas.Where(x => x.OrderType == OrderTyp.SELL).OrderBy(x => x.OrderTime).ToList();

            var consolidatedResults = new List<OrderDataSimplified>();


            if (sellOrders.Count >= buyOrders.Count)
            {
                foreach (var buyOrder in buyOrders)
                {
                    int quantity = buyOrder.Quantity;

                    sellOrders.RemoveAll(x => x == null);

                    for (int i = 0; i < sellOrders.Count && quantity != 0; i++)
                    {
                        var result = new OrderDataSimplified();

                        result.EquityName = buyOrder.EquityName;

                        result.BuyPrice = buyOrder.OrderPrice;

                        result.SellPrice = sellOrders[i].OrderPrice;

                        result.Quantity = sellOrders[i].Quantity;

                        quantity -= sellOrders[i].Quantity;

                        consolidatedResults.Add(result);

                        sellOrders[i] = null;
                    }
                }
            }
            else
            {
                foreach (var sellOrder in sellOrders)
                {
                    int quantity = sellOrder.Quantity;

                    buyOrders.RemoveAll(x => x == null);

                    for (int i = 0; i < buyOrders.Count && quantity != 0; i++)
                    {
                        var result = new OrderDataSimplified();

                        result.EquityName = sellOrder.EquityName;

                        result.SellPrice = sellOrder.OrderPrice;

                        result.BuyPrice = buyOrders[i].OrderPrice;

                        result.Quantity = buyOrders[i].Quantity;

                        quantity -= buyOrders[i].Quantity;

                        consolidatedResults.Add(result);

                        buyOrders[i] = null;
                    }
                }
            }

            return consolidatedResults;
        }
    }

    public class OrderDataSimplified
    {
        public string EquityName { get; set; }

        public float BuyPrice { get; set; }

        public float SellPrice { get; set; }

        public int Quantity { get; set; }
    }


    public class OrderData
    {
        public DateTime OrderTime { get; set; }

        public string EquityName { get; set; }

        public int Quantity { get; set; }

        public float OrderPrice { get; set; }

        public OrderTyp OrderType { get; set; }

        public string OrderCategory { get; set; }

        public string OrderStatus { get; set; }
    }

    public enum OrderTyp
    {
        SELL,
        BUY
    }

}
