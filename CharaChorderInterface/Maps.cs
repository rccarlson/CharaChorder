using CsvHelper;
using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CharaChorderInterface;

public static class Maps
{
  //static Maps()
  //{
  //  //  var config = new CsvConfiguration(CultureInfo.InvariantCulture)
  //  //  {
  //  //    MissingFieldFound = null,
  //  //    //BadDataFound = null,
  //  //  };
  //  //  var assembly = Assembly.GetAssembly(typeof(Maps));
  //  //using var stream = assembly?.GetManifestResourceStream("CharaChorder.Data.ActionMap.csv");
  //  //  using var reader = new StreamReader(stream);
  //  //  using var csv = new CsvReader(reader, config);
  //  //  //csv.Configuration.MissingFieldFound = true;
  //  //  using var csvData = new CsvDataReader(csv);
  //  //  var dt = new DataTable();
  //  //  dt.Load(csvData);
  //  var lines = ReadLinesFromResource("CharaChorder.Data.ActionMap.txt");
  //}

  private static string[] ReadLinesFromResource(string resourceName)
  {
    var assembly = Assembly.GetAssembly(typeof(Maps));
		using var stream = assembly?.GetManifestResourceStream(resourceName);
    if (stream is null) return Array.Empty<string>();
    using var reader = new StreamReader(stream);
    return reader.ReadToEnd().Split(Environment.NewLine);
	}

  public static readonly string[] ActionMap = ReadLinesFromResource("CharaChorder.Data.ActionMap.txt");
}
