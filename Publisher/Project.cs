using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using Newtonsoft.Json;
using NLog;
using Tools;

namespace Publisher
{
    public class Project : ObservableObject
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private string _name;

        [JsonIgnore]
        public string Name
        {
            get => _name;
            set => OnPropertyChanged(ref _name, value);
        }

        private string _version;

        [JsonIgnore]
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

        //private bool _noDelta = true;

        //public bool NoDelta
        //{
        //    get => _noDelta;
        //    set => OnPropertyChanged(ref _noDelta, value);
        //}

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

        private string _uploadPath;

        public string UploadPath
        {
            get => _uploadPath;
            set => OnPropertyChanged(ref _uploadPath, value);
        }

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

        private string _output;

        [JsonIgnore]
        public string Output
        {
            get => _output;
            set => OnPropertyChanged(ref _output, value);
        }

        //public delegate void OutputDataReceivedHandler(DataReceivedSource dataReceivedSource, DataReceivedType dataReceivedType, string data);

        //public event OutputDataReceivedHandler OutputDataReceived;

        public void LoadName()
        {
            if (!File.Exists(CsprojPath)) return;

            var doc = new XmlDocument();
            doc.Load(CsprojPath);
            var assemblyName = doc.GetElementsByTagName("AssemblyName").Cast<XmlNode>().Single().InnerText;
            
            Name = assemblyName;
        }

        public void LoadVersion()
        {
            if (!File.Exists(CsprojPath)) return;
            
            string assemblyInfoPath = Path.Combine(new FileInfo(CsprojPath).Directory.FullName, "Properties", "AssemblyInfo.cs");
            var assemblyInfo = File.ReadAllText(assemblyInfoPath, Encoding.UTF8);
            var rawVersion = Regex.Match(assemblyInfo, @"^\[assembly: AssemblyVersion\(""([0-9.]+)""\)\]", RegexOptions.Multiline).Groups.Cast<Group>().ElementAtOrDefault(1)?.Value;
            var version = System.Version.Parse(rawVersion).ToString(3);
            
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

        //[JsonIgnore]
        //public Process NugetPackProcess { get; set; }

        public bool NugetPack()
        {
            var process = new Process();
            //NugetPackProcess = new Process();
            //NugetPackProcess.Exited += (sender, args) =>
            //{
            //    Output += $"NugetPackProcess ExitCode: {process.ExitCode}\r\n";
            //    //NugetPackProcess.Dispose();
            //};

            process.StartInfo = GetDefaultProcessStartInfo(NugetPath, GetNugetPackCmd());
            process.EnableRaisingEvents = true;
            //process.OutputDataReceived += (sender, args) => OutputDataReceived?.Invoke(DataReceivedSource.NugetPack, DataReceivedType.Output, args.Data);
            //process.ErrorDataReceived += (sender, args) => OutputDataReceived?.Invoke(DataReceivedSource.NugetPack, DataReceivedType.Error, args.Data);
            process.OutputDataReceived += (sender, args) => Output += $"[NugetPack, Output] {args.Data}\r\n";
            process.ErrorDataReceived += (sender, args) => Output += $"[NugetPack, Error] {args.Data}\r\n";

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

            Log.Info($"CancelErrorRead...");
            process.CancelErrorRead();
            Log.Info($"CancelErrorRead");

            return process.ExitCode == 0;
        }

        public string GetNupkgFile()
        {
            return $"{Name}.{Version}.nupkg";
        }

        public string GetNupkgPath()
        {
            return Path.Combine(OutputDirectory, GetNupkgFile());
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
            //var noDelta = NoDelta ? " --no-delta" : "";
            return $@"--releasify {GetNupkgPath()}{noMsi} --no-delta --releaseDir={SquirrelReleaseDir}";
        }

        public bool SquirrelReleasify()
        {
            var process = new Process();
            process.StartInfo = GetDefaultProcessStartInfo(SquirrelPath, GetSquirrelCmd());
            process.EnableRaisingEvents = true;
            //process.OutputDataReceived += (sender, args) => OutputDataReceived?.Invoke(DataReceivedSource.Squirrel, DataReceivedType.Output, args.Data);
            //process.ErrorDataReceived += (sender, args) => OutputDataReceived?.Invoke(DataReceivedSource.Squirrel, DataReceivedType.Error, args.Data);
            process.OutputDataReceived += (sender, args) => Output += $"[Squirrel, Output] {args.Data}\r\n";
            process.ErrorDataReceived += (sender, args) => Output += $"[Squirrel, Error] {args.Data}\r\n";

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

            Log.Info($"CancelErrorRead...");
            process.CancelErrorRead();
            Log.Info($"CancelErrorRead");

            return process.ExitCode == 0;
        }

        public void LeaveOneLineInReleasesFile()
        {
            // RELEASES вроде как нужен для дельта-пакетов
            // но можно сломать обнову клиентам, если проетерять этот файл
            // поэтому поддерживать дельту не буду
            // просто в RELEASES оставляем одну строку с последней версией

            var releaseFile = Path.Combine(SquirrelReleaseDir, "RELEASES");

            var lines = File.ReadAllLines(releaseFile, Encoding.UTF8);

            var lineWithCurrentVersion = lines.FirstOrDefault(x => x.Contains(Path.GetFileName(GetSquirrelReleaseNupkgPath())));

            File.WriteAllText(releaseFile, lineWithCurrentVersion, Encoding.UTF8);
        }

        public string GetSquirrelReleaseNupkgPath()
        {
            return $"{Path.Combine(SquirrelReleaseDir, $"{Name}-{Version}-full.nupkg")}";
        }

        public void Upload()
        {
            if (!Directory.Exists(UploadPath))
            {
                Directory.CreateDirectory(UploadPath);
            }

            var currentNuget = Path.GetFileName(GetSquirrelReleaseNupkgPath());

            var filesToCopy = new List<string>();
            filesToCopy.Add(currentNuget);
            filesToCopy.Add("RELEASES");
            filesToCopy.Add("Setup.exe");
            if (!NoMsi)
            {
                filesToCopy.Add("Setup.msi");
            }

            var files = Directory.GetFiles(SquirrelReleaseDir).Where(x => filesToCopy.Contains(Path.GetFileName(x)));
            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);
                Log.Info($"{file}");

                if (fileName == "Setup.exe" || fileName == "Setup.msi" || fileName == "RELEASES")
                {
                    File.Copy(file, Path.Combine(UploadPath, Path.GetFileName(file)), true);
                }
                else if (fileName == currentNuget)
                {
                    File.Copy(file, Path.Combine(UploadPath, Path.GetFileName(file)));
                }
            }
        }

        public void Publish()
        {
            if (File.Exists(GetSquirrelReleaseNupkgPath()))
            {
                Log.Warn($"Версия {Version} уже релизнута");
                return;
            }

            if (!NugetPack())
            {
                Log.Warn($"Ошибка NugetPack");
                return;
            }

            if (IsDeleteDependenciesFromNuspecEnabled)
            {
                DeleteDependenciesFromNuspec();
            }

            if (!SquirrelReleasify())
            {
                Log.Warn($"Ошибка SquirrelReleasify");
                return;
            }

            LeaveOneLineInReleasesFile();

            Upload();
        }
    }
}