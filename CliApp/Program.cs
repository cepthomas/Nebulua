namespace Nebulua.CliApp
{
    internal class Program
    {
        static void Main(string[] _)
        {
            using var app = new App(); // guarantees Dispose()
            app.Run();
        }
    }
}
