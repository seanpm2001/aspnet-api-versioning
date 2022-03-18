﻿namespace ApiVersioning.Examples;

using ApiVersioning.Examples.Controllers;
using Asp.Versioning.Conventions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.OData;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public class Startup
{
    public Startup( IConfiguration configuration ) => Configuration = configuration;

    public IConfiguration Configuration { get; }

    public void ConfigureServices( IServiceCollection services )
    {
        services.AddControllers().AddOData();
        services.AddApiVersioning(
                    options =>
                    {
                        // reporting api versions will return the headers
                        // "api-supported-versions" and "api-deprecated-versions"
                        options.ReportApiVersions = true;
                    } )
                .AddMvc(
                    options =>
                    {
                        // apply api versions using conventions rather than attributes
                        options.Conventions.Controller<OrdersController>()
                                           .HasApiVersion( 1, 0 );

                        options.Conventions.Controller<PeopleController>()
                                           .HasApiVersion( 1, 0 )
                                           .HasApiVersion( 2, 0 )
                                           .Action( c => c.Patch( default, default, default ) ).MapToApiVersion( 2, 0 );

                        options.Conventions.Controller<People2Controller>()
                                           .HasApiVersion( 3, 0 );
                    } )
                .AddOData(
                    options =>
                    {
                        // INFO: you do NOT and should NOT use both the query string and url segment methods together.
                        // this configuration is merely illustrating that they can coexist and allows you to easily
                        // experiment with either configuration. one of these would be removed in a real application.

                        // WHEN VERSIONING BY: query string, header, or media type
                        options.AddRouteComponents( "api" );

                        // WHEN VERSIONING BY: url segment
                        options.AddRouteComponents( "api/v{version:apiVersion}" );
                    } );
    }

    public void Configure( IApplicationBuilder app )
    {
        app.UseRouting();
        app.UseEndpoints( endpoints => endpoints.MapControllers() );
    }
}