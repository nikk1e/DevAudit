﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

using Sprache;
using Versatile;

using DevAudit.AuditLibrary;

namespace DevAudit.Tests
{
    public abstract class PackageSourceTests
    {
        #region Constructors
        public PackageSourceTests()
        {
            DPS = this.Sources.Select(s => s as IDeveloperPackageSource).ToList();
        }
        #endregion

        #region Abstract Properties
        protected abstract List<PackageSource> Sources { get; }
        #endregion

        #region Abstract Tsests
        public abstract void CanTestVulnerabilityVersionInPackageVersionRange();

        public abstract void IsDeveloperPackageSource();

        public abstract void CanGetDPSMinimumPackageVersion();
        #endregion

        #region Properties
        protected CancellationTokenSource Cts { get; } = new CancellationTokenSource();

        protected List<IDeveloperPackageSource> DPS { get; }
        #endregion

        #region Methods
        protected static void EnvironmentMessageHandler(object sender, EnvironmentEventArgs e) { }
        #endregion

        #region Tests
        [Fact]
        public virtual void CanConstructPackageSources()
        {
            Assert.All(Sources, s => Assert.NotNull(s));
        }

        [Fact]
        public virtual void CanGetPackages()
        {
            Assert.All(Sources, s => Assert.NotEmpty(s.GetPackages()));
        }

        [Fact]
        public virtual void CanGetVulnerabilities()
        {
            Assert.All(Sources, s => Assert.Equal(AuditTarget.AuditResult.SUCCESS, s.Audit(Cts.Token)));
        }

        [Fact]
        public void SemanticVersionCanParseRange()
        {
            var comparatorSets = SemanticVersion.Grammar.Range.Parse("6.1.0");
            Assert.NotEmpty(comparatorSets);
            Assert.Single(comparatorSets);
            var comparatorSet = comparatorSets.Single();
            var a = comparatorSet[0];
            Assert.Equal(ExpressionType.Equal, a.Operator);

            comparatorSet = SemanticVersion.Grammar.Range.Parse("<2.0.0").Single();
            Assert.Equal(2, comparatorSet.Count);
            Assert.Contains(comparatorSet, c => c.Operator == ExpressionType.LessThan);

            var max = comparatorSet.Single(c => c.Operator == ExpressionType.LessThan);
            Assert.Equal(2, max.Version.Major);
            var min = comparatorSet.Single(c => c.Operator == ExpressionType.GreaterThan);
            Assert.Equal(0, min.Version.Major);

            comparatorSet = SemanticVersion.Grammar.Range.Parse("<=3.1.4").Single();
            Assert.Equal(2, comparatorSet.Count);
            Assert.Contains(comparatorSet, c => c.Operator == ExpressionType.LessThanOrEqual);
            Assert.Contains(comparatorSet, c => c.Operator == ExpressionType.GreaterThan);

            max = comparatorSet.Single(c => c.Operator == ExpressionType.LessThanOrEqual);
            Assert.Equal(3, max.Version.Major);
            min = comparatorSet.Single(c => c.Operator == ExpressionType.GreaterThan);
            Assert.Equal(0, min.Version.Major);

            comparatorSet = SemanticVersion.Grammar.Range.Parse(">0.4.2").Single();
            Assert.Equal(2, comparatorSet.Count);
            Assert.Contains(comparatorSet, c => c.Operator == ExpressionType.GreaterThan);
            Assert.Contains(comparatorSet, c => c.Operator == ExpressionType.LessThan);

            max = comparatorSet.Single(c => c.Operator == ExpressionType.LessThan);
            Assert.Equal(1000000, max.Version.Major);
            min = comparatorSet.Single(c => c.Operator == ExpressionType.GreaterThan);
            Assert.Equal(4, min.Version.Minor);

            comparatorSet = SemanticVersion.Grammar.Range.Parse(">=2.7.1").Single();
            Assert.Equal(2, comparatorSet.Count);
            Assert.Contains(comparatorSet, c => c.Operator == ExpressionType.GreaterThanOrEqual);
            Assert.Contains(comparatorSet, c => c.Operator == ExpressionType.LessThan);

            max = comparatorSet.Single(c => c.Operator == ExpressionType.LessThan);
            Assert.Equal(1000000, max.Version.Major);
            min = comparatorSet.Single(c => c.Operator == ExpressionType.GreaterThanOrEqual);
            Assert.Equal(1, min.Version.Patch);

            comparatorSet = SemanticVersion.Grammar.Range.Parse("=4.6.6").Single();
            Assert.Single(comparatorSet);
            Assert.Contains(comparatorSet, c => c.Operator == ExpressionType.Equal);
        }
        #endregion
    }
}
