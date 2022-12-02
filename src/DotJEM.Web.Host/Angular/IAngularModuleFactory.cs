using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DotJEM.Web.Host.Util;

namespace DotJEM.Web.Host.Angular;

public interface IAngularModuleFactory
{
    IAngularModule Load(string path);
}

internal class AngularModuleFactory : IAngularModuleFactory
{
    public static Regex Module = new Regex(@"angular\.module\s*\((?<definition>.*?\[.*?\].*?)\)", RegexOptions.Compiled);
    public static Regex Definition = new Regex(@"('(?<name>.+?)'|""(?<name>.+?)"")\s*,\s*\[(?<dependencies>.*?)\]", RegexOptions.Compiled);
    public static Regex Dependencies = new Regex(@"('(?<name>.+?)'|""(?<name>.+?)"")", RegexOptions.Compiled);

    private readonly AngularContext context;

    public AngularModuleFactory(AngularContext context)
    {
        this.context = context;
    }

    public IAngularModule Load(string path)
    {
        HashSet<string> scripts = SortScripts(path);
        string definition = ExtractDefinition(LoadSource(scripts.First()));
        Match match = Definition.Match(definition);
        string[] dependencies = ParseDependencies(match.Groups["dependencies"].ToString()).ToArray();

        return new AngularModule(
            match.Groups["name"].ToString(),
            scripts,
            dependencies.Select(name => new AngularDependency(name, context)));
    }

    private static HashSet<string> SortScripts(string path)
    {
        HashSet<string> sources = new HashSet<string>();
        IEnumerable<string> scripts = Directory.GetFiles(path, "*.js", SearchOption.AllDirectories);
        sources.Add(scripts.Single(script => script.EndsWith(".module.js")));
        scripts.ForEach(script => sources.Add(script));
        return sources;
    }


    private static string LoadSource(string module)
    {
        string source = File.ReadAllLines(module).Aggregate((aggregate, next) => aggregate + " " + next);
        return Regex.Replace(source, "\\s+", " ");
    }

    private static string ExtractDefinition(string source)
    {
        return Module.Match(source).Groups["definition"].ToString().Trim();
    }

    private static IEnumerable<string> ParseDependencies(string dependencyString)
    {
        return from Match dependency in Dependencies.Matches(dependencyString)
            select dependency.Groups["name"].ToString();
    }
}