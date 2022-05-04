﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.

namespace Microsoft.Extensions.DependencyInjection;

using Asp.Versioning;
using Asp.Versioning.ApplicationModels;
using Asp.Versioning.OData;
using Asp.Versioning.Routing;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Routing.Template;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Runtime.CompilerServices;
using static Asp.Versioning.OData.ODataMultiModelApplicationModelProvider;
using static Microsoft.Extensions.DependencyInjection.ServiceDescriptor;

/// <summary>
/// Provides ASP.NET Core OData specific extension methods for <see cref="IApiVersioningBuilder"/>.
/// </summary>
#if NETCOREAPP3_1
[CLSCompliant( false )]
#endif
public static class IApiVersioningBuilderExtensions
{
    /// <summary>
    /// Adds ASP.NET Core OData support for API versioning.
    /// </summary>
    /// <param name="builder">The extended <see cref="IApiVersioningBuilder">API versioning builder</see>.</param>
    /// <returns>The original <paramref name="builder"/>.</returns>
    public static IApiVersioningBuilder AddOData( this IApiVersioningBuilder builder )
    {
        if ( builder == null )
        {
            throw new ArgumentNullException( nameof( builder ) );
        }

        AddServices( builder.AddMvc().Services );
        return builder;
    }

    /// <summary>
    /// Adds ASP.NET Core OData support for API versioning.
    /// </summary>
    /// <param name="builder">The extended <see cref="IApiVersioningBuilder">API versioning builder</see>.</param>
    /// <param name="setupAction">An <see cref="Action{T}">action</see> used to configure the provided options.</param>
    /// <returns>The original <paramref name="builder"/>.</returns>
    [CLSCompliant( false )]
    public static IApiVersioningBuilder AddOData( this IApiVersioningBuilder builder, Action<ODataApiVersioningOptions> setupAction )
    {
        if ( builder == null )
        {
            throw new ArgumentNullException( nameof( builder ) );
        }

        var services = builder.AddMvc().Services;
        AddServices( services );
        services.Configure( setupAction );
        return builder;
    }

    private static void AddServices( IServiceCollection services )
    {
        services.TryRemoveODataService( typeof( IApplicationModelProvider ), ODataRoutingApplicationModelProviderType );

        var partManager = services.GetOrCreateApplicationPartManager();

        ConfigureDefaultFeatureProviders( partManager );

        services.AddHttpContextAccessor();
        services.TryAddSingleton<VersionedODataOptions>();
        services.TryReplaceODataService(
            Singleton<IODataTemplateTranslator, VersionedODataTemplateTranslator>(),
            "Microsoft.AspNetCore.OData.Routing.Template.DefaultODataTemplateTranslator" );
        services.Replace( Singleton<IOptions<ODataOptions>>( sp => sp.GetRequiredService<VersionedODataOptions>() ) );
        services.TryAddTransient<VersionedODataModelBuilder>();
        services.TryAddSingleton<IOptionsFactory<ODataApiVersioningOptions>, ODataApiVersioningOptionsFactory>();
        services.TryAddSingleton<IODataApiVersionCollectionProvider, ODataApiVersionCollectionProvider>();
        services.TryAddEnumerable( Transient<IApiControllerSpecification, ODataControllerSpecification>() );
        services.TryAddEnumerable( Transient<IPostConfigureOptions<ODataOptions>, ODataOptionsPostSetup>() );
        services.TryAddEnumerable( Singleton<MatcherPolicy, DefaultMetadataMatcherPolicy>() );
        services.TryAddEnumerable( Transient<IApplicationModelProvider, ODataApplicationModelProvider>() );
        services.TryAddEnumerable( Transient<IApplicationModelProvider, ODataMultiModelApplicationModelProvider>() );
        services.AddModelConfigurationsAsServices( partManager );
    }

    private static T GetService<T>( this IServiceCollection services ) =>
        (T) services.LastOrDefault( d => d.ServiceType == typeof( T ) )?.ImplementationInstance!;

    private static ApplicationPartManager GetOrCreateApplicationPartManager( this IServiceCollection services )
    {
        var partManager = services.GetService<ApplicationPartManager>();

        if ( partManager == null )
        {
            partManager = new ApplicationPartManager();
            services.TryAddSingleton( partManager );
        }

        partManager.ApplicationParts.Add( new AssemblyPart( typeof( ODataApiVersioningOptions ).Assembly ) );
        return partManager;
    }

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    private static Type GetODataType( string typeName )
    {
        var assemblyName = typeof( ODataOptions ).Assembly.GetName().Name;
        return Type.GetType( $"{typeName}, {assemblyName}", throwOnError: true, ignoreCase: false )!;
    }

    private static void TryRemoveODataService( this IServiceCollection services, Type serviceType, Type implementationType )
    {
        for ( var i = 0; i < services.Count; i++ )
        {
            var service = services[i];

            if ( service.ServiceType == serviceType && service.ImplementationType == implementationType )
            {
                services.RemoveAt( i );
                return;
            }
        }

        var message = string.Format( CultureInfo.CurrentCulture, SR.UnableToFindServices, nameof( IMvcBuilder ), "AddOData", "ConfigureServices(...)" );
        throw new InvalidOperationException( message );
    }

    private static void TryReplaceODataService(
        this IServiceCollection services,
        ServiceDescriptor replacement,
        string implementationTypeName )
    {
        var serviceType = replacement.ServiceType;
        var implementationType = GetODataType( implementationTypeName );

        for ( var i = 0; i < services.Count; i++ )
        {
            var service = services[i];

            if ( service.ServiceType == serviceType && service.ImplementationType == implementationType )
            {
                services[i] = replacement;
                break;
            }
        }
    }

    private static void AddModelConfigurationsAsServices( this IServiceCollection services, ApplicationPartManager partManager )
    {
        var feature = new ModelConfigurationFeature();
        var modelConfigurationType = typeof( IModelConfiguration );

        partManager.PopulateFeature( feature );

        foreach ( var modelConfiguration in feature.ModelConfigurations )
        {
            services.TryAddEnumerable( Transient( modelConfigurationType, modelConfiguration ) );
        }
    }

    private static void ConfigureDefaultFeatureProviders( ApplicationPartManager partManager )
    {
        if ( !partManager.FeatureProviders.OfType<ModelConfigurationFeatureProvider>().Any() )
        {
            partManager.FeatureProviders.Add( new ModelConfigurationFeatureProvider() );
        }
    }
}