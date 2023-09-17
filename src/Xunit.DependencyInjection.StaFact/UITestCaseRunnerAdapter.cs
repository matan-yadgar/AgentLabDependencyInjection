﻿using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Xunit.DependencyInjection.StaFact;

// ReSharper disable once InconsistentNaming
public class UITestCaseRunnerAdapter : IXunitTestCaseRunnerWrapper
{
    /// <inheritdoc />
    public virtual Type TestCaseType => typeof(UITestCase);

    public async Task<RunSummary> RunAsync(IXunitTestCase testCase, DependencyInjectionContext context,
        IMessageSink diagnosticMessageSink, IMessageBus messageBus, object?[] constructorArguments,
        ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource)
    {
        var scope = context.RootServices.CreateAsyncScope();

        await using var _ = scope.ConfigureAwait(false);

        var raw = new Dictionary<int, object>();
        foreach (var kv in FromServicesAttribute.CreateFromServices(testCase.Method.ToRuntimeMethod()))
        {
            raw[kv.Key] = testCase.TestMethodArguments[kv.Key];

            testCase.TestMethodArguments[kv.Key] = kv.Value == typeof(ITestOutputHelper)
                ? throw new NotSupportedException("Can't inject ITestOutputHelper via method arguments when use StaFact")
                : context.RootServices.GetService(kv.Value);
        }

        constructorArguments = scope.ServiceProvider.CreateTestClassConstructorArguments(constructorArguments, aggregator);

        var summary = await testCase.RunAsync(diagnosticMessageSink, messageBus, constructorArguments, aggregator, cancellationTokenSource).ConfigureAwait(false);

        foreach (var kv in raw)
            testCase.TestMethodArguments[kv.Key] = kv.Value;

        return summary;
    }
}
