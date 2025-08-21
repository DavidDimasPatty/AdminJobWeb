namespace AdminJobWeb.Tracelog
{
    public class TracelogUser
    {
        public void WriteLog(string text)
        {
            var folderTracelog = Path.Combine(Environment.CurrentDirectory, "TraceLog", "Tracelog User");
            Directory.CreateDirectory(folderTracelog);

            var fileTracelog = Path.Combine(folderTracelog, DateTime.Now.ToString("yyyy-MM-dd") + "_UserTracelog.txt");

            using (StreamWriter sw = new StreamWriter(fileTracelog, append: true))
            {
                sw.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " Log : " + text);
            }
        }
    }
}
