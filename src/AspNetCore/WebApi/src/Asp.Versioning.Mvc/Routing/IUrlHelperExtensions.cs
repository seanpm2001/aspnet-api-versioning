﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.

namespace Microsoft.AspNetCore.Mvc;

using Asp.Versioning;
using Asp.Versioning.Routing;

/// <summary>
/// Provides extension methods for the <see cref="IUrlHelper"/> interface.
/// </summary>
[CLSCompliant( false )]
public static class IUrlHelperExtensions
{
    /// <summary>
    /// Returns a new URL helper that includes the requested API version.
    /// </summary>
    /// <param name="urlHelper">The extended <see cref="IUrlHelper">URL helper</see>.</param>
    /// <returns>A new <see cref="IUrlHelper">URL helper</see> that excludes the requested
    /// API version or the original <paramref name="urlHelper">URL helper</paramref> if
    /// unnecessary.</returns>
    /// <remarks>Excluding the requested API version is useful in a limited set of scenarios
    /// such as building a URL from an API that versions by URL segment to an API that is
    /// version-neutral. A version-neutral API would not use the specified route value and
    /// it would be erroneously added as a query string parameter.</remarks>
    public static IUrlHelper WithoutApiVersion( this IUrlHelper urlHelper )
    {
        if ( urlHelper == null )
        {
            throw new ArgumentNullException( nameof( urlHelper ) );
        }

        if ( urlHelper is WithoutApiVersionUrlHelper ||
             urlHelper.ActionContext.HttpContext.Features.Get<IApiVersioningFeature>() is null )
        {
            return urlHelper;
        }

        return new WithoutApiVersionUrlHelper( urlHelper );
    }
}