using System;
using System.Collections.Generic;

namespace devises
{
    class CurrencyExchange
    {
        public string from_currency;
        public string to_currency;
        public double rate;

        public CurrencyExchange(string from_currency, string to_currency, double rate)
        {
            this.from_currency = from_currency;
            this.to_currency = to_currency;
            this.rate = rate;
        }

        public bool isExchange(string currency_a, string currency_b)
        {
            return this.from_currency == currency_a && this.to_currency == currency_b || this.from_currency == currency_b && this.to_currency == currency_a;
        }

        public double getExchangeRate(string from_currency, string to_currency)
        {
            if (this.from_currency == from_currency && this.to_currency == to_currency)
                return this.rate;
            else if (this.from_currency == to_currency && this.to_currency == from_currency)
                return Math.Round(1 / this.rate, 4);
            return -1;
        }

    }

    class CurrencyConversion
    {

        public string from_currency;
        public int amount;
        public string to_currency;
        public List<string> currencies;
        public CurrencyExchange[] exchanges;

        public CurrencyConversion(string from_currency, string to_currency, int amount, List<string> currencies, CurrencyExchange[] exchanges)
        {
            this.from_currency = from_currency;
            this.to_currency = to_currency;
            this.amount = amount;
            this.currencies = currencies;
            this.exchanges = exchanges;
        }

        public CurrencyConversion()
        {
            this.from_currency = string.Empty;
        }

        public bool isEmpty()
        {
            return this.from_currency == string.Empty;
        }

        bool isRateBetween(string from_curr, string to_curr)
        {
            return Array.Exists(this.exchanges, exchange => exchange.isExchange(from_curr, to_curr));
        }

        double getExchangeRate(string from_curr, string to_curr)
        {
            foreach (CurrencyExchange exchange in this.exchanges)
            {
                double rate = exchange.getExchangeRate(from_curr, to_curr);
                if ((int)rate != -1)
                    return rate;
            }
            return -1;
        }

        int getMinDistance(int[] distances, bool[] is_currency_set)
        {
            int min = int.MaxValue;
            int min_index = -1;
            for (int v = 0; v < distances.Length; v++)
            {
                if (is_currency_set[v] == false && distances[v] <= min)
                {
                    min = distances[v];
                    min_index = v;
                }

            }
            return min_index;
        }

        int[] DijkstraSearchShortestPath()
        {
            int nb_currencies = this.currencies.Count;
            int[] distances = new int[nb_currencies]; // minimal conversion number from start currency to each currency
            bool[] is_currency_set = new bool[nb_currencies]; // used to keep track of processed currencies
            int[] predecessor_index = new int[nb_currencies]; // indicates previous currency index to follow shortest path to go back to start currency

            // initialize Dijkstra algorithm with start currency
            // set all distances to maximum value unless the one from starting point
            for (int i = 0; i < nb_currencies; i++)
            {
                if (this.currencies[i] == this.from_currency)
                    distances[i] = 0;
                else
                    distances[i] = int.MaxValue;
                is_currency_set[i] = false;
                predecessor_index[i] = -1;
            }

            // find shortest conversion path
            // iterate through every currency (except the initialized one)
            for (int i = 0; i < nb_currencies - 1; i++)
            {
                // get currency index with the shortest path from start currency
                int u = this.getMinDistance(distances, is_currency_set);
                if (u == -1) { break; }
                // set this currency has processed
                is_currency_set[u] = true;

                // update distances for each currency
                for (int v = 0; v < nb_currencies; v++)
                {
                    // update distance if node has not been processed yet
                    // if it exists a conversion rate with the processing currency 
                    // if distance is shortest than previous one set
                    if (!is_currency_set[v] && this.isRateBetween(this.currencies[u], this.currencies[v]) && distances[u] != int.MaxValue && distances[u] + 1 < distances[v])
                    {
                        distances[v] = distances[u] + 1; // we add 1 conversion to the distance
                        predecessor_index[v] = u; // we set currency predecessor
                    }
                }
            }

            return predecessor_index;
        }

