// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core.BuildEngine;
using Xenko.Core.Diagnostics;
using Xenko.Core.IO;
using Xenko.Core.Serialization;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace Xenko.Core.Assets.CompilerApp
{
    public class DoNothingCommand : Command
    {
        /// <inheritdoc/>
        public override string Title { get { return "Do nothing!"; } }

        private static int commandCounter;
        private readonly int commandId;

        protected override Task<ResultStatus> DoCommandOverride(ICommandContext commandContext)
        {
            return Task.Run(() => ResultStatus.Successful);
        }

        public static void ResetCounter()
        {
            commandCounter = 0;
        }

        public DoNothingCommand()
        {
            commandId = ++commandCounter;
        }

        public override string ToString()
        {
            return GetType().Name + " " + commandId;
        }

        protected override void ComputeParameterHash(BinarySerializationWriter writer)
        {
            base.ComputeParameterHash(writer);

            writer.Write(commandId);
        }
    }

    public class TestSession
    {
        public void RunTest(string testName, Logger logger)
        {
            foreach (MethodInfo method in typeof(TestSession).GetMethods())
            {
                if (method.GetParameters().Length == 1 && method.GetParameters()[0].ParameterType == typeof(Logger))
                {
                    if (string.Compare(method.Name, testName, StringComparison.OrdinalIgnoreCase) == 0)
                        method.Invoke(this, new object[] { logger });
                }
            }
        }

        //private static PluginResolver pluginManager;
        private static void BuildStepsRecursively(BuildEngine.Builder builder, ICollection<BuildStep> steps, int stepsPerLevel, int maxLevel, BuildStep curParent = null, int curLevel = 0)
        {
            if (curLevel == maxLevel)
                return;

            for (var i = 0; i < stepsPerLevel; ++i)
            {
                BuildStep step = builder.Root.Add(new DoNothingCommand());
                if (curParent != null)
                    BuildStep.LinkBuildSteps(curParent, step);
                BuildStepsRecursively(builder, steps, stepsPerLevel, maxLevel, step, curLevel + 1);
                steps.Add(step);
            }
        }

        public static void TestVeryLargeNumberOfEmptyCommands(Logger logger)
        {
            string appPath = VirtualFileSystem.GetAbsolutePath("/data/TestVeryLargeNumberOfEmptyCommands");
            string dbPath = appPath + "/TestVeryLargeNumberOfEmptyCommands";

            if (Directory.Exists(dbPath))
                Directory.Delete(dbPath, true);

            Directory.CreateDirectory(dbPath);
            VirtualFileSystem.MountFileSystem(VirtualFileSystem.ApplicationDatabasePath, dbPath);

            logger.ActivateLog(LogMessageType.Debug);
            var builder = new Builder(logger, appPath, "index") { BuilderName = "TestBuilder" };
            var steps = new List<BuildStep>();
            const int StepsPerLevel = 5;
            const int MaxLevel = 5;

            BuildStepsRecursively(builder, steps, StepsPerLevel, MaxLevel);
            int stepCount = 0;
            for (var i = 0; i < MaxLevel; ++i)
            {
                stepCount += (int)Math.Pow(StepsPerLevel, i + 1);
            }
            Debug.Assert(steps.Count == stepCount);

            logger.Info(stepCount + " steps registered.");
            logger.Info("Starting builder (logger disabled)");
            logger.ActivateLog(LogMessageType.Fatal);
            builder.Run(Builder.Mode.Build);
            logger.ActivateLog(LogMessageType.Debug);
            logger.Info("Build finished (logger re-enabled)");

            foreach (BuildStep step in steps)
            {
                Debug.Assert(step.Status == ResultStatus.Successful);
            }
        }

    }
}
