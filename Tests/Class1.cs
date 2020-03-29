using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Publisher;
using Tools;

namespace Tests
{
    public class Class1
    {
        public Q Q = new Q();

        public Project Project = new Project();

        [SetUp]
        public void SetUp()
        {
            Helper.MagickInitMethod();

            Project.CsprojPath = @"c:\Users\buh\source\repos\buherpe\Publisher\Publisher\Publisher.csproj";
            Project.OutputDirectory = @"K:\";
            Project.SquirrelReleaseDir = @"K:\SquirrelReleases\Publisher";
            Project.UploadPath = @"K:\OSPanel\domains\buherpet.tk\updates\Publisher";
            Project.OutputDataReceived += (dataReceivedSource, dataReceivedType, data) => { Console.WriteLine($"[{dataReceivedSource}, {dataReceivedType}] {data}"); };

            Project.LoadName();
            Project.LoadVersion();
        }

        [Test]
        public void Test1()
        {
            Project.Publish(Q.NugetPath, Q.SquirrelPath);
        }
    }
}