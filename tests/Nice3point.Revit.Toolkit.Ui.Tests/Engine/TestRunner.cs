using Autodesk.Revit.UI;

namespace Nice3point.Revit.Toolkit.Ui.Tests.Engine;

/// <summary>
///     Provides test execution scope with automatic result reporting via TaskDialog.
/// </summary>
public sealed class TestRunner
{
    private readonly string _testName;
    private readonly List<string> _summaryMessages = [];

    internal TestRunner(string testName)
    {
        _testName = testName;
    }

    internal Exception? Exception { get; set; }

    /// <summary>
    ///     Adds a summary message to the test report.
    /// </summary>
    public void Summary(string message)
    {
        _summaryMessages.Add(message);
    }

    internal void ShowResult()
    {
        var passed = Exception is null;
        var status = passed ? "PASSED ✓" : "FAILED ✗";

        var content = string.Empty;

        if (_summaryMessages.Count > 0)
        {
            content = string.Join("\n", _summaryMessages);
        }

        if (Exception is not null)
        {
            if (content.Length > 0) content += "\n\n";
            content += $"Exception:\n{Exception.Message}";
        }

        if (string.IsNullOrEmpty(content))
        {
            content = passed ? "All assertions passed." : "Test failed.";
        }

        var dialog = new TaskDialog($"{_testName} - {status}")
        {
            MainContent = content,
            MainIcon = passed ? TaskDialogIcon.TaskDialogIconShield : TaskDialogIcon.TaskDialogIconWarning
        };

        dialog.Show();
    }
}