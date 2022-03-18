﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.

namespace Asp.Versioning.Conventions;

public partial class OriginalControllerNameConventionTest
{
    [Fact]
    public void normalize_name_should_trim_suffix()
    {
        // arrange
        var convention = new OriginalControllerNameConvention();

        // act
        var name = convention.NormalizeName( "ValuesController" );

        // assert
        name.Should().Be( "Values" );
    }
}