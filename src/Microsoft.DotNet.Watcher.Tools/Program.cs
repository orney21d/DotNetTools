// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Watcher.Internal;
using Microsoft.DotNet.Watcher.Tools;
using Microsoft.Extensions.Tools.Internal;

namespace Microsoft.DotNet.Watcher
{
    public class Program
    {
        private readonly IConsole _console;
        private readonly string _workingDir;
        private IReporter _reporter;

        public Program(IConsole console, string workingDir)
        {
            Ensure.NotNull(console, nameof(console));
            Ensure.NotNullOrEmpty(workingDir, nameof(workingDir));

            _console = console;
            _workingDir = workingDir;
        }

        public static int Main(string[] args)
        {
            DebugHelper.HandleDebugSwitch(ref args);
            return new Program(PhysicalConsole.Singleton, Directory.GetCurrentDirectory())
                .RunAsync(args)
                .GetAwaiter()
                .GetResult();
        }

        public async Task<int> RunAsync(string[] args)
        {
            var options = CommandLineOptions.Parse(args, _console);
            if (options == null)
            {
                // invalid args syntax
                return 1;
            }

            if (options.IsHelp)
            {
                return 2;
            }

            InitializeReporter(options);

            using (CancellationTokenSource ctrlCTokenSource = new CancellationTokenSource())
            {
                _console.CancelKeyPress += (sender, ev) =>
                {
                    if (!ctrlCTokenSource.IsCancellationRequested)
                    {
                        _reporter.Output("Shutdown requested. Press Ctrl+C again to force exit.");
                        ev.Cancel = true;
                    }
                    else
                    {
                        ev.Cancel = false;
                    }
                    ctrlCTokenSource.Cancel();
                };

                try
                {
                    return await MainInternalAsync(options.Project, options.RemainingArguments, ctrlCTokenSource.Token);
                }
                catch (Exception ex)
                {
                    if (ex is TaskCanceledException || ex is OperationCanceledException)
                    {
                        // swallow when only exception is the CTRL+C forced an exit
                        return 0;
                    }

                    _reporter.Error(ex.ToString());
                    _reporter.Error("An unexpected error occurred");
                    return 1;
                }
            }
        }

        private void InitializeReporter(CommandLineOptions options)
        {
            var prefix = "watch : ";
            var colorPrefix = $"{ColorFormatter.GetAnsiCode(ConsoleColor.DarkGray)}{prefix}{ColorFormatter.ResetCode}";

            _reporter = new ReporterBuilder()
                .WithConsole(_console)
                .Verbose(f =>
                {
                    if (_console.IsOutputRedirected)
                    {
                        f.WithPrefix(prefix);
                    }
                    else
                    {
                        f.WithColor(ConsoleColor.DarkGray).WithPrefix(colorPrefix);
                    }

                    f.When(() => options.IsVerbose || CliContext.IsGlobalVerbose());
                })
                .Output(f => f
                    .WithPrefix(_console.IsOutputRedirected ? prefix : colorPrefix)
                    .When(() => !options.IsQuiet))
                .Warn(f =>
                {
                    if (_console.IsOutputRedirected)
                    {
                        f.WithPrefix(prefix);
                    }
                    else
                    {
                        f.WithColor(ConsoleColor.Yellow).WithPrefix(colorPrefix);
                    }
                })
                .Error(f =>
                {
                    if (_console.IsOutputRedirected)
                    {
                        f.WithPrefix(prefix);
                    }
                    else
                    {
                        f.WithColor(ConsoleColor.Red).WithPrefix(colorPrefix);
                    }
                })
                .Build();
        }

        private async Task<int> MainInternalAsync(
            string project,
            ICollection<string> args,
            CancellationToken cancellationToken)
        {
            // TODO multiple projects should be easy enough to add here
            string projectFile;
            try
            {
                projectFile = MsBuildProjectFinder.FindMsBuildProject(_workingDir, project);
            }
            catch (FileNotFoundException ex)
            {
                _reporter.Error(ex.Message);
                return 1;
            }

            var fileSetFactory = new MsBuildFileSetFactory(_reporter, projectFile);

            var processInfo = new ProcessSpec
            {
                Executable = DotNetMuxer.MuxerPathOrDefault(),
                WorkingDirectory = Path.GetDirectoryName(projectFile),
                Arguments = args
            };

            await new DotNetWatcher(_reporter)
                .WatchAsync(processInfo, fileSetFactory, cancellationToken);

            return 0;
        }
    }
}