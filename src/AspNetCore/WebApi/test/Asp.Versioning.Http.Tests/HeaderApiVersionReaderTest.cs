﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.

namespace Asp.Versioning;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

public class HeaderApiVersionReaderTest
{
    [Theory]
    [InlineData( "api-version", "2.1" )]
    [InlineData( "x-ms-version", "2016-07-09" )]
    public void read_should_retrieve_version_from_header( string headerName, string requestedVersion )
    {
        // arrange
        var headers = new HeaderDictionary() { [headerName] = requestedVersion };
        var request = new Mock<HttpRequest>();
        var reader = new HeaderApiVersionReader() { HeaderNames = { "api-version", "x-ms-version" } };

        request.SetupGet( r => r.Headers ).Returns( headers );

        // act
        var versions = reader.Read( request.Object );

        // assert
        versions.Single().Should().Be( requestedVersion );
    }

    [Fact]
    public void read_should_return_ambiguous_api_versions()
    {
        // arrange
        var headers = new HeaderDictionary() { ["api-version"] = new StringValues( new[] { "1.0", "2.0" } ) };
        var request = new Mock<HttpRequest>();
        var reader = new HeaderApiVersionReader() { HeaderNames = { "api-version" } };

        request.SetupGet( r => r.Headers ).Returns( headers );

        // act
        var versions = reader.Read( request.Object );

        // assert
        versions.Should().BeEquivalentTo( "1.0", "2.0" );
    }

    [Fact]
    public void read_should_not_throw_exception_when_duplicate_api_versions_are_requested()
    {
        // arrange
        var headers = new HeaderDictionary()
        {
            ["api-version"] = "1.0",
            ["x-ms-version"] = "1.0",
        };
        var request = new Mock<HttpRequest>();
        var reader = new HeaderApiVersionReader() { HeaderNames = { "api-version", "x-ms-version" } };

        request.SetupGet( r => r.Headers ).Returns( headers );

        // act
        var versions = reader.Read( request.Object );

        // assert
        versions.Single().Should().Be( "1.0" );
    }
}