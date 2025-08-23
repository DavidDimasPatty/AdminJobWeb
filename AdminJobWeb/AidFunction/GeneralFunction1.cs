using System.Security.Cryptography;

namespace AdminJobWeb.AidFunction
{
    public class GeneralFunction1
    {
        public int size;
        public GeneralFunction1(int size=16)
        {
            this.size = size;
        }

        public string GenerateRandomKey()
        {

            var randomNumber = new byte[size];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
            }

            string token = Convert.ToBase64String(randomNumber)
                             .Replace("+", "-")
                             .Replace("/", "_")
                             .Replace("=", "");

            return token;
        }
    }
}
