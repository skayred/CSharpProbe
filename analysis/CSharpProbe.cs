using System;
using System.Linq;
using System.Threading.Tasks;
using ArchiMetrics.Analysis;
using Microsoft.CodeAnalysis.MSBuild;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using CommandLine;
using System.Net;
using System.Reflection;

namespace CSharpProbe
{
    public class Options
    {
        [Option('p', "path", Required = true, HelpText = "Project path")]
        public string Path { get; set; }

        [Option('d', "dry", Default = false, HelpText = "Dry run, data does not go anywhere")]
        public bool Dry { get; set; }

        [Option('m', "min", Default = false, HelpText = "Minimal mode, excludes all project info")]
        public bool MinMode { get; set; }

        [Option('H', "host", Required = false, HelpText = "API host")]
        public string Host { get; set; }

        [Option('P', "port", Required = false, HelpText = "API port")]
        public string Port { get; set; }

        [Option('T', "token", Required = false, HelpText = "API token")]
        public string Token { get; set; }

        [Option('N', "probe", Required = false, HelpText = "Associated probe ID")]
        public string ProbeID { get; set; }

        [Option('R', "revision", Required = false, HelpText = "CVS revision")]
        public string Revision { get; set; }

        [Option('D', "revision_date", Required = false, HelpText = "CVS revision date")]
        public string RevisionDate { get; set; }

        [Option('I', "project", Required = false, HelpText = "Project to analyze")]
        public string Project { get; set; }
    }

    class CSharpProbe
    {
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed<Options>(o =>
                {
                    var task = Run(o);
                    task.Wait();
                });
        }

        private static async Task Run(Options opts)
        {
            try
            {
                Console.WriteLine("Loading Solution");

                var workspace = MSBuildWorkspace.Create();
                workspace.WorkspaceFailed += (s, e) =>
                {
                    Console.WriteLine($"Workspace failed with: {e.Diagnostic}");
                };
                var solution = await workspace.OpenSolutionAsync(opts.Path);
                
                var projects = string.IsNullOrEmpty(opts.Project) ? solution.Projects.Where(p => p.FilePath.EndsWith("csproj")) : solution.Projects.Where(p => p.Name == opts.Project);

                Console.WriteLine("Loading metrics, wait it may take a while.");
                var metricsCalculator = new CodeMetricsCalculator();
                var calculateTasks = projects.Select(p => metricsCalculator.Calculate(p, solution));
                var metrics = (await Task.WhenAll(calculateTasks)).SelectMany(nm => nm);

                var currentAmount = 0;
                var maintainability = 0.0;
                var modules = new List<ResultModule>();
                var loc = 0;

                foreach (var metric in metrics)
                {
                    currentAmount += 1;
                    maintainability = maintainability + (metric.MaintainabilityIndex - maintainability) / (currentAmount + 1.0);
                    loc += metric.LinesOfCode;

                    if (!opts.MinMode)
                    {
                        modules.Add(new ResultModule(metric.Name, metric.MaintainabilityIndex));
                    }
                }

                var result = new Result(
                    maintainability,
                    opts.MinMode ? 0 : loc,
                    0,
                    opts.Revision,
                    opts.RevisionDate,
                    modules
                );

                UploadResult(opts, result);
            } catch (ReflectionTypeLoadException ex)
            {
                foreach (var item in ex.LoaderExceptions)
                {
                    Console.WriteLine(item.Message);
                }
            }
        }

        private static void UploadResult(Options opts, Result result)
        {
            if (opts.Dry)
            {
                var ms = new MemoryStream();

                var ser = new DataContractJsonSerializer(typeof(Payload));
                ser.WriteObject(ms, new Payload(opts.ProbeID, result));
                var json = ms.ToArray();
                ms.Close();
                var serialized = Encoding.UTF8.GetString(json, 0, json.Length);

                System.Console.WriteLine(serialized);
            }
            else
            {
                try
                {
                    var url = "http://" + opts.Host + ":" + opts.Port + "/api/probe_infos";

                    var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
                    httpWebRequest.ContentType = "application/json; charset=utf-8";
                    httpWebRequest.Method = "POST";
                    //httpWebRequest.PreAuthenticate = true;
                    httpWebRequest.Headers.Add("Authorization", "Token token=" + opts.Token);

                    var ms = new MemoryStream();

                    var ser = new DataContractJsonSerializer(typeof(Payload));
                    ser.WriteObject(ms, new Payload(opts.ProbeID, result));
                    var json = ms.ToArray();
                    ms.Close();
                    var serialized = Encoding.UTF8.GetString(json, 0, json.Length);

                    using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                    {
                        streamWriter.Write(serialized);
                        streamWriter.Flush();
                    }
                    var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                    using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                    {
                        var responseText = streamReader.ReadToEnd();
                        Console.WriteLine(responseText);
                    }
                }
                catch (WebException ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }
    }
}
