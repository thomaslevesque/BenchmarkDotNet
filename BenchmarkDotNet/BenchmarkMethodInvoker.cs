﻿using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Tasks;

namespace BenchmarkDotNet
{
    // TODO: remove duplications
    public class BenchmarkMethodInvoker
    {
        private const long InvokeTimoutMilliseconds = 1000; // TODO: Move to settings

        private class Measurement
        {
            public long OperationCount { get; }
            public long Ticks { get; }
            public double Nanoseconds { get; }
            public double Milliseconds { get; }
            public double Seconds { get; }
            public double NanosecondsPerOperation { get; }
            public double OperationsPerSecond { get; }

            public Measurement(long operationCount, long ticks)
            {
                OperationCount = operationCount;
                Ticks = Math.Max(ticks, 1);
                Nanoseconds = (Ticks / (double)Stopwatch.Frequency) * 1000000000;
                Milliseconds = Nanoseconds / 1000000;
                Seconds = Nanoseconds / 1000000000;
                NanosecondsPerOperation = Nanoseconds / OperationCount;
                OperationsPerSecond = OperationCount / Seconds;
            }

            public string GetDisplayValue()
            {
                return string.Format(EnvironmentHelper.MainCultureInfo,
                    "{0} op, {1:0.#} ms, {2:0.#} ns, {3} ticks, {4:0.####} ns/op, {5:0.#} op/s",
                    OperationCount, Milliseconds, Nanoseconds, Ticks, NanosecondsPerOperation, OperationsPerSecond);
            }
        }

        public void Throughput(BenchmarkSettings settings, long operationsPerInvoke, Action setupAction, Action targetAction, Action idleAction)
        {
            setupAction();
            targetAction();
            idleAction();

            long invokeCount = 1;
            double lastPreWarmupMilliseconds = 0;
            while (true)
            {
                var measurement = MultiInvoke("// Pre-Warmup", setupAction, targetAction, invokeCount, operationsPerInvoke);
                lastPreWarmupMilliseconds = measurement.Milliseconds;
                if (lastPreWarmupMilliseconds > InvokeTimoutMilliseconds)
                    break;
                if (lastPreWarmupMilliseconds < 1)
                    invokeCount *= InvokeTimoutMilliseconds;
                else
                    invokeCount *= (long)Math.Ceiling(InvokeTimoutMilliseconds / lastPreWarmupMilliseconds);
            }
            double idleMilliseconds = 0;
            for (int i = 0; i < Math.Min(3, settings.WarmupIterationCount); i++)
            {
                var measurement = MultiInvoke("// Warmup (idle)", setupAction, idleAction, invokeCount, operationsPerInvoke);
                idleMilliseconds = measurement.Milliseconds;
            }
            invokeCount = invokeCount * 1000 / (long)Math.Round(Math.Min(1000, Math.Max(100, lastPreWarmupMilliseconds - idleMilliseconds)));
            Console.WriteLine("// IterationCount = " + invokeCount);
            long idleTicks = 0;
            var targetIdleInvokeCount = Math.Min(5, settings.TargetIterationCount);
            for (int i = 0; i < targetIdleInvokeCount; i++)
            {
                var measurement = MultiInvoke("// Target (idle)", setupAction, idleAction, invokeCount, operationsPerInvoke);
                idleTicks += measurement.Ticks;
            }
            idleTicks /= targetIdleInvokeCount;

            for (int i = 0; i < settings.WarmupIterationCount; i++)
                MultiInvoke("// Warmup " + (i + 1), setupAction, targetAction, invokeCount, operationsPerInvoke, idleTicks);
            for (int i = 0; i < settings.TargetIterationCount; i++)
                MultiInvoke("Target " + (i + 1), setupAction, targetAction, invokeCount, operationsPerInvoke, idleTicks);
        }

