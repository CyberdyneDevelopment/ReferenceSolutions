using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Fdw.Validation.FastEndpoints;

namespace FdwReferenceSchedules.Endpoints.Tests;

/// <summary>
/// A request validator that FDW declares abstract runs in no host until the host derives from it, because the endpoint scan skips abstract
/// types (FDW-3572). This test fails when one has no concrete subclass in this host's assembly, so a validator added to FDW later cannot go
/// unused here.
/// </summary>
public sealed class EveryFdwRequestValidatorHasAHostSubclassTests
{
    private static bool DerivesFromEndpointValidator(Type type)
    {
        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(FdwEndpointValidator<>)) return true;
        }

        return false;
    }

    private static bool Derives(Type type, Type baseType)
    {
        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            var candidate = current.IsGenericType ? current.GetGenericTypeDefinition() : current;
            if (candidate == baseType) return true;
        }

        return false;
    }

    [Fact]
    public void EveryAbstractFdwRequestValidatorHasAConcreteSubclassInTheHost()
    {
        var fdwAssemblies = new List<Assembly>
        {
            typeof(Fdw.Services.Scheduling.Endpoints.CreateScheduleRequest).Assembly,
        };
        var hostTypes = typeof(FdwReferenceSchedules.Endpoints.Validators.ReferenceCreateScheduleRequestValidator).Assembly.GetTypes();

        var withoutSubclass = fdwAssemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.IsAbstract && !type.IsSealed && DerivesFromEndpointValidator(type))
            .Where(type => !hostTypes.Any(host => !host.IsAbstract && Derives(host, type)))
            .Select(type => type.Name)
            .ToList();

        withoutSubclass.ShouldBeEmpty();
    }
}