        double[] GetConversionsRates(int[] predecessor_index)
        {
            // to use if we want to get currency path
            // List<string> path = new List<string> { this.to_currency }; 

            List<double> conversion = new List<double>();
            // we start by the end to go back to the start currency
            string node = this.to_currency;
            int i = 1;
            while (node != this.from_currency && i < this.currencies.Count)
            {
                i++;
                int currency_idx = this.currencies.IndexOf(node, 0);
                int prev_currency_idx = predecessor_index[currency_idx];
                if (prev_currency_idx == -1)
                {
                    conversion = new List<double>();
                    break;
                }
                string prev_currency = this.currencies[prev_currency_idx];

                double conv = this.getExchangeRate(prev_currency, node);
                conversion.Insert(0, conv);
                // to use if we want to get currency path
                // path.Insert(0, prev_currency); 

                node = prev_currency;
            }
            // Console.WriteLine("Global Path [{0}]", string.Join(", ", path));
            // Console.WriteLine("Global Conv [{0}]", string.Join(", ", conversion));

            return conversion.ToArray();
        }

        int ComputeAmount(double[] conversion)
        {
            double result = this.amount;
            foreach (double conv in conversion)
            {
                result = Math.Round(result * conv, 4);
            }
            return (int)Math.Round(result, 0);
        }

        int getConversionAmount()
        {
            int[] predecessor_index = this.DijkstraSearchShortestPath();
            double[] conversion = this.GetConversionsRates(predecessor_index);
            if (conversion.Length == 0 && this.from_currency != this.to_currency)
            {
                Console.WriteLine("Cannot compute this conversion. Some rates are missing.");
                return -1;
            }
            return this.ComputeAmount(conversion);
        }


        static CurrencyConversion readInputFile(string path)
        {
            System.IO.StreamReader file = new System.IO.StreamReader(path);
            CurrencyConversion currency_conversion = new CurrencyConversion();
            try
            {
                // read first line to get D1, M and D2
                string first_line = file.ReadLine();
                string[] first_line_data_list = first_line.Split(';');
                if (first_line_data_list.Length != 3) { throw new FormatException("Wrong file format: First line should have following format D1;M;D2"); }
                string from_currency = first_line_data_list[0];
                string to_currency = first_line_data_list[2];
                int amount = int.Parse(first_line_data_list[1]);

                // read second line to number of exchanges
                string second_line = file.ReadLine();
                int nb_exchanges = int.Parse(second_line);
                CurrencyExchange[] exchanges = new CurrencyExchange[nb_exchanges];
                List<string> currencies = new List<string>();

                // read next lines to get all exchange rates
                for (int i = 0; i < nb_exchanges; i++)
                {
                    string line = file.ReadLine();
                    string[] line_data_list = line.Split(';');
                    if (line_data_list.Length != 3) { throw new FormatException("Wrong file format: Currency exchange should have following format DD;DA;T"); }
                    string from_curr = line_data_list[0];
                    string to_curr = line_data_list[1];
                    double rate = double.Parse(line_data_list[2], System.Globalization.CultureInfo.InvariantCulture);
                    exchanges[i] = new CurrencyExchange(from_curr, to_curr, rate);
                    if (!currencies.Contains(from_curr))
                        currencies.Add(from_curr);
                    if (!currencies.Contains(to_curr))
                        currencies.Add(to_curr);
                }
                currency_conversion = new CurrencyConversion(from_currency, to_currency, amount, currencies, exchanges);

            }
            catch (Exception e)
            {
                Console.WriteLine("File reading issue: {0}", e.ToString());
            }
            finally
            {
                file.Close();
            }
            return currency_conversion;
        }

        public static void Main(string[] args)
        {
            string path = "/home/melyss/perso/lucca_back/currency_data"; // TODO
            if (args.Length > 0)
            {
                path = args[0];
            }
            CurrencyConversion conv = readInputFile(path);
            if (!conv.isEmpty())
            {
                int result = conv.getConversionAmount();
                Console.WriteLine(result);
            }
        }
    }
}
