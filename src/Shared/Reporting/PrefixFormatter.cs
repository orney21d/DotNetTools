// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Tools.Internal;

namespace Microsoft.DotNet.Watcher.Tools
{
    public class PrefixFormatter : IFormatter
    {
        private readonly string _prefix;

        public PrefixFormatter(string prefix)
        {
            Ensure.NotNullOrEmpty(prefix, nameof(prefix));

            _prefix = prefix;
        }

        public string Format(string text)
            => _prefix + text;
    }
}