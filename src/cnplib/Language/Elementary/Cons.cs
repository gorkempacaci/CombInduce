﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using CNP.Helper.EagerLinq;
using CNP.Helper;
using System.Diagnostics;

namespace CNP.Language
{

  public class Cons : IProgram
  {
    private static ElementaryValenceSeries ConsValences =
  ElementaryValenceSeries.SeriesFromArrays(new[] { "a", "b", "ab" },
                                new[]
                                {
                                      new[]{  Mode.In,  Mode.In, Mode.In},
                                      new[]{  Mode.In,  Mode.In, Mode.Out},
                                      new[]{  Mode.In, Mode.Out, Mode.In},
                                      new[]{  Mode.Out, Mode.In, Mode.In},
                                      new[]{  Mode.Out, Mode.Out, Mode.In},
                                });

    /// <summary>
    /// The valence that lead to this program.
    /// </summary>
    public string DebugValenceString { get; set; }
    /// <summary>
    /// The observations that lead to this program.
    /// </summary>
    public string DebugObservationString { get; set; }

    public bool IsClosed => true;

    public override int GetHashCode() => 31;

    public override bool Equals(object obj) => obj is Cons;

    public void ReplaceFree(Free _, ITerm __) { }

    public string Accept(ICNPVisitor ps)
    {
      return ps.Visit(this);
    }

    public IProgram Clone(CloningContext cc)
    {
      return cc.Clone(this);
    }


    public ObservedProgram FindLeftmostHole() => null;

    public (ObservedProgram, int) FindRootmostHole(int calleesDistanceToRoot = 0) => (null, int.MaxValue);

    public int GetHeight() => 0;

    public string GetTreeQualifier() => "p";



    /// <summary>
    /// Does not modify the given program, returns alternative cloned programs if they exist.
    /// </summary>
    public static IEnumerable<ProgramEnvironment> CreateAtFirstHole(ProgramEnvironment env)
    {
      List<ProgramEnvironment> programs = new();
      ObservedProgram oldObs = env.Root.FindHole();
      if (oldObs.Observables.TuplesCount == 0)
        throw new ArgumentException("Cons: Observation is empty.");

      ConsValences.GroundingAlternatives(oldObs.Valence, env.NameBindings, out var alts);
      foreach (var altNames in alts)
      {
        var currEnv = env.Clone();
        var observ = currEnv.Root.FindHole();
        if (currEnv.NameBindings.TryBindingAllNamesToGround(observ.Valence, altNames))
        { // then all names for valence are ground
          string[] obsNames = currEnv.NameBindings.GetNamesForVars(observ.Observables.Names);
          int a = Array.IndexOf(obsNames, "a");
          int b = Array.IndexOf(obsNames, "b");
          int ab = Array.IndexOf(obsNames, "ab");
          bool unificationSuccess = true;
          for (int ri = 0; ri < observ.Observables.TuplesCount; ri++)
          {
            var tuple = observ.Observables.Tuples[ri];
            var unifier = new ITerm[tuple.Length];
            unifier[ab] = new TermList(tuple[a], tuple[b]);
            if (!currEnv.UnifyInPlace(tuple, unifier))
            {
              unificationSuccess = false;
              break;
            }
          }
          if (unificationSuccess && observ.IsAllOutArgumentsGround())
          {
            Cons cns = new Cons();
            (cns as IProgram).SaveDebugInformationString(currEnv, observ);
            programs.Add(currEnv.Clone((observ, cns)));
          }
        } // if not then this alt is skipped
      }
      return programs;
    }

  }

}
