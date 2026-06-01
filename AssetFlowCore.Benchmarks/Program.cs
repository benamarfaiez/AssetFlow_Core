using BenchmarkDotNet.Running;

// Lance tous les benchmarks du projet
BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).RunAllJoined();