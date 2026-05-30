using BenchmarkDotNet.Running;
using AssetFlowCore.Benchmarks;

// Lance tous les benchmarks du projet
BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).RunAllJoined();