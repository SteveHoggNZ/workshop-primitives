namespace Lab;

/// <summary>A problem the person can fix; printed without a stack trace.</summary>
internal sealed class LabException(string message) : Exception(message);
