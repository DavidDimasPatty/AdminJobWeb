namespace AdminJobWeb.Tracelog
{
    public class TracelogApplicant
    {
        public void WriteLog(string text)
        {
            var folderTracelog = Path.Combine(Environment.CurrentDirectory, "TraceLog", "Tracelog Applicant");

            if (!Directory.Exists(folderTracelog))
            {
                Directory.CreateDirectory(folderTracelog);
            }

            var fileTracelog = Path.Combine(folderTracelog, DateTime.Now.ToString("yyyy-MM-dd") + "_ApplicantTracelog.txt");

            using (StreamWriter sw = new StreamWriter(fileTracelog, append: true))
            {
                sw.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " Log : " + text);
            }

        }
    }
}
