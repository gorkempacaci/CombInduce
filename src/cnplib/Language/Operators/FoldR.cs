﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using CNP.Helper;
using CNP.Helper.EagerLinq;

namespace CNP.Language
{
    public class FoldR : Fold
    {
        public override ISet<string> ArgumentNames => foldrArgumentNames;
        protected static readonly HashSet<string> foldrArgumentNames = new HashSet<string>(new string[] { "b0", "as", "b" });
        public FoldR(Program recursiveCase, Program baseCase) : base(recursiveCase, baseCase) { }
        public override string ToString()
        {
            return "foldr(" + Recursive.ToString() + "," + Base.ToString() + ")";
        }
        internal override ObservedProgram FindFirstHole()
        {
            return Base.FindFirstHole() ?? Recursive.FindFirstHole();
        }
        public override Program CloneAndReplace(ObservedProgram oldComponent, Program newComponent, FreeDictionary plannedParenthood)
        {
            return new FoldR(Base.CloneAndReplace(oldComponent, newComponent, plannedParenthood),
                                Recursive.CloneAndReplace(oldComponent, newComponent, plannedParenthood));
        }

        // public static IEnumerable<Fold> FromObservationBase(ObservedProgram obs)
        // {
        //     
        // }
        public static IEnumerable<FoldR> FromObservation(ObservedProgram obs)
        {
            if (!obs.ArgumentNames.SetEquals(foldrArgumentNames) ||
                !Valences.FoldR.TryGetValue(obs.Signature, out IEnumerable<OperatorCombinedSignature> pqSignatures))
            {
                return Iterators.Empty<FoldR>();
            }
            List<AlphaTuple> pObs = new List<AlphaTuple>(), qObs = new List<AlphaTuple>();
            foreach (AlphaTuple at in obs.Observables)
                foldRtoPQ(at["b0"], at["as"], at["b"], pObs, qObs);
            if (!pObs.Any() || !qObs.Any())
                return Iterators.Empty<FoldR>();
            var newFolds = pqSignatures.Select(op =>
            {
                FreeDictionary fd = new FreeDictionary();
                return new FoldR(new ObservedProgram(pObs.Clone(fd), op.LeftOperandSignature),
                    new ObservedProgram(qObs.Clone(fd), op.RightOperandSignature));
            });
            return newFolds;
        }

        // foldr(P,Q)(A0,[],B) :- Q(A0,B).
        // foldr(P,Q)(A0,[A|At],B) :- foldr(P,Q)(A0,At,Acc), P(A,Acc,B).
        static void foldRtoPQ(Term b0, Term @as, Term b, List<AlphaTuple> atusP, List<AlphaTuple> atusQ, Term acc = null)
        {
            if (acc == null)
            {
                acc = b;
            }
            if (@as is TermList li)
            {
                Free f = new Free();
                atusP.Add(new AlphaTuple(("a", li.Head), ("b", f), ("ab", acc)));
                foldRtoPQ(b0, li.Tail, b, atusP, atusQ, f);
            } else if (@as is NilTerm)
            {
                atusQ.Add(new AlphaTuple(("a", b0), ("b", acc)));
            } else throw new Exception("foldRtoPQ: as is not list or nil.");
        }
    }
}
