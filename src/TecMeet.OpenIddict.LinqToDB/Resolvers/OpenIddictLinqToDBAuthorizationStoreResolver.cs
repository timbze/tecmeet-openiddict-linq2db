﻿/*
 * Licensed under the Apache License, Version 2.0 (http://www.apache.org/licenses/LICENSE-2.0)
 * See https://github.com/openiddict/openiddict-core for more information concerning
 * the license and the contributors participating to this project.
 */

using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenIddict.Core;
using OpenIddict.Extensions;
using TecMeet.OpenIddict.LinqToDB.Models;

namespace TecMeet.OpenIddict.LinqToDB;

/// <summary>
/// Exposes a method allowing to resolve an authorization store.
/// </summary>
public class OpenIddictLinqToDBAuthorizationStoreResolver : IOpenIddictAuthorizationStoreResolver
{
    private readonly ConcurrentDictionary<Type, Type> _cache = new();
    private readonly IOptionsMonitor<OpenIddictCoreOptions> _options;
    private readonly IServiceProvider _provider;

    public OpenIddictLinqToDBAuthorizationStoreResolver(
        IOptionsMonitor<OpenIddictCoreOptions> options,
        IServiceProvider provider)
    {
        _options = options;
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    /// <summary>
    /// Returns an authorization store compatible with the specified authorization type or throws an
    /// <see cref="InvalidOperationException"/> if no store can be built using the specified type.
    /// </summary>
    /// <typeparam name="TAuthorization">The type of the Authorization entity.</typeparam>
    /// <returns>An <see cref="IOpenIddictAuthorizationStore{TAuthorization}"/>.</returns>
    public IOpenIddictAuthorizationStore<TAuthorization> Get<TAuthorization>()
        where TAuthorization : class
    {
        var store = _provider.GetService<IOpenIddictAuthorizationStore<TAuthorization>>();
        if (store is not null)
        {
            return store;
        }

        var type = _cache.GetOrAdd(typeof(TAuthorization), key =>
        {
            var root = OpenIddictHelpers.FindGenericBaseType(key, typeof(OpenIddictLinqToDBAuthorization<>)) ??
                       throw new InvalidOperationException(SR.GetResourceString(SR.ID0256));

            return typeof(OpenIddictLinqToDBAuthorizationStore<,,,>).MakeGenericType(
                /* TAuthorization: */ key,
                /* TApplication: */ _options.CurrentValue.DefaultApplicationType!,
                /* TToken: */ _options.CurrentValue.DefaultTokenType!,
                /* TKey: */ root.GenericTypeArguments[0]);
        });

        return (IOpenIddictAuthorizationStore<TAuthorization>) _provider.GetRequiredService(type);
    }
}
