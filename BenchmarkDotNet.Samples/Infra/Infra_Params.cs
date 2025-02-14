﻿using System;
using System.Threading;
using BenchmarkDotNet.Tasks;

namespace BenchmarkDotNet.Samples.Infra
{
    [BenchmarkTask(1)]
    public class Infra_Params
    {
        [Params(50, 100, 150, 200)]
        public int Value = 1;

        private readonly Random random = new Random();

        [Benchmark]
        public void RunSlow()
        {
            Thread.Sleep(Value * 2);
        }

        [Benchmark]
        public void RunFast()
        {
            var offset = (random.Next(201) - 100) * 0.01;
            Thread.Sleep((int)(Value * (1 + offset)));
        }
    }
}