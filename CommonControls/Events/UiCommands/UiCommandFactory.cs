﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.Extensions.DependencyInjection;

namespace CommonControls.Events.UiCommands
{
    public interface IUiCommand
    {
    }

    public interface IUiCommandFactory
    {
        T Create<T>(Action<T> configure = null) where T : IUiCommand;
    }

    public class UiCommandFactory : IUiCommandFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public UiCommandFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public T Create<T>(Action<T> configure = null) where T : IUiCommand
        {
            var instance = _serviceProvider.GetRequiredService<T>();
            if (configure != null)
                configure(instance);
            return instance;
        }
    }
}