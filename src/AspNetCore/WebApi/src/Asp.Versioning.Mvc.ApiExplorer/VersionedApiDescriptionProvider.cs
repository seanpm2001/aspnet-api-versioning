﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.

namespace Asp.Versioning.ApiExplorer;

using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;
using static Asp.Versioning.ApiVersionMapping;
using static System.Globalization.CultureInfo;

/// <summary>
/// Represents an API explorer that provides <see cref="ApiDescription">API descriptions</see> for actions represented by
/// <see cref="ControllerActionDescriptor">controller action descriptors</see> that are <see cref="ApiVersion">API version</see> aware.
/// </summary>
[CLSCompliant( false )]
public class VersionedApiDescriptionProvider : IApiDescriptionProvider
{
    private readonly IOptions<ApiExplorerOptions> options;
    private ApiVersionModelMetadata? modelMetadata;

    /// <summary>
    /// Initializes a new instance of the <see cref="VersionedApiDescriptionProvider"/> class.
    /// </summary>
    /// <param name="sunsetPolicyManager">The <see cref="ISunsetPolicyManager">manager</see> used to resolve sunset policies.</param>
    /// <param name="modelMetadataProvider">The <see cref="IModelMetadataProvider">provider</see> used to retrieve model metadata.</param>
    /// <param name="options">The <see cref="IOptions{TOptions}">container</see> of configured
    /// <see cref="ApiExplorerOptions">API explorer options</see>.</param>
    public VersionedApiDescriptionProvider(
        ISunsetPolicyManager sunsetPolicyManager,
        IModelMetadataProvider modelMetadataProvider,
        IOptions<ApiExplorerOptions> options )
    {
        SunsetPolicyManager = sunsetPolicyManager ?? throw new ArgumentNullException( nameof( sunsetPolicyManager ) );
        ModelMetadataProvider = modelMetadataProvider ?? throw new ArgumentNullException( nameof( modelMetadataProvider ) );
        this.options = options ?? throw new ArgumentNullException( nameof( options ) );
    }

    /// <summary>
    /// Gets or sets the order precedence of the current API description provider.
    /// </summary>
    /// <value>The order precedence of the current API description provider. The default value is 0.</value>
    public int Order { get; protected set; }

    /// <summary>
    /// Gets the manager used to resolve sunset policies.
    /// </summary>
    /// <value>The associated <see cref="ISunsetPolicyManager">sunset policy manager</see>.</value>
    protected ISunsetPolicyManager SunsetPolicyManager { get; }

    /// <summary>
    /// Gets the options associated with the API explorer.
    /// </summary>
    /// <value>The current <see cref="ApiExplorerOptions">API explorer options</see>.</value>
    protected ApiExplorerOptions Options => options.Value;

    /// <summary>
    /// Gets the model metadata provider associated with the API description provider.
    /// </summary>
    /// <value>The <see cref="IModelMetadataProvider">provider</see> used to retrieve model metadata.</value>
    protected IModelMetadataProvider ModelMetadataProvider { get; }

    private ApiVersionModelMetadata ModelMetadata =>
        modelMetadata ??= new( ModelMetadataProvider, Options.DefaultApiVersionParameterDescription );

    /// <summary>
    /// Determines whether the specified action should be explored for the indicated API version.
    /// </summary>
    /// <param name="actionDescriptor">The <see cref="ActionDescriptor">action</see> to evaluate.</param>
    /// <param name="apiVersion">The <see cref="ApiVersion">API version</see> for action being explored.</param>
    /// <returns>True if the action should be explored; otherwise, false.</returns>
    protected virtual bool ShouldExploreAction( ActionDescriptor actionDescriptor, ApiVersion apiVersion ) =>
        actionDescriptor.GetApiVersionMetadata().IsMappedTo( apiVersion );

    /// <summary>
    /// Populates the API version parameters for the specified API description.
    /// </summary>
    /// <param name="apiDescription">The <see cref="ApiDescription">API description</see> to populate parameters for.</param>
    /// <param name="apiVersion">The <see cref="ApiVersion">API version</see> used to populate parameters with.</param>
    protected virtual void PopulateApiVersionParameters( ApiDescription apiDescription, ApiVersion apiVersion )
    {
        var parameterSource = Options.ApiVersionParameterSource;
        var context = new ApiVersionParameterDescriptionContext( apiDescription, apiVersion, ModelMetadata, Options );

        parameterSource.AddParameters( context );
    }

    /// <summary>
    /// Occurs after the providers have been executed.
    /// </summary>
    /// <param name="context">The current <see cref="ApiDescriptionProviderContext">execution context</see>.</param>
    /// <remarks>The default implementation performs no action.</remarks>
    public virtual void OnProvidersExecuted( ApiDescriptionProviderContext context )
    {
        if ( context == null )
        {
            throw new ArgumentNullException( nameof( context ) );
        }

        var results = context.Results;

        if ( results.Count == 0 )
        {
            return;
        }

        var groupResults = new List<ApiDescription>( capacity: results.Count );

        foreach ( var version in FlattenApiVersions( results ) )
        {
            var groupName = version.ToString( Options.GroupNameFormat, CurrentCulture );

            for ( var i = 0; i < results.Count; i++ )
            {
                var result = results[i];
                var action = result.ActionDescriptor;

                if ( !ShouldExploreAction( action, version ) )
                {
                    continue;
                }

                var groupResult = result.Clone();
                var metadata = action.GetApiVersionMetadata();

                if ( string.IsNullOrEmpty( groupResult.GroupName ) )
                {
                    groupResult.GroupName = groupName;
                }

                if ( SunsetPolicyManager.TryResolvePolicy( metadata.Name, version, out var policy ) )
                {
                    groupResult.SetSunsetPolicy( policy );
                }

                groupResult.SetApiVersion( version );
                PopulateApiVersionParameters( groupResult, version );
                groupResult.TryUpdateRelativePathAndRemoveApiVersionParameter( Options );
                groupResults.Add( groupResult );
            }
        }

        results.Clear();

        for ( var i = 0; i < groupResults.Count; i++ )
        {
            results.Add( groupResults[i] );
        }
    }

    /// <summary>
    /// Occurs when the providers are being executed.
    /// </summary>
    /// <param name="context">The current <see cref="ApiDescriptionProviderContext">execution context</see>.</param>
    /// <remarks>The default implementation performs no operation.</remarks>
    public virtual void OnProvidersExecuting( ApiDescriptionProviderContext context ) { }

    private IEnumerable<ApiVersion> FlattenApiVersions( IList<ApiDescription> descriptions )
    {
        var versions = default( SortedSet<ApiVersion> );

        for ( var i = 0; i < descriptions.Count; i++ )
        {
            var action = descriptions[i].ActionDescriptor;
            var model = action.GetApiVersionMetadata().Map( Explicit | Implicit );
            var declaredVersions = model.DeclaredApiVersions;

            if ( versions is null && declaredVersions.Count > 0 )
            {
                versions = new();
            }

            for ( var j = 0; j < declaredVersions.Count; j++ )
            {
                versions!.Add( declaredVersions[j] );
            }
        }

        if ( versions is null )
        {
            return new[] { Options.DefaultApiVersion };
        }

        return versions;
    }
}