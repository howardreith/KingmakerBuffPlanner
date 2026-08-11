using System;
using System.Collections.Generic;
using KingmakerBuffPlanner.Domain.Effects;
using Newtonsoft.Json;

namespace KingmakerBuffPlanner.Discovery
{
    public sealed class DiscoveryDiagnostic
    {
        public DiscoveryDiagnostic(string code, string nodeIdentity, string detail)
        {
            Code = code ?? string.Empty;
            NodeIdentity = nodeIdentity ?? string.Empty;
            Detail = detail ?? string.Empty;
        }

        [JsonProperty("code", Order = 1)] public string Code { get; private set; }
        [JsonProperty("nodeIdentity", Order = 2)] public string NodeIdentity { get; private set; }
        [JsonProperty("detail", Order = 3)] public string Detail { get; private set; }
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
                diagnostics.Add(new DiscoveryDiagnostic("maximum-depth", node.Identity, depth.ToString()));
                return new EmptyEffectExpression();
            }
            if (!active.Add(node))
            {
                diagnostics.Add(new DiscoveryDiagnostic("cycle", node.Identity, "Active traversal cycle detected."));
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
                            targetOverride ?? node.Target,
                            node.SourceContract,
                            path);
                    case DiscoveryNodeKind.Sequence:
                        return VisitSequence(node, depth, active, diagnostics, targetOverride, path);
                    case DiscoveryNodeKind.Conditional:
                        return new ConditionalEffectExpression(
                            node.ConditionContract,
                            Visit(node.WhenTrue, depth + 1, active, diagnostics, targetOverride, path + "/true"),
                            Visit(node.WhenFalse, depth + 1, active, diagnostics, targetOverride, path + "/false"));
                    case DiscoveryNodeKind.TargetTransform:
                        return new TargetedEffectExpression(
                            node.Target,
                            VisitSequence(node, depth, active, diagnostics, node.Target, path));
                    case DiscoveryNodeKind.AbilityReference:
                        return new ReferencedAbilityExpression(
                            node.ReferencedAbilityId,
                            VisitSequence(node, depth, active, diagnostics, targetOverride, path));
                    default:
                        diagnostics.Add(new DiscoveryDiagnostic("unknown-node", node.Identity, node.SourceContract));
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
    }

    internal sealed class ReferenceEqualityComparer<T> : IEqualityComparer<T> where T : class
    {
        internal static readonly ReferenceEqualityComparer<T> Instance = new ReferenceEqualityComparer<T>();
        public bool Equals(T x, T y) { return ReferenceEquals(x, y); }
        public int GetHashCode(T obj) { return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj); }
    }
}
