namespace Nice3point.Revit.Toolkit.Utils;

/// <summary>
///     Provides utils for processing failures
/// </summary>
internal static class FailureUtils
{
    /// <summary>
    ///     Automatically processes failure messages during transaction flow by resolving or deleting them, eliminating the need for manual intervention
    /// </summary>
    /// <param name="failuresAccessor">An object that provides access to the failure messages and their handling options</param>
    /// <returns>
    ///     Returns <see cref="FailureProcessingResult.ProceedWithCommit"/> if all failures are resolved or deleted, otherwise returns <see cref="FailureProcessingResult.ProceedWithRollBack"/>
    /// </returns>
    /// <remarks>
    ///     Warnings are automatically deleted, and failures with available resolutions are resolved.
    /// </remarks>
    internal static FailureProcessingResult ResolveFailures(FailuresAccessor failuresAccessor)
    {
        var failureMessages = failuresAccessor.GetFailureMessages();
        if (failureMessages.Count == 0)
        {
            return FailureProcessingResult.Continue;
        }
        
        var hasUnresolvedFailures = false;
        foreach (var failureMessage in failureMessages)
        {
            var severity = failureMessage.GetSeverity();
            if (severity == FailureSeverity.Warning)
            {
                failuresAccessor.DeleteWarning(failureMessage);
            }
            else
            {
                if (failureMessage.HasResolutions())
                {
                    failuresAccessor.ResolveFailure(failureMessage);
                }
                else
                {
                    hasUnresolvedFailures = true;
                }
            }
        }

        if (!hasUnresolvedFailures) return FailureProcessingResult.ProceedWithCommit;
        
        var failureHandlingOptions = failuresAccessor.GetFailureHandlingOptions();
        failureHandlingOptions.SetClearAfterRollback(true);
        failuresAccessor.SetFailureHandlingOptions(failureHandlingOptions);

        return FailureProcessingResult.ProceedWithRollBack;
    }
    
    /// <summary>
    ///     Automatically processes failure messages during transaction flow by cancelling all them, eliminating the need for manual intervention
    /// </summary>
    /// <param name="failuresAccessor">An object that provides access to the failure messages and their handling options</param>
    /// <returns>
    ///     Returns <see cref="FailureProcessingResult.ProceedWithRollBack"/> if the failuresAccessor has any message/>
    /// </returns>
    internal static FailureProcessingResult DismissFailures(FailuresAccessor failuresAccessor)
    {
        var failureMessages = failuresAccessor.GetFailureMessages();
        if (failureMessages.Count == 0)
        {
            return FailureProcessingResult.Continue;
        }

        var failureHandlingOptions = failuresAccessor.GetFailureHandlingOptions();
        failureHandlingOptions.SetClearAfterRollback(true);
        failuresAccessor.SetFailureHandlingOptions(failureHandlingOptions);

        return FailureProcessingResult.ProceedWithRollBack;
    }
}