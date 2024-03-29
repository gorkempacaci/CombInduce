﻿using System.Collections.Generic;
using System.Linq;
using CNP;
using CNP.Helper;
using CNP.Language;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.Synthesis
{
  [TestClass]
  public class FoldLTests : TestBase
  {

    [TestMethod]
    public void DecomposeFoldL()
    {
      NameVarBindings nvb = new();
      var freeFact = new FreeFactory();
      NameVar b0 = nvb.AddNameVar("b0");
      NameVar @as = nvb.AddNameVar("as");
      NameVar b = nvb.AddNameVar("b");
      var names = new NameVar[] { b0, @as, b };
      var tups = new ITerm[][] { new ITerm[] { list(), list(1, 2, 3), list(3, 2, 1) } };
      var foldrel = new AlphaRelation(names, tups);
      ValenceVar vv = new ValenceVar(new[] { b0, @as }, new[] { b });
      ObservedProgram obs = new ObservedProgram(foldrel, vv, 2, 0, ObservedProgram.Constraint.None);
      ProgramEnvironment env = new ProgramEnvironment(obs, nvb, freeFact);
      FoldL.UnFoldL(foldrel, (0, 1, 2), env, out var pTuples);
      var stringer = new PrettyStringer(nvb);
      var pTuplesString = stringer.Visit(pTuples, FoldL.Valences.RecursiveCaseNames);
      Assert.AreEqual("{{a:1, b:[], ab:F0}, {a:2, b:F0, ab:F1}, {a:3, b:F1, ab:[3, 2, 1]}}", pTuplesString, "Recursive case tuples should match");
    }

    [TestMethod]
    public void Reverse3()
    {
      string typeStr = "{b0:in, as:in, b:out}";
      string atusStr = "{{b0:[], as:[1,2,3], b:[3,2,1]}}";
      assertFirstResultFor(typeStr, atusStr, "foldl(cons)");
    }

    
    [TestMethod]
    public void Reverse2()
    {
      string typeStr = "{as:in, bs:out}";
      string atusStr = "{{as:[1,2,3], bs:[3,2,1]}, {as:[], bs:[]}}";
      // proj(foldr(proj(cons, {a->a, ab->b, b->ab})), {b0->as, as->bs})
      assertFirstResultFor(typeStr, atusStr, "proj(and(const(b0, []), foldl(cons)), {as->as, b->bs})");
    }
    
  }
}
