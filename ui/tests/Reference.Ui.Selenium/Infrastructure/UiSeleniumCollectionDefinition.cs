namespace Reference.Ui.Selenium.Infrastructure;

/// <summary>xUnit collection so the one-time login happens once for the whole suite.</summary>
[CollectionDefinition("ui-selenium")]
public sealed class UiSeleniumCollectionDefinition : ICollectionFixture<SeleniumFixture>;
