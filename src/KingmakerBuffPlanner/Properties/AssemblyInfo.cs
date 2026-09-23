using System.Reflection;
using System.Runtime.InteropServices;

[assembly: AssemblyTitle("Kingmaker Buff Planner")]
[assembly: AssemblyDescription("Standalone buff discovery, planning, and execution for Pathfinder: Kingmaker")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("Kingmaker Buff Planner")]
[assembly: AssemblyCopyright("Copyright © 2026 Howie Reith")]
[assembly: ComVisible(false)]
[assembly: Guid("8f690856-fc6e-4b4d-a41b-640ae21fce04")]
[assembly: AssemblyVersion("0.2.0.0")]
[assembly: AssemblyFileVersion("0.2.0.0")]
[assembly: AssemblyInformationalVersion("0.2.0-rc1")]
// Review L5: the candidate-profile format this binary reads (install
// rollback reads it from the restored binary).
[assembly: AssemblyMetadata(
    KingmakerBuffPlanner.Persistence.CastingPlanProfile.CandidateFormatAttributeKey,
    KingmakerBuffPlanner.Persistence.CastingPlanProfile.CandidateFormatToken)]
