namespace AdminJobWeb.Tracelog
{
    public class TracelogAccount
    {
        public void WriteLog(string text)
        {
            var folderTracelog = Path.Combine(Environment.CurrentDirectory,"TraceLog");

            if (!Directory.Exists(folderTracelog))
            {
                Directory.CreateDirectory(folderTracelog);
            }

            var fileTracelog = Path.Combine(folderTracelog, DateTime.Now.ToString("yyyy-MM-dd") + "_AccountTracelog.txt");

            using (StreamWriter sw = new StreamWriter(fileTracelog, append: true))
            {
                sw.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " Log : " + text);
            }

        }
    }
}
