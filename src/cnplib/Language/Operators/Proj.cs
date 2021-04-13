﻿using System;
using System.Collections.Generic;
using CNP.Helper;
using CNP.Helper.EagerLinq;

namespace CNP.Language
{
  public class Proj : Program
  {
    /// <summary>
    /// How many out arguments is proj allowed to eliminate. (or inversely, 'introduce' during synthesis)
    /// </summary>
    private const int MAX_ELIMINATED_OUT_ARGS = 2;
    public readonly Program Source;
    public readonly ProjectionMap Projection;

    public Proj(Program sourceProgram, ProjectionMap projection) : base(sourceProgram.IsClosed)
    {
      this.Source = sourceProgram;
      this.Projection = projection;
    }

    public override int GetHeight()
    {
      return Source.GetHeight() + 1;
    }

    public override void SetAllRootsTo(Program newRoot)
    {
      this.Root = newRoot;
      Source.SetAllRootsTo(newRoot);
    }

    protected override Program CloneNode(TermReferenceDictionary trd)
    {
      var p = new Proj(this.Source.CloneAsSubTree(trd), this.Projection.Clone(trd));
      return p;
    }

    protected override Program CloneAndReplaceObservationAtNode(ObservedProgram oldComponent, Program newComponent, TermReferenceDictionary plannedParenthood)
    {
      var p = new Proj(Source.CloneAndReplaceObservationAsSubTree(oldComponent, newComponent, plannedParenthood), Projection.Clone(plannedParenthood));
      return p;
    }

    internal override ObservedProgram FindFirstHole()
    {
      return Source.FindFirstHole();
    }

    public static IEnumerable<Program> CreateAtFirstHole(Program originalProgram)
    {
      var origObservation = originalProgram.FindFirstHole();
      if (origObservation.DTL == 0)
        return Iterators.Empty<Program>();
      if ((origObservation.Constraints & ObservedProgram.Constraint.NotProjection) == ObservedProgram.Constraint.NotProjection)
        return Iterators.Empty<Program>();
      int projNumberOfOuts = origObservation.Valence.OutsCount;
      int maxIntroducedOuts = projNumberOfOuts == 0 ? 0 : MAX_ELIMINATED_OUT_ARGS;
      List<Program> programs = new List<Program>(maxIntroducedOuts + 1);
      // if proj is 3i3o, source can be 3i1o, 3i2o, 3i3o. all inputs should be projected, and at least one output should be projected, if there are any outputs.
      for (int i_outs = 0; i_outs <= maxIntroducedOuts; i_outs++)
      {
        TermReferenceDictionary plnprn = new();
        var rootProgram = originalProgram.CloneAtRoot(plnprn);
        ObservedProgram obs = rootProgram.FindFirstHole();
        // projections map the domains(non-eliminated) of the new observation to the domains of proj expression.
        var projection = obs.Valence.Keys.ToDictionary(n => NameVar.NewUnbound(), n => n);
        // inverse projection maps the domains of proj to domains of observation
        var invProjection = projection.ToDictionary(kv => kv.Value, kv => kv.Key);
        // proj may have eliminated some domains, these will have free names and Out modes.
        var eliminatedDoms = Enumerable.Range(0, i_outs).ToDictionary(_ => NameVar.NewUnbound(), _ => Mode.Out);
        // terms are needed for the eliminated domains for the new observation
        Func<IEnumerable<NameVar>, Dictionary<NameVar, Term>> makeFreeTerms = doms => doms.ToDictionary(d => d, _ => new Free() as Term);
        // function returns a new alpha tuple where the terms are the same but domains are replaced with those in the source.
        Func<AlphaTuple, AlphaTuple> projToSource = patu => new AlphaTuple(patu.Terms.ToDictionary(t => invProjection[t.Key], t => t.Value).Concat(makeFreeTerms(eliminatedDoms.Keys)));
        var sourceTuples = obs.Observables.Select(projToSource);
        var sourceDoms = obs.Valence.ToDictionary(nv => invProjection[nv.Key], nv => nv.Value).Concat(eliminatedDoms);
        var sourceProgram = new ObservedProgram(sourceTuples, new Valence(sourceDoms), obs.DTL - 1, ObservedProgram.Constraint.NotProjection);
        var program = new Proj(sourceProgram, new(projection));
        program.SetFoundingState(originalProgram.CloneAtRoot());
        rootProgram = rootProgram.CloneAndReplaceObservation(obs, program);
        program.DebugTag = rootProgram.CloneAtRoot();
        programs.Add(rootProgram);
      }
      return programs;
    }

    public override int GetHashCode()
    {
      return Projection.GetHashCode();
    }

    public override bool Equals(object obj)
    {
      if (obj is not Proj otherProj)
        return false;
      return Projection.EqualsAsDictionary(otherProj.Projection) && Source.Equals(otherProj.Source);
    }

    public override string ToString()
    {
      return "proj("+Source.ToString()+","+Projection.ToString()+")";
    }
  }
}
