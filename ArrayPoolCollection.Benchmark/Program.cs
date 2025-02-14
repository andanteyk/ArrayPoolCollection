// See https://aka.ms/new-console-template for more information
using ArrayPoolCollection.Benchmark;
using ArrayPoolCollection.Benchmark.Experiment;
using BenchmarkDotNet.Running;

BenchmarkRunner.Run<ArrayPoolWrapperBenchmark>(args: Environment.GetCommandLineArgs());
BenchmarkRunner.Run<ArrayPoolListBenchmark>(args: Environment.GetCommandLineArgs());
BenchmarkRunner.Run<ArrayPoolDictionaryBenchmark>(args: Environment.GetCommandLineArgs());
BenchmarkRunner.Run<ArrayPoolHashSetBenchmark>(args: Environment.GetCommandLineArgs());
BenchmarkRunner.Run<ArrayPoolStackBenchmark>(args: Environment.GetCommandLineArgs());
BenchmarkRunner.Run<ArrayPoolQueueBenchmark>(args: Environment.GetCommandLineArgs());
BenchmarkRunner.Run<ArrayPoolPriorityQueueBenchmark>(args: Environment.GetCommandLineArgs());
BenchmarkRunner.Run<ArrayPoolBitsBenchmark>(args: Environment.GetCommandLineArgs());
BenchmarkRunner.Run<BufferWriter>(args: Environment.GetCommandLineArgs());
