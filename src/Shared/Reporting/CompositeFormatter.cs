﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Tools.Internal;

namespace Microsoft.DotNet.Watcher.Tools
{
    public class CompositeFormatter : IFormatter
    {
        private readonly IFormatter[] _formatters;

        public CompositeFormatter(IFormatter[] formatters)
        {
            Ensure.NotNull(formatters, nameof(formatters));
            _formatters = formatters;
        }

        public string Format(string text)
        {
            foreach (var formatter in _formatters)
            {
                text = formatter.Format(text);
            }

            return text;
        }
    }
}