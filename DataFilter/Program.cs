using System;
using System.Collections.Generic;
using System.Data;
using Excel = Microsoft.Office.Interop.Excel;
using System.Linq;
using GenericParsing;
using System.Text;
using System.IO;

namespace DataFilter
{
    class Program
    {
        private static string path = @"C:\temp\pump-data.csv";
        private static string textPath = @"C:\temp\result.txt";
        private static string csvPath = @"C:\temp\result.csv";
        private static Dictionary<string, double> maxDic = new Dictionary<string, double>();
        private static Dictionary<string, double> minDic = new Dictionary<string, double>();
        private static double percentage = 4;
        static void Main(string[] args)
        {
            DataTable dt = ConvertToDataTable(path);
            setMinAndMax(dt);
            var possibilities = GetPosibilities(11);
            Dictionary<string, double> resultDic = GetResult(dt, possibilities);
            WriteOutTheFile(resultDic);

            DataTable resultTable = GenerateResultDataTable(resultDic, dt.Columns);
            DatatableToCsv(resultTable);
        }

        private static DataTable GenerateResultDataTable(Dictionary<string, double> resultDic, DataColumnCollection datacolumnCollection)
        {
            DataTable dataTable = new DataTable();

            datacolumnCollection[datacolumnCollection.Count - 1].ColumnName = "Posibilities";
            datacolumnCollection.Add("Performance");

            foreach(DataColumn column in datacolumnCollection)
            {
                dataTable.Columns.Add(column.ColumnName);
            }

            foreach (string posibility in resultDic.Keys)
            {
                DataRow row = dataTable.NewRow();

                char[] tokens = posibility.ToCharArray();
                for (int i = 0; i < tokens.Length; i++)
                {
                    var colName = datacolumnCollection[i].ColumnName;
                    row[colName] = tokens[i] == 'L' ? minDic[colName] : maxDic[colName];
                }

                row[datacolumnCollection.Count - 2] = posibility;
                row[datacolumnCollection.Count - 1] = resultDic[posibility];

                dataTable.Rows.Add(row);
            }

            return dataTable;
        }

        private static void WriteOutTheFile(Dictionary<string, double> dic)
        {
            using (System.IO.StreamWriter file =
            new System.IO.StreamWriter(textPath, false))
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

        private static Dictionary<string, double> GetResult(DataTable dt, List<string> possibilities)
        {
            Dictionary<string, double> resultDic = new Dictionary<string, double>();
            Dictionary<string, int> ColumnLowHigh = new Dictionary<string, int>();

            for (int r = 0; r < dt.Rows.Count; r++)
            {
                double performance = Convert.ToDouble(dt.Rows[r][dt.Columns.Count - 1]);
                ColumnLowHigh.Clear();

                for (int c = 0; c < dt.Columns.Count - 1; c++)
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
                        if (resultDic.ContainsKey(possibility))
                        {
                            var occaranceOfLow = ((double)possibility.Count(x => x == 'L')) / possibility.Length;

                            if (occaranceOfLow > 0.5)
                            {
                                if (performance > resultDic[possibility])
                                {
                                    resultDic[possibility] = performance;
                                }

                            }
                            else if (occaranceOfLow < 0.5)
                            {
                                if (performance < resultDic[possibility])
                                {
                                    resultDic[possibility] = performance;
                                }
                            }
                            else
                            {
                                if (performance != resultDic[possibility])
                                {
                                    resultDic[possibility] = (performance + resultDic[possibility]) / 2;
                                }
                            }
                        }
                        else
                        {
                            resultDic.Add(possibility, percentage);
                        }
                    }
                }
            }

            foreach (var posibility in possibilities)
            {
                if (!resultDic.ContainsKey(posibility))
                {
                    resultDic[posibility] = 0;
                }
            }

            return resultDic;
        }

        private static List<string> GetPosibilities(int size)
        {
            var list = new List<string>();
            var alphabet = "LH";
            var q = alphabet.Select(x => x.ToString());
            for (int i = 0; i < size - 1; i++)
            {
                q = q.SelectMany(x => alphabet, (x, y) => x + y);
            }

            foreach (var item in q)
            {
                list.Add(item);
            }

            return list;
        }

        private static void DatatableToCsv(DataTable dt)
        {
            StringBuilder sb = new StringBuilder();

            IEnumerable<string> columnNames = dt.Columns.Cast<DataColumn>().
                                              Select(column => column.ColumnName);
            sb.AppendLine(string.Join(",", columnNames));

            foreach (DataRow row in dt.Rows)
            {
                IEnumerable<string> fields = row.ItemArray.Select(field => field.ToString());
                sb.AppendLine(string.Join(",", fields));
            }

            File.WriteAllText(csvPath, sb.ToString());
        }
    }
}