
using Newtonsoft.Json;
using System.Data.SqlClient;
using System.Text.RegularExpressions;

public class Report
{
    public string ReportName { get; set; }
    public string XmlTitle { get; set; }
    public List<SubReport> SubReports { get; set; }
}

public class SubReport
{
    public string SubReportName { get; set; }
    public string XmlTitle { get; set; }
}

public class Program
{
    static void Main(string[] args)
    {
        var rawData = new List<Tuple<string, string>>();
        var reports = new List<Report>();

        string connectionString = "Data Source=;Initial Catalog=;Integrated Security=True";

        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            connection.Open();

            using (SqlCommand command = new SqlCommand("SELECT * FROM Reports", connection))
            {
                SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    var data = new Tuple<string, string>(
                        reader.GetString(0),
                        reader.IsDBNull(1) ? null : reader.GetString(1)
                    );
                    rawData.Add(data);
                }
            }
        }

        var groupedData = rawData.GroupBy(x => x.Item1);
        foreach (var group in groupedData)
        {
            var report = new Report
            {
                ReportName = group.Key,
                XmlTitle = "",
                SubReports = group.Select(x => new SubReport { SubReportName = x.Item2, XmlTitle = "" }).ToList()
            };
            reports.Add(report);
        }

        // Write the data to the JSON file
        string json = JsonConvert.SerializeObject(reports, Formatting.Indented);
        string jsonFilePath = "ReportPairData.json";
        File.WriteAllText(jsonFilePath, json);

        // Load .rdl files and update XML titles in the JSON file
        string folderPath = "My Folder Path";
        string[] files = Directory.GetFiles(folderPath, "*.rdl", SearchOption.AllDirectories);

        foreach (var file in files)
        {
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file);
            string content = File.ReadAllText(file);

            var match = Regex.Match(content, @"http://tempuri\.org/(.+)&lt;");
            if (match.Success)
            {
                string xmlTitle = match.Groups[1].Value;

                foreach (var report in reports)
                {
                    if (report.ReportName == fileNameWithoutExtension)
                    {
                        report.XmlTitle = xmlTitle;
                    }

                    foreach (var subReport in report.SubReports)
                    {
                        if (subReport.SubReportName == fileNameWithoutExtension)
                        {
                            subReport.XmlTitle = xmlTitle;
                        }
                    }
                }
            }
        }

        // Write the updated data back to the JSON file
        string updatedJson = JsonConvert.SerializeObject(reports, Formatting.Indented);
        File.WriteAllText(jsonFilePath, updatedJson);
    }
}
