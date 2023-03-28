﻿using System;
using System.Collections.Generic;
using CNP.Helper;
using System.Linq;

namespace CNP.Language
{
  public class And : IProgram
  {
    public const int AND_MAX_ARITY = 5;

    public readonly static AndValenceSeries AndValences = AndValenceSeries.Generate(AND_MAX_ARITY);

    public readonly IProgram LHOperand, RHOperand;

    public And(IProgram lhOperand, IProgram rhOperand) 
    {
      LHOperand = lhOperand;
      RHOperand = rhOperand;
    }

    /// <summary>
    /// The valence that lead to this program.
    /// </summary>
    public string DebugValenceString { get; set; }
    /// <summary>
    /// The observations that lead to this program.
    /// </summary>
    public string DebugObservationString { get; set; }

    public void ReplaceFree(Free free, ITerm term)
    {
      LHOperand.ReplaceFree(free, term);
      RHOperand.ReplaceFree(free, term);
    }

    public bool IsClosed => LHOperand.IsClosed && RHOperand.IsClosed;

    public string Accept(ICNPVisitor ps)
    {
      return ps.Visit(this);
    }

    public IProgram Clone(CloningContext cc)
    {
      return cc.Clone(this);
    }

    public string GetTreeQualifier()
    {
      return "and(" + LHOperand.GetTreeQualifier() + "," + RHOperand.GetTreeQualifier() + ")";
    }

    public int GetHeight()
    {
      return Math.Max(LHOperand.GetHeight(), RHOperand.GetHeight()) + 1;
    }

    public ObservedProgram FindLeftmostHole()
    {
      return LHOperand.FindLeftmostHole() ?? RHOperand.FindLeftmostHole();
    }

    public static IEnumerable<ProgramEnvironment> CreateAtFirstHole(ProgramEnvironment origEnv)
    {
      var origObservation = origEnv.Root.FindHole();
      if (origObservation.RemainingSearchDepth < 2)
        return Array.Empty<ProgramEnvironment>();
      if ((origObservation.Constraints & ObservedProgram.Constraint.NotAnd) == ObservedProgram.Constraint.NotAnd)
        return Array.Empty<ProgramEnvironment>();
      var debugInfo = origObservation.GetDebugInformation(origEnv);
      Mode[] modes = origObservation.Valence.GetModesOrderedByNames(origObservation.Observables.Names);
      ProtoAndValence[] valences = AndValences.GetValencesForOpMode(modes);
      NameVar[] opNames = origObservation.Observables.Names;
      ProgramEnvironment[] programs = new ProgramEnvironment[valences.Length];
      for(int i=0; i<valences.Length; i++)
      {
        ProtoAndValence protVal = valences[i];
        var (lhNames, lhIndices, lhNamesOfIns, lhNamesOfOuts) = getNamesIndicesModesForNames(protVal.LHModes, opNames);
        var (rhNames, rhIndices, rhNamesOfIns, rhNamesOfOuts) = getNamesIndicesModesForNames(protVal.RHModes, opNames);
        var (lhTuples, rhTuples) = origObservation.Observables.GetCroppedTo2ByIndices(lhIndices, rhIndices);
        int remSearchDepth = origObservation.RemainingSearchDepth - 1;
        int remUnbound = origObservation.RemainingUnboundArguments;
        var constraint = ObservedProgram.Constraint.NotAnd;
        var lhRel = new AlphaRelation(lhNames, lhTuples);
        var lhVal = new ValenceVar(lhNamesOfIns, lhNamesOfOuts);
        var lhObs = new ObservedProgram(lhRel, lhVal, remSearchDepth, remUnbound, constraint);
        var rhRel = new AlphaRelation(rhNames, rhTuples);
        var rhVal = new ValenceVar(rhNamesOfIns, rhNamesOfOuts);
        var rhObs = new ObservedProgram(rhRel, rhVal, remSearchDepth, remUnbound, constraint);
        var andProg = new And(lhObs, rhObs);
        (andProg as IProgram).SetDebugInformation(debugInfo);
        var onlyLHNames = protVal.OnlyLHIndices.Select(i => opNames[i]).ToArray();
        var onlyRHNames = protVal.OnlyRHIndices.Select(i => opNames[i]).ToArray();
        var prog = origEnv.Clone((origObservation, andProg), (onlyLHNames,onlyRHNames));
        programs[i] = prog;
      }
      return programs;
    }


    private static (short index, Mode mode)[] getModesAsNonNull(Mode?[] modes)
    {
      return modes.Select((mm, i) => ((short)i, mm))
                  .Where(e => e.mm.HasValue)
                  .Select(e => (e.Item1, e.mm.Value))
                  .ToArray();
    }

    private static (NameVar[] names, short[] indices, NameVar[] namesOfIns, NameVar[] namesOfOuts) getNamesIndicesModesForNames(Mode?[] modes, NameVar[] names)
    {
      (short index, Mode mode)[] indicesModes = getModesAsNonNull(modes);
      var namesarr = new NameVar[indicesModes.Length];
      var indicesarr = new short[indicesModes.Length];
      List<NameVar> namesOfIns = new List<NameVar>(indicesModes.Length);
      List<NameVar> namesOfOuts = new List<NameVar>(indicesModes.Length);
      for(int i=0; i<indicesModes.Length; i++)
      {
        namesarr[i] = names[indicesModes[i].index];
        indicesarr[i] = indicesModes[i].index;
        if (indicesModes[i].mode == Mode.In)
          namesOfIns.Add(namesarr[i]);
        else
          namesOfOuts.Add(namesarr[i]);
      }
      return (namesarr, indicesarr, namesOfIns.ToArray(), namesOfOuts.ToArray());
    }
  }
}
