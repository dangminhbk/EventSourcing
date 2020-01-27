﻿using JKang.EventSourcing.Domain;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class EventSourcingOptionsServiceCollectionExtensions
    {
        /// <summary>
        /// Configure options for an aggregate type
        /// </summary>
        public static IServiceCollection ConfigureAggregate<TAggregate, TAggregateKey, TAggregateOptions>(
            this IServiceCollection services,
            Action<TAggregateOptions> configureOptions)
            where TAggregate : IAggregate<TAggregateKey>
            where TAggregateOptions : class
            => services.Configure(typeof(TAggregate).FullName, configureOptions);
    }
}
