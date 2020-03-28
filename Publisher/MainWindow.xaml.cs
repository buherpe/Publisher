using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Xml;
using NLog;
using Tools;

namespace Publisher
{
    public partial class MainWindow
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public Q W { get; set; } = new Q();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            // \nuget pack GDTGen.nuspec -Version %(myAssemblyInfo.Version) -Properties Configuration=Release -OutputDirectory $(OutDir) -BasePath $(OutDir)
            // \squirrel --releasify $(OutDir)GDTGen.$([System.Version]::Parse(%(myAssemblyInfo.Version)).ToString(3)).nupkg --no-msi --no-delta

            //var pathToExe = @"C:\Users\buh\source\repos\buherpe\GameDevTycoon\GameDevTycoon\bin\Debug\GDTGen.exe";
            //var versionInfo = FileVersionInfo.GetVersionInfo(pathToExe);
            ////MessageBox.Show($"{versionInfo}");

            //W.Projects.Add(new Project
            //{
            //    Name = versionInfo.ProductName,
            //    CurrentVersion = versionInfo.FileVersion,
            //    PathToExe = pathToExe
            //});

        }
    }

    public class Q : ObservableObject
    {
        private string _nugetPath = @"C:\Users\buh\.nuget\packages\nuget.commandline\5.3.1\tools\nuget";

        public string NugetPath
        {
            get => _nugetPath;
            set => OnPropertyChanged(ref _nugetPath, value);
        }

        private string _squirrelPath = @"C:\Users\buh\.nuget\packages\squirrel.windows\1.9.1\tools\squirrel";

        public string SquirrelPath
        {
            get => _squirrelPath;
            set => OnPropertyChanged(ref _squirrelPath, value);
        }

        private TrulyObservableCollection<Project> _projects = new TrulyObservableCollection<Project>();

        public TrulyObservableCollection<Project> Projects
        {
            get => _projects;
            set => OnPropertyChanged(ref _projects, value);
        }
    }

    public class Project : ObservableObject
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private string _name;

        public string Name
        {
            get => _name;
            set => OnPropertyChanged(ref _name, value);
        }

        private string _version;

        public string Version
        {
            get => _version;
            set => OnPropertyChanged(ref _version, value);
        }

        private string _csprojPath;

        public string CsprojPath
        {
            get => _csprojPath;
            set => OnPropertyChanged(ref _csprojPath, value);
        }

        private bool _noMsi = true;

        public bool NoMsi
        {
            get => _noMsi;
            set => OnPropertyChanged(ref _noMsi, value);
        }

        private bool _noDelta = true;

        public bool NoDelta
        {
            get => _noDelta;
            set => OnPropertyChanged(ref _noDelta, value);
        }

        private string _outputDirectory;

        public string OutputDirectory
        {
            get => _outputDirectory;
            set => OnPropertyChanged(ref _outputDirectory, value);
        }

        private bool _isDeleteDependenciesFromNuspecEnabled = true;

        public bool IsDeleteDependenciesFromNuspecEnabled
        {
            get => _isDeleteDependenciesFromNuspecEnabled;
            set => OnPropertyChanged(ref _isDeleteDependenciesFromNuspecEnabled, value);
        }

        private string _squirrelReleaseDir;

        public string SquirrelReleaseDir
        {
            get => _squirrelReleaseDir;
            set => OnPropertyChanged(ref _squirrelReleaseDir, value);
        }

        public delegate void OutputDataReceivedHandler(DataReceivedSource dataReceivedSource, DataReceivedType dataReceivedType, string data);

        public event OutputDataReceivedHandler OutputDataReceived;

        public void LoadName()
        {
            var doc = new XmlDocument();
            doc.Load(CsprojPath);
            var assemblyName = doc.GetElementsByTagName("AssemblyName").Cast<XmlNode>().Single().InnerText;
            //Console.WriteLine($"assemblyName: {assemblyName}");
            Name = assemblyName;
        }

        public void LoadVersion()
        {
            string assemblyInfoPath = Path.Combine(new FileInfo(CsprojPath).Directory.FullName, "Properties", "AssemblyInfo.cs");
            var assemblyInfo = File.ReadAllText(assemblyInfoPath, Encoding.UTF8);
            var rawVersion = Regex.Match(assemblyInfo, @"^\[assembly: AssemblyVersion\(""([0-9.]+)""\)\]", RegexOptions.Multiline).Groups.Cast<Group>().ElementAtOrDefault(1)?.Value;
            var version = System.Version.Parse(rawVersion).ToString(3);
            //Console.WriteLine($"version: {version}");
            Version = version;
        }

        public string GetNugetPackCmd()
        {
            // -Version {NewVersion}
            return $@"pack {CsprojPath} -Build -Properties Configuration=Release -OutputDirectory {OutputDirectory}";
        }

        public static ProcessStartInfo GetDefaultProcessStartInfo(string fileName, string arguments)
        {
            return new ProcessStartInfo(fileName, arguments)
            {
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
        }

        public void NugetPack(string nugetPath)
        {
            var process = new Process();
            process.StartInfo = GetDefaultProcessStartInfo(nugetPath, GetNugetPackCmd());
            process.EnableRaisingEvents = true;
            process.OutputDataReceived += (sender, args) => OutputDataReceived?.Invoke(DataReceivedSource.NugetPack, DataReceivedType.Output, args.Data);
            process.ErrorDataReceived += (sender, args) => OutputDataReceived?.Invoke(DataReceivedSource.NugetPack, DataReceivedType.Error, args.Data);

            Log.Info($"Process starting...");
            var start = process.Start();
            Log.Info($"start result: {start}");

            Log.Info($"BeginOutputReadLine...");
            process.BeginOutputReadLine();
            Log.Info($"BeginOutputReadLine");

            Log.Info($"WaitForExit...");
            process.WaitForExit();
            Log.Info($"WaitForExit. ExitCode: {process.ExitCode}");

            Log.Info($"CancelOutputRead...");
            process.CancelOutputRead();
            Log.Info($"CancelOutputRead");
        }

        public string GetNupkgPath()
        {
            return Path.Combine(OutputDirectory, $"{Name}.{Version}.nupkg");
        }

        public void DeleteDependenciesFromNuspec()
        {
            Log.Info($"Start");
            var tempPath = Path.GetTempPath();
            Log.Info($"tempPath: {tempPath}");

            var tempDir = Path.Combine(tempPath, "PublisherTemp");
            Log.Info($"tempDir: {tempDir}");

            var nuspecFile = $"{Name}.nuspec";
            Log.Info($"nuspecFile: {nuspecFile}");

            var nuspecTempFile = Path.Combine(tempDir, nuspecFile);
            Log.Info($"nuspecTempFile: {nuspecTempFile}");

            if (!Directory.Exists(tempDir))
            {
                Log.Info($"tempDir не существует, создаем папку");
                Directory.CreateDirectory(tempDir);
            }

            if (File.Exists(nuspecTempFile))
            {
                Log.Info($"nuspecTempFile существует, удаляем файл");
                File.Delete(nuspecTempFile);
            }

            using (var zip = ZipFile.Open(GetNupkgPath(), ZipArchiveMode.Update))
            {
                Log.Info($"Открыли архив");
                var nuspec = zip.GetEntry(nuspecFile);

                var rawNuspecXml = "";

                using (var stream = nuspec.Open())
                using (var reader = new StreamReader(stream))
                {
                    rawNuspecXml = reader.ReadToEnd();
                }

                Log.Info($"rawNuspecXml: {rawNuspecXml}");

                var nuspecXml = Regex.Replace(rawNuspecXml, "^((?!<!--).)*(<dependency .*)$", m => $"<!-- {m.Groups.Cast<Group>().ElementAtOrDefault(2).Value.Trim()} -->", RegexOptions.Multiline);
                Log.Info($"nuspecXml: {nuspecXml}");

                var sameNuspecXml = rawNuspecXml == nuspecXml;
                Log.Info($"sameNuspecXml: {sameNuspecXml}");

                if (!sameNuspecXml)
                {
                    Log.Info($"Сохраняем темповый файл...");
                    File.WriteAllText(nuspecTempFile, nuspecXml, Encoding.UTF8);

                    Log.Info($"Удаляем *.nuspec из архива");
                    nuspec.Delete();

                    Log.Info($"Добавляем в архив темповый nuspec-файл");
                    zip.CreateEntryFromFile(nuspecTempFile, Path.GetFileName(nuspecTempFile));
                }

                Log.Info($"Закрываем архив");
            }

            if (File.Exists(nuspecTempFile))
            {
                Log.Info($"nuspecTempFile существует, удаляем файл");
                File.Delete(nuspecTempFile);
            }
        }

        public string GetSquirrelCmd()
        {
            var noMsi = NoMsi ? " --no-msi" : "";
            var noDelta = NoDelta ? " --no-delta" : "";
            return $@"--releasify {GetNupkgPath()}{noMsi}{noDelta} --releaseDir={SquirrelReleaseDir}";
        }

        public void SquirrelReleasify(string squirrelPath)
        {
            var process = new Process();
            process.StartInfo = GetDefaultProcessStartInfo(squirrelPath, GetSquirrelCmd());
            process.EnableRaisingEvents = true;
            process.OutputDataReceived += (sender, args) => OutputDataReceived?.Invoke(DataReceivedSource.Squirrel, DataReceivedType.Output, args.Data);
            process.ErrorDataReceived += (sender, args) => OutputDataReceived?.Invoke(DataReceivedSource.Squirrel, DataReceivedType.Error, args.Data);

            Log.Info($"Process starting...");
            var start = process.Start();
            Log.Info($"start result: {start}");

            Log.Info($"BeginOutputReadLine...");
            process.BeginOutputReadLine();
            Log.Info($"BeginOutputReadLine");

            Log.Info($"BeginErrorReadLine...");
            process.BeginErrorReadLine();
            Log.Info($"BeginErrorReadLine");

            Log.Info($"WaitForExit...");
            process.WaitForExit();
            Log.Info($"WaitForExit. ExitCode: {process.ExitCode}");

            Log.Info($"CancelOutputRead...");
            process.CancelOutputRead();
            Log.Info($"CancelOutputRead");
        }

        public void Upload()
        {

        }

        public void Publish(string nugetPath, string squirrelPath)
        {
            NugetPack(nugetPath);

            if (IsDeleteDependenciesFromNuspecEnabled)
            {
                DeleteDependenciesFromNuspec();
            }

            SquirrelReleasify(squirrelPath);

            // todo upload...
        }
    }

    public enum DataReceivedType
    {
        Output,
        Error
    }

    public enum DataReceivedSource
    {
        NugetPack,
        Squirrel
    }
}