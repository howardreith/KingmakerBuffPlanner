using System;
using System.Collections.Generic;
using KingmakerBuffPlanner.Domain.Effects;
using Newtonsoft.Json;

namespace KingmakerBuffPlanner.Discovery
{
    public sealed class DiscoveryDiagnostic
    {
        public DiscoveryDiagnostic(
            string code, string nodeIdentity, string detail, string actionPath = "")
        {
            Code = code ?? string.Empty;
            NodeIdentity = nodeIdentity ?? string.Empty;
            Detail = detail ?? string.Empty;
            ActionPath = actionPath ?? string.Empty;
        }

        [JsonProperty("code", Order = 1)] public string Code { get; private set; }
        [JsonProperty("nodeIdentity", Order = 2)] public string NodeIdentity { get; private set; }
        [JsonProperty("detail", Order = 3)] public string Detail { get; private set; }
        [JsonProperty("actionPath", Order = 4)] public string ActionPath { get; private set; }
    }

    public sealed class DiscoveryScanResult
    {
        internal DiscoveryScanResult(EffectExpression expression, List<DiscoveryDiagnostic> diagnostics)
        {
            Expression = expression;
            Diagnostics = diagnostics.AsReadOnly();
        }

        public EffectExpression Expression { get; private set; }
        public IReadOnlyList<DiscoveryDiagnostic> Diagnostics { get; private set; }
    }

    public sealed class ActionGraphScanner
    {
        private readonly int _maximumDepth;

        public ActionGraphScanner(int maximumDepth = 64)
        {
            if (maximumDepth < 1 || maximumDepth > 256)
                throw new ArgumentOutOfRangeException("maximumDepth");
            _maximumDepth = maximumDepth;
        }

        public DiscoveryScanResult Scan(DiscoveryNode root)
        {
            if (root == null) throw new ArgumentNullException("root");
            var diagnostics = new List<DiscoveryDiagnostic>();
            var active = new HashSet<DiscoveryNode>(ReferenceEqualityComparer<DiscoveryNode>.Instance);
            EffectExpression expression = Visit(root, 0, active, diagnostics, null, root.Identity);
            return new DiscoveryScanResult(expression, diagnostics);
        }

        private EffectExpression Visit(
            DiscoveryNode node,
            int depth,
            HashSet<DiscoveryNode> active,
            List<DiscoveryDiagnostic> diagnostics,
            EffectTarget? targetOverride,
            string path)
        {
            if (node == null) return new EmptyEffectExpression();
            if (depth > _maximumDepth)
            {
                diagnostics.Add(new DiscoveryDiagnostic(
                    "maximum-depth", node.Identity, depth.ToString(), path));
                return new EmptyEffectExpression();
            }
            if (!active.Add(node))
            {
                diagnostics.Add(new DiscoveryDiagnostic(
                    "cycle", node.Identity, "Active traversal cycle detected.", path));
                return new EmptyEffectExpression();
            }
            try
            {
                switch (node.Kind)
                {
                    case DiscoveryNodeKind.Empty:
                        return new EmptyEffectExpression();
                    case DiscoveryNodeKind.Effect:
                        return new EffectLeafExpression(
                            node.EffectKind,
                            node.EffectId,
                            ResolveLeafTarget(targetOverride, node.Target),
                            node.SourceContract,
                            path);
                    case DiscoveryNodeKind.OffensiveAction:
                        diagnostics.Add(new DiscoveryDiagnostic(
                            "offensive-action", node.Identity, node.SourceContract, path));
                        return new EmptyEffectExpression();
                    case DiscoveryNodeKind.RestorativeAction:
                        diagnostics.Add(new DiscoveryDiagnostic(
                            "restorative-action", node.Identity, node.SourceContract, path));
                        return new EmptyEffectExpression();
                    case DiscoveryNodeKind.Sequence:
                        return VisitSequence(node, depth, active, diagnostics, targetOverride, path);
                    case DiscoveryNodeKind.Conditional:
                        return new ConditionalEffectExpression(
                            node.ConditionContract,
                            Visit(node.WhenTrue, depth + 1, active, diagnostics, targetOverride, path + "/true"),
                            Visit(node.WhenFalse, depth + 1, active, diagnostics, targetOverride, path + "/false"));
                    case DiscoveryNodeKind.TargetTransform:
                        EffectTarget transformedTarget = ComposeTarget(
                            targetOverride, node.Target);
                        return new TargetedEffectExpression(
                            transformedTarget,
                            VisitSequence(node, depth, active, diagnostics, transformedTarget, path));
                    case DiscoveryNodeKind.AbilityReference:
                        return new ReferencedAbilityExpression(
                            node.ReferencedAbilityId,
                            VisitSequence(node, depth, active, diagnostics, targetOverride, path));
                    default:
                        diagnostics.Add(new DiscoveryDiagnostic(
                            "unknown-node", node.Identity, node.SourceContract, path));
                        return new EmptyEffectExpression();
                }
            }
            finally { active.Remove(node); }
        }

        private EffectExpression VisitSequence(
            DiscoveryNode node,
            int depth,
            HashSet<DiscoveryNode> active,
            List<DiscoveryDiagnostic> diagnostics,
            EffectTarget? targetOverride,
            string path)
        {
            var children = new List<EffectExpression>();
            for (int i = 0; i < node.Children.Count; i++)
                children.Add(Visit(node.Children[i], depth + 1, active, diagnostics, targetOverride,
                    path + "/" + i + ":" + node.Children[i].Identity));
            return new SequenceEffectExpression(children);
        }

        private static EffectTarget ResolveLeafTarget(
            EffectTarget? targetOverride,
            EffectTarget leafTarget)
        {
            return leafTarget == EffectTarget.CurrentTarget && targetOverride != null
                ? targetOverride.Value : leafTarget;
        }

        private static EffectTarget ComposeTarget(
            EffectTarget? outerTarget,
            EffectTarget transformTarget)
        {
            return transformTarget == EffectTarget.CurrentTarget && outerTarget != null
                ? outerTarget.Value : transformTarget;
        }
    }

    internal sealed class ReferenceEqualityComparer<T> : IEqualityComparer<T> where T : class
    {
        internal static readonly ReferenceEqualityComparer<T> Instance = new ReferenceEqualityComparer<T>();
        public bool Equals(T x, T y) { return ReferenceEquals(x, y); }
        public int GetHashCode(T obj) { return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj); }
    }
}
