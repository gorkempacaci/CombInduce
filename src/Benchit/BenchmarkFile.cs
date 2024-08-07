﻿using System;
using System.Text.Json;
using CNP.Language;
using CNP.Parsing;

namespace Benchit
{
  public readonly record struct BenchmarkFileEntry(string Name, string[] ExpectedPrograms, string Valence, string Examples, string NegExamples)
  {
    public SynTask Parse(int searchDepth, int maxUnboundArguments)
    {
      NameVarBindings names = new();
      FreeFactory frees = new();
      ValenceVar vv = Parser.ParseValence(Valence, names);
      AlphaRelation examples = Parser.ParseAlphaTupleSet(Examples, names, frees);
      GroundRelation negExamples = Parser.ParseGroundRelation(NegExamples, frees);
      ObservedProgram obs = new ObservedProgram(examples, vv, searchDepth, maxUnboundArguments, ObservedProgram.Constraint.None);
      ProgramEnvironment env = new ProgramEnvironment(obs, names, frees);
      return new SynTask(Name, ExpectedPrograms, env, negExamples);
    }
  }

  public readonly record struct SynTask(string Name, string[] ExpectedPrograms, ProgramEnvironment ProgramEnv, GroundRelation NegativeExamples) { }

  public class BenchmarkFile
  {
    public int SearchDepth { get; set; }
    public int MaxUnboundArguments { get; set; }
    public int WaitBetweenRunsMaxms { get; set; } // max ms wait between runs
    public BenchmarkFileEntry[] Tasks { get; set; } = Array.Empty<BenchmarkFileEntry>();
    
    

    private SynTask[] ParseAllTasks(out int maxWaitMsBetweenRuns)
    {
      List<SynTask> synTasks = new();
      maxWaitMsBetweenRuns = WaitBetweenRunsMaxms;
      for (int i = 0; i < Tasks.Length; i++)
      {
        try
        {
          synTasks.Add(Tasks[i].Parse(SearchDepth, MaxUnboundArguments)); // parse valences and examples into CNP
        }
        catch (Exception e)
        {
          Console.WriteLine("Error while parsing " + Tasks[i].Name);
          Console.WriteLine(e);
        }
      }
      return synTasks.ToArray();
    }

    public static SynTask[] ReadFromFile(string filename, out int maxWaitmsBetweenRuns)
    {
      string jsonContent = File.ReadAllText(filename);
      try
      {
        BenchmarkFile file = JsonSerializer.Deserialize<BenchmarkFile>(jsonContent)!;
        Console.WriteLine("Number of benchmarks read: " + file.Tasks.Length);
        return file.ParseAllTasks(out maxWaitmsBetweenRuns);
      }
      catch(Exception e)
      {
        throw new FileLoadException("Can't find or parse the benchmarks file.", e);
      }

    }

    public static string GetExampleBenchmarksFileString()
    {
      BenchmarkFileEntry[] arr = new BenchmarkFileEntry[] {
      new(){ Name="append", ExpectedPrograms=new[]{"foldr(cons, id)" }, Valence="{b0:in, as:in, bs:out}",
        Examples="{{b0:[4,5,6], as:[1,2,3], bs:[1,2,3,4,5,6]}]", NegExamples="[]"},
      new(){ Name="reverse3", ExpectedPrograms=new[]{"foldl(cons, id)" }, Valence="{b0:in, as:in, bs:out}",
        Examples="[{b0:[], as:[1,2,3], b:[3,2,1]}]", NegExamples="[]"}
      };
      BenchmarkFile file = new BenchmarkFile { SearchDepth = 4, Tasks = arr };
      string benchmarks_example = JsonSerializer.Serialize(file, new JsonSerializerOptions { WriteIndented = true });
      return benchmarks_example;
    }
  }



}

