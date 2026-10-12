using System.Linq;
using Fdw.Services.Data.Clients.Models;
using Fdw.Services.Data.Endpoints;
using FdwReferenceDataSets.Endpoints.Validators;
using Shouldly;
using Xunit;

namespace FdwReferenceData.Endpoints.Tests;

/// <summary>FDW-1102: there is no default for isFilterable, so a request that carries fields states it on each.</summary>
public sealed class DataSetFieldFilterableValidatorTests
{
    [Fact]
    public void ACreateWhoseFieldDoesNotStateIsFilterableIsRefusedNamingTheField()
    {
        var request = new CreateDataSetRequest
        {
            Name = "Teams",
            Version = "1.0.0",
            Fields = [new CreateDataSetFieldRequest { Name = "City", DataType = "String" }],
        };

        var result = new CreateDataSetRequestValidator().Validate(request);

        result.IsValid.ShouldBeFalse();
        result.Errors.Select(e => e.ErrorMessage).ShouldContain("Field 'City' must state isFilterable (true or false); there is no default");
    }

    [Fact]
    public void ACreateWhoseFieldsStateIsFilterableTrueOrFalseIsAccepted()
    {
        var request = new CreateDataSetRequest
        {
            Name = "Teams",
            Version = "1.0.0",
            Fields =
            [
                new CreateDataSetFieldRequest { Name = "City", DataType = "String", IsFilterable = true },
                new CreateDataSetFieldRequest { Name = "Secret", DataType = "String", IsFilterable = false },
            ],
        };

        new CreateDataSetRequestValidator().Validate(request).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void AnUpdateThatReplacesFieldsMustStateIsFilterableOnEachButOneThatSendsNoFieldsNeedsNone()
    {
        var withoutState = new UpdateDataSetRequest { Name = "Teams", Fields = [new CreateDataSetFieldRequest { Name = "City", DataType = "String" }] };
        var withoutFields = new UpdateDataSetRequest { Name = "Teams" };

        var validator = new UpdateDataSetRequestValidator();

        validator.Validate(withoutState).IsValid.ShouldBeFalse();
        validator.Validate(withoutFields).IsValid.ShouldBeTrue();
    }
}
