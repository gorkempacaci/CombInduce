﻿using System;
using System.Collections.Generic;
using CNP.Helper;
using CNP.Helper.EagerLinq;

namespace CNP.Language
{

  /// <summary>
  /// An unbound program variable, an observation. Immutable object.
  /// </summary>
  public class ObservedProgram : IProgram
  {
    [Flags] public enum Constraint : short { None = 0, NotProjection = 1, NotAnd = 2,
      /// <summary>
      /// Only And, elementary and library predicates. This is useful because if proj adds an unbound argument then
      /// it introduces this constraint so the source predicate expression will always be well-formed.
      /// </summary>
      OnlyAndElemLib = 4}

    public readonly Constraint Constraints;
    
    /// <summary>
    /// Sub-tree depth allowed for this hole. 0 should not exist, 1 would only match elementary predicates, higher values would match operators as well.
    /// </summary>
    public readonly int RemainingSearchDepth;

    /// <summary>
    /// Limiting the unbound arguments generated by 'proj' operator is useful to avoid universal predicates.
    /// </summary>
    public readonly int RemainingUnboundArguments;


    /// <summary>
    /// The valence that lead to this program.
    /// </summary>
    public string DebugValenceString { get; set; }

    /// <summary>
    /// The observations that lead to this program.
    /// </summary>
    public string DebugObservationString { get; set; }

    /// <summary>
    /// Disjunctive. If any of these observations is matched to a predicate, the search path is resolved.
    /// If it's matched to an operator, the disjunctive observations need all to be propagated.
    /// </summary>
    public Observation[] Observations { get; private set; }

    public ObservedProgram(AlphaRelation rel, ValenceVar val, int remSearchDepth, int remUnboundArgs, Constraint constraints)
      : this(new [] { new Observation(rel, val) }, remSearchDepth, remUnboundArgs, constraints) { }

    public ObservedProgram(Observation[] observations, int remSearchDepth, int remUnboundArgs, Constraint constraints)
    {
      Observations = observations;
      this.Constraints = constraints;
      this.RemainingSearchDepth = remSearchDepth;
      this.RemainingUnboundArguments = remUnboundArgs;
    }

    public string[] GetGroundNames(NameVarBindings nvb)
    {
      throw new InvalidOperationException("An observation does not expose names because it's not runnable.");
    }

    public RunResult _Run(ExecutionEnvironment env, GroundRelation rel)
    {
      throw new InvalidOperationException("An observation cannot be run.");
    }

    public void ReplaceFree(Free free, ITerm term)
    {
      foreach(Observation o in Observations)
        o.Examples.ReplaceFreeInPlace(free, term);
    }

    public bool IsClosed => false;

    public string Accept(ICNPVisitor ps)
    {
      return ps.Visit(this);
    }

    public IProgram Clone(CloningContext cc)
    {
      return cc.Clone(this);
    }

    public ObservedProgram FindLeftmostHole()
    {
      return this;
    }

    public int GetHeight()
    {
      return 0;
    }

    public int GetComplexityExponent()
    {
      throw new InvalidOperationException("Observation doesn't have complexity.");
    }

    public string GetTreeQualifier()
    {
      return "O";
    }
      
  }
}
