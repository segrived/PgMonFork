using CommandLine;

namespace PgMonFork
{
    public class CmdOptions
    {
        [Option('i', Required = false, HelpText = "Refresh interval", Default = 2)]
        public int Interval { get; set; }

        [Option('t', Required = false, HelpText = "Install task", Default = false)]
        public bool InstallTask { get; set; }
    }
}
