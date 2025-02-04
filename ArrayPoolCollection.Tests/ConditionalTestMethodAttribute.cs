namespace ArrayPoolCollection.Tests;

public class ConditionalTestMethodAttribute : TestMethodAttribute
{
    public string Key { get; init; }

    public ConditionalTestMethodAttribute(string key)
    {
        Key = key;
    }

    public override TestResult[] Execute(ITestMethod testMethod)
    {
        if (Environment.GetEnvironmentVariable(Key) is not null)
        {
            return base.Execute(testMethod);
        }
        else
        {
            return [new TestResult {
                Outcome = UnitTestOutcome.Inconclusive,
                TestContextMessages = $"This test was skipped. To run this test, define the environment variable `{Key}`. You can do this with `dotnet test --environment {Key}`."
            }];
        }
    }
}
