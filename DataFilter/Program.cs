using System;
using System.Collections.Generic;
using System.Data;
using Excel = Microsoft.Office.Interop.Excel;
using System.Linq;
using GenericParsing;

namespace DataFilter
{
    class Program
    {
        private static string path = @"C:\temp\pump-data.csv";
        private static Dictionary<string, double> maxDic = new Dictionary<string, double>();
        private static Dictionary<string, double> minDic = new Dictionary<string, double>();
        private static double percentage = 0.9;
        static void Main(string[] args)
        {
            DataTable dt = ConvertToDataTable(path);
            setMinAndMax(dt);
            Dictionary<string, double> resultDic = GetResult(dt);
            WriteOutTheFile(resultDic);
        }

        private static void WriteOutTheFile(Dictionary<string, double> dic)
        {
            using (System.IO.StreamWriter file =
            new System.IO.StreamWriter(@"C:\temp\Result.txt", false))
            {
                file.WriteLine("-----Max Values -----");
                foreach (string key in maxDic.Keys)
                {
                    file.WriteLine(key + "--->" + maxDic[key]);
                }
                file.WriteLine("-----Min Values -----");
                foreach (string key in minDic.Keys)
                {
                    file.WriteLine(key + "--->" + minDic[key]);
                }
                file.WriteLine("-----LOW/HIGH Values -----");
                foreach (string key in dic.Keys)
                {
                    file.WriteLine(key + "--->" + dic[key]);
                }
            }
        }

        public static System.Data.DataTable ConvertToDataTable(string path)
        {
            DataTable dt;
            using (GenericParserAdapter parser = new GenericParserAdapter())
            {
                parser.SetDataSource(path);
                parser.ColumnDelimiter = ',';
                parser.FirstRowHasHeader = true;
                parser.MaxBufferSize = 4096;
                dt = parser.GetDataTable();
            }

            return dt;
        }

        private static void setMinAndMax(DataTable dt)
        {
            for (int i = 0; i < dt.Columns.Count - 1; i++)
            {
                string colName = dt.Columns[i].ColumnName;
                var list = dt.AsEnumerable().Select(r => r.Field<string>(colName)).ToList().Select(double.Parse).ToList();
                minDic.Add(colName, list.Min());
                maxDic.Add(colName, list.Max());
            }
        }

        private static Dictionary<string, double> GetResult(DataTable dt)
        {
            Dictionary<string, double> resultDic = new Dictionary<string, double>();
            Dictionary<string, double> temp = new Dictionary<string, double>();
            Dictionary<string, int> ColumnLowHigh = new Dictionary<string, int>();

            string[] possibilities = { "LLLL", "HLHH", "LLHL", "HHHH", "HLLL", "LHLH", "LLHH", "LHHH", "HHLH", "HLLH", "HHHL", "HHLL", "LLLH", "LHLL", "HLHL", "LHHL" };

            for (int r = 0; r < dt.Rows.Count; r++)
            {
                double performance = Convert.ToDouble(dt.Rows[r][dt.Columns.Count - 5]);

                for (int c = 0; c <= dt.Columns.Count - 2; c++)
                {
                    string colName = dt.Columns[c].ColumnName;
                    double value = Convert.ToDouble(dt.Rows[r][c]);

                    var rangeDiff = (maxDic[colName] - (minDic[colName]) / percentage);
                    ColumnLowHigh.Add("L" + c, value <= minDic[colName] + (rangeDiff / percentage) ? 1 : 0);
                    ColumnLowHigh.Add("H" + c, value >= maxDic[colName] - (rangeDiff / percentage) ? 1 : 0);
                }

                foreach (string possibility in possibilities)
                {
                    char[] token = possibility.ToCharArray();

                    int ShouldConsider = 1;
                    for (var i = 0; i < token.Length; i++)
                    {
                        ShouldConsider = ShouldConsider * (token[i] == 'L' ? ColumnLowHigh["L" + i] : ColumnLowHigh["H" + i]);
                    }

                    if (ShouldConsider == 1)
                    {
                        temp.Add(possibility + "-" + r, performance);
                    }
                }

                ColumnLowHigh.Clear();
            }

            foreach (string possibility in possibilities)
            {
                char[] token = possibility.ToCharArray();
                var Ls = token.Count(x => x == 'L');
                double max = 0;
                var list = temp.Where(p => p.Key.Contains(possibility)).Select(x => x.Value).DefaultIfEmpty(0);
                switch (Ls)
                {
                    case 0:
                        max = list.Max();
                        break;
                    case 1:
                        max = list.Max();
                        break;
                    case 2:
                        max = list.Average();
                        break;
                    case 3:
                        max = list.Min();
                        break;
                    case 4:
                        max = list.Min();
                        break;
                }

                resultDic.Add(possibility, max);
            }

            return resultDic;
        }
    }
}