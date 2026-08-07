using Fdw.Data.Sqlite;
using System;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Moq;
using Shouldly;
using Xunit;

namespace ReferenceConnections.Sqlite.Tests.Translators;

/// <summary>
/// Security-focused coverage for <see cref="SqliteFindTranslator"/>'s column-name validation
/// guard and LIKE/GLOB wildcard-escaping branches.
/// </summary>
[Collection(nameof(SqliteTestCollection))]
public sealed class SqliteFindTranslatorTests
{
    private readonly SqliteFindTranslator _sut = new();

    private static Mock<IStorageContainer> CreateContainer(
        string tableName = "customers",
        IField[]? fields = null)
    {
        var dbPath = new SqliteDatabasePath(tableName);

        var schema = new Mock<IContainerSchema>();
        schema.Setup(s => s.Fields).Returns(fields ?? Array.Empty<IField>());

        var container = new Mock<IStorageContainer>();
        container.Setup(c => c.Name).Returns(tableName);
        container.Setup(c => c.Path).Returns(dbPath);
        container.Setup(c => c.Schema).Returns(schema.Object);

        return container;
    }

    private static IField CreateField(string name, Type? clrType = null)
    {
        var fieldType = new Mock<IFieldType>();
        fieldType.Setup(ft => ft.ClrType).Returns(clrType ?? typeof(string));

        var field = new Mock<IField>();
        field.Setup(f => f.Name).Returns(name);
        field.Setup(f => f.FieldType).Returns(fieldType.Object);
        return field.Object;
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public void ConstructorSetsName()
    {
        _sut.Name.ShouldBe("Find");
    }

    // ── Column-name injection guard ──────────────────────────────────────────

    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    [InlineData("x; DROP TABLE")]
    [InlineData("a' OR '1'='1")]
    [InlineData("col--")]
    [InlineData("name with space")]
    [InlineData("name\"quote")]
    [InlineData("name;semicolon")]
    public async Task TranslateRejectsSqlInjectionInExplicitFieldName(string maliciousColumn)
    {
        // Why: BuildFindStatement validates every search column via IsValidColumnName
        // (SqliteFindTranslator.cs ~132) before it is quoted into the LIKE/GLOB WHERE clause.
        // A hostile field name must never reach SQL text — translation must fail loud instead.
        var container = CreateContainer(fields: [CreateField("name")]);
        var command = new FindCommand<object>
        {
            SearchTerm = "acme",
            FieldNames = [maliciousColumn]
        };

        var result = await _sut.Translate(command, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldNotBeNull();
        result.Code!.Name.ShouldBe("FindTranslationFailed");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public async Task TranslateAcceptsValidExplicitFieldNames()
    {
        var container = CreateContainer(fields: [CreateField("name"), CreateField("email")]);
        var command = new FindCommand<object>
        {
            SearchTerm = "acme",
            FieldNames = ["name", "email"]
        };

        var result = await _sut.Translate(command, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldContain("\"name\" LIKE @searchTerm");
        result.Value.CommandText.ShouldContain("\"email\" LIKE @searchTerm");
    }

    // ── LIKE wildcard escaping (case-insensitive path) ───────────────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task TranslateEscapesPercentAndUnderscoreLikeWildcardsCaseInsensitive()
    {
        // Why: EscapeLikeWildcards must neutralize literal LIKE metacharacters in the user-supplied
        // search term — otherwise "50%_off" would behave as a pattern instead of a literal substring.
        var container = CreateContainer(fields: [CreateField("name")]);
        var command = new FindCommand<object>
        {
            SearchTerm = "50%_off",
            CaseSensitive = false,
            FieldNames = ["name"]
        };

        var result = await _sut.Translate(command, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldContain("\"name\" LIKE @searchTerm");
        result.Value.CommandText.ShouldNotContain("GLOB");
        result.Value.Parameters[0].Value.ShouldBe("%50\\%\\_off%");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public async Task TranslateWrapsCaseInsensitiveSearchTermInPercentWildcards()
    {
        var container = CreateContainer(fields: [CreateField("name")]);
        var command = new FindCommand<object>
        {
            SearchTerm = "acme",
            CaseSensitive = false,
            FieldNames = ["name"]
        };

        var result = await _sut.Translate(command, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Parameters[0].Value.ShouldBe("%acme%");
    }

    // ── GLOB wildcard escaping (case-sensitive path) ─────────────────────────

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "DataGateway")]
    public async Task TranslateUsesGlobNotLikeForCaseSensitiveSearch()
    {
        var container = CreateContainer(fields: [CreateField("name")]);
        var command = new FindCommand<object>
        {
            SearchTerm = "Acme",
            CaseSensitive = true,
            FieldNames = ["name"]
        };

        var result = await _sut.Translate(command, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldContain("\"name\" GLOB @searchTerm");
        result.Value.CommandText.ShouldNotContain("LIKE");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public async Task TranslateWrapsCaseSensitiveSearchTermInGlobWildcards()
    {
        // Why: only asserts the no-metacharacter case. EscapeGlobWildcards (SqliteFindTranslator.cs
        // ~158) chains .Replace("*","[*]").Replace("?","[?]").Replace("[","[[]") — the final "["
        // replacement re-matches the brackets the first two replacements just inserted, corrupting
        // the escape for any term containing '*', '?', or '[' (see defectsFound). A plain
        // alphanumeric term has no such characters, so the no-op escape path is what's verified here.
        var container = CreateContainer(fields: [CreateField("name")]);
        var command = new FindCommand<object>
        {
            SearchTerm = "Acme",
            CaseSensitive = true,
            FieldNames = ["name"]
        };

        var result = await _sut.Translate(command, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Parameters[0].Value.ShouldBe("*Acme*");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Security")]
    public async Task TranslateEscapesAllGlobMetacharactersInCaseSensitiveSearchTerm()
    {
        // Why: EscapeGlobWildcards (SqliteFindTranslator.cs ~161) now escapes "[" FIRST, then "*"
        // then "?" — escaping "[" last (the old order) would re-match the "[" characters the "*"/"?"
        // replacements had just inserted, corrupting the escape. Escaping "[" first means the "[" it
        // inserts for "*"/"?" is never revisited, so a term containing all three metacharacters must
        // come through as a literal GLOB match, not a corrupted pattern.
        var container = CreateContainer(fields: [CreateField("name")]);
        var command = new FindCommand<object>
        {
            SearchTerm = "a*b?c[d]",
            CaseSensitive = true,
            FieldNames = ["name"]
        };

        var result = await _sut.Translate(command, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Parameters[0].Value.ShouldBe("*a[*]b[?]c[[]d]*");
    }

    // ── No searchable columns (AlwaysFalsePredicate) ─────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public async Task TranslateEmitsFalsePredicateWhenNoSearchableColumns()
    {
        var container = CreateContainer(fields: [CreateField("id", typeof(int))]);
        var command = new FindCommand<object> { SearchTerm = "acme" };

        var result = await _sut.Translate(command, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.CommandText.ShouldContain("WHERE FALSE");
    }

    // ── Empty search term ─────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public async Task TranslateReturnsFailureForEmptySearchTerm()
    {
        var container = CreateContainer(fields: [CreateField("name")]);
        var command = new FindCommand<object> { SearchTerm = "   " };

        var result = await _sut.Translate(command, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldNotBeNull();
        result.Code!.Name.ShouldBe("FindTranslationFailed");
    }

    // ── Command type guard ────────────────────────────────────────────────

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "DataGateway")]
    public async Task TranslateReturnsFailureForNonFindCommand()
    {
        var container = CreateContainer(fields: [CreateField("name")]);
        var genericCommand = new Mock<IDataCommand>();

        var result = await _sut.Translate(genericCommand.Object, container.Object, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldNotBeNull();
        result.Code!.Name.ShouldBe("InvalidCommandType");
    }
}
