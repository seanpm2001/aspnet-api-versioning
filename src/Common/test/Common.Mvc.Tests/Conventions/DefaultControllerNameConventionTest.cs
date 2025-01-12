﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.

namespace Asp.Versioning.Conventions;

public partial class DefaultControllerNameConventionTest
{
    [Theory]
    [MemberData(nameof(NormalizeNameData))]
    public void normalize_name_should_trim_suffix( string controllerName )
    {
        // arrange
        var convention = new DefaultControllerNameConvention();

        // act
        var name = convention.NormalizeName( controllerName );

        // assert
        name.Should().Be( "Values" );
    }

    [Fact]
    public void group_name_should_return_original_name()
    {
        // arrange
        var convention = new DefaultControllerNameConvention();

        // act
        var name = convention.GroupName( "Values" );

        // assert
        name.Should().Be( "Values" );
    }

    public static IEnumerable<object[]> NormalizeNameData
    {
        get
        {
            return new[]
            {
#if NETFRAMEWORK
                new object[] { "ValuesController" },
                new object[] { "Values2Controller" },
#else
                new object[] { "Values" },
                new object[] { "Values2" },
#endif
            };
        }
    }
}