        public void Throughput<T>(BenchmarkSettings settings, long operationsPerInvoke, Action setupAction, Func<T> targetAction, Func<T> idleAction)
        {
            setupAction();
            targetAction();
            idleAction();

            long invokeCount = 1;
            double lastPreWarmupMilliseconds = 0;
            while (true)
            {
                var measurement = MultiInvoke("// Pre-Warmup", setupAction, targetAction, invokeCount, operationsPerInvoke);
                lastPreWarmupMilliseconds = measurement.Milliseconds;
                if (lastPreWarmupMilliseconds > InvokeTimoutMilliseconds)
                    break;
                if (lastPreWarmupMilliseconds < 1)
                    invokeCount *= InvokeTimoutMilliseconds;
                else
                    invokeCount *= (long)Math.Ceiling(InvokeTimoutMilliseconds / lastPreWarmupMilliseconds);
            }
            double idleMilliseconds = 0;
            for (int i = 0; i < Math.Min(3, settings.WarmupIterationCount); i++)
            {
                var measurement = MultiInvoke("// Warmup (idle)", setupAction, idleAction, invokeCount, operationsPerInvoke);
                idleMilliseconds = measurement.Milliseconds;
            }
            invokeCount = invokeCount * 1000 / (long)Math.Round(Math.Min(1000, Math.Max(100, lastPreWarmupMilliseconds - idleMilliseconds)));
            Console.WriteLine("// IterationCount = " + invokeCount);
            long idleTicks = 0;
            var targetIdleInvokeCount = Math.Min(5, settings.TargetIterationCount);
            for (int i = 0; i < targetIdleInvokeCount; i++)
            {
                var measurement = MultiInvoke("// Target (idle)", setupAction, idleAction, invokeCount, operationsPerInvoke);
                idleTicks += measurement.Ticks;
            }
            idleTicks /= targetIdleInvokeCount;

            for (int i = 0; i < settings.WarmupIterationCount; i++)
                MultiInvoke("// Warmup " + (i + 1), setupAction, targetAction, invokeCount, operationsPerInvoke, idleTicks);
            for (int i = 0; i < settings.TargetIterationCount; i++)
                MultiInvoke("Target " + (i + 1), setupAction, targetAction, invokeCount, operationsPerInvoke, idleTicks);
        }


        public void SingleRun(BenchmarkSettings settings, long operationsPerInvoke, Action setupAction, Action targetAction, Action idleAction)
        {
            for (int i = 0; i < settings.WarmupIterationCount; i++)
                MultiInvoke("// Warmup " + (i + 1), setupAction, targetAction, 1, operationsPerInvoke);
            for (int i = 0; i < settings.TargetIterationCount; i++)
                MultiInvoke("Target " + (i + 1), setupAction, targetAction, 1, operationsPerInvoke);
        }

        private Measurement MultiInvoke(string name, Action setupAction, Action targetAction, long invocationCount, long operationsPerInvoke, long idleTicks = 0)
        {
            var totalOperations = invocationCount * operationsPerInvoke;
            setupAction();
            var stopwatch = new Stopwatch();
            if (invocationCount == 1)
            {
                stopwatch.Start();
                targetAction();
                stopwatch.Stop();
            }
            else if (invocationCount < int.MaxValue)
            {
                int intInvocationCount = (int)invocationCount;
                stopwatch.Start();
                for (int i = 0; i < intInvocationCount; i++)
                    targetAction();
                stopwatch.Stop();
            }
            else
            {
                stopwatch.Start();
                for (long i = 0; i < invocationCount; i++)
                    targetAction();
                stopwatch.Stop();
            }
            var measurement = new Measurement(totalOperations, stopwatch.ElapsedTicks - idleTicks);
            Console.WriteLine($"{name}: {measurement.GetDisplayValue()}");
            GcCollect();
            return measurement;
        }

        private object multiInvokeReturnHolder;

        private Measurement MultiInvoke<T>(string name, Action setupAction, Func<T> targetAction, long invocationCount, long operationsPerInvoke, long idleTicks = 0, T returnHolder = default(T))
        {
            var totalOperations = invocationCount * operationsPerInvoke;
            setupAction();
            var stopwatch = new Stopwatch();
            if (invocationCount == 1)
            {
                stopwatch.Start();
                returnHolder = targetAction();
                stopwatch.Stop();
            }
            else if (invocationCount < int.MaxValue)
            {
                int intInvocationCount = (int)invocationCount;
                stopwatch.Start();
                for (int i = 0; i < intInvocationCount; i++)
                    returnHolder = targetAction();
                stopwatch.Stop();
            }
            else
            {
                stopwatch.Start();
                for (long i = 0; i < invocationCount; i++)
                    returnHolder = targetAction();
                stopwatch.Stop();
            }
            multiInvokeReturnHolder = returnHolder;
            var measurement = new Measurement(totalOperations, stopwatch.ElapsedTicks - idleTicks);
            Console.WriteLine($"{name}: {measurement.GetDisplayValue()}");
            GcCollect();
            return measurement;
        }

        private static void GcCollect()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
    }
}