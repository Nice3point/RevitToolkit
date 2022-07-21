using Autodesk.Revit.DB;

namespace Nice3point.Revit.Toolkit.Transaction;

public static class TransactionManager
{
    /// <summary>
    ///     The method used to create a single sub-transaction.
    /// </summary>
    public static void CreateSubTransaction(Document document, Action<Document> action)
    {
        using var transaction = new SubTransaction(document);
        transaction.Start();
        try
        {
            action?.Invoke(document);
            transaction.Commit();
        }
        finally
        {
            if (!transaction.HasEnded()) transaction.RollBack();
        }
    }

    /// <summary>
    ///     The method used to create a single sub-transaction.
    /// </summary>
    public static void CreateSubTransaction<T>(Document document, T param, Action<Document, T> action)
    {
        using var transaction = new SubTransaction(document);
        transaction.Start();
        try
        {
            action?.Invoke(document, param);
            transaction.Commit();
        }
        finally
        {
            if (!transaction.HasEnded()) transaction.RollBack();
        }
    }

    /// <summary>
    ///     The method used to create a single sub-transaction.
    /// </summary>
    public static void CreateSubTransaction<T0, T1>(Document document, T0 param0, T1 param1, Action<Document, T0, T1> action)
    {
        using var transaction = new SubTransaction(document);
        transaction.Start();
        try
        {
            action?.Invoke(document, param0, param1);
            transaction.Commit();
        }
        finally
        {
            if (!transaction.HasEnded()) transaction.RollBack();
        }
    }

    /// <summary>
    ///     The method used to create a single sub-transaction.
    /// </summary>
    public static void CreateSubTransaction<T0, T1, T2>(Document document, T0 param0, T1 param1, T2 param2, Action<Document, T0, T1, T2> action)
    {
        using var transaction = new SubTransaction(document);
        transaction.Start();
        try
        {
            action?.Invoke(document, param0, param1, param2);
            transaction.Commit();
        }
        finally
        {
            if (!transaction.HasEnded()) transaction.RollBack();
        }
    }

    /// <summary>
    ///     The method used to create a single transaction.
    /// </summary>
    public static void CreateTransaction(Document document, string transactionName, Action<Document> action)
    {
        using var transaction = new Autodesk.Revit.DB.Transaction(document);
        transaction.Start(transactionName);
        try
        {
            action?.Invoke(document);
            transaction.Commit();
        }
        finally
        {
            if (!transaction.HasEnded()) transaction.RollBack();
        }
    }

    /// <summary>
    ///     The method used to create a single transaction.
    /// </summary>
    public static void CreateTransaction<T>(Document document, string transactionName, T param, Action<Document, T> action)
    {
        using var transaction = new Autodesk.Revit.DB.Transaction(document);
        transaction.Start(transactionName);
        try
        {
            action?.Invoke(document, param);
            transaction.Commit();
        }
        finally
        {
            if (!transaction.HasEnded()) transaction.RollBack();
        }
    }

    /// <summary>
    ///     The method used to create a single transaction.
    /// </summary>
    public static void CreateTransaction<T0, T1>(Document document, string transactionName, T0 param0, T1 param1, Action<Document, T0, T1> action)
    {
        using var transaction = new Autodesk.Revit.DB.Transaction(document);
        transaction.Start(transactionName);
        try
        {
            action?.Invoke(document, param0, param1);
            transaction.Commit();
        }
        finally
        {
            if (!transaction.HasEnded()) transaction.RollBack();
        }
    }

    /// <summary>
    ///     The method used to create a single transaction.
    /// </summary>
    public static void CreateTransaction<T0, T1, T2>(Document document, string transactionName, T0 param0, T1 param1, T2 param2, Action<Document, T0, T1, T2> action)
    {
        using var transaction = new Autodesk.Revit.DB.Transaction(document);
        transaction.Start(transactionName);
        try
        {
            action?.Invoke(document, param0, param1, param2);
            transaction.Commit();
        }
        finally
        {
            if (!transaction.HasEnded()) transaction.RollBack();
        }
    }

    /// <summary>
    ///     The method used to create a group of transactions.
    /// </summary>
    public static void CreateGroupTransaction(Document document, string transactionName, Action<Document> action)
    {
        using var transaction = new TransactionGroup(document);
        transaction.Start(transactionName);
        try
        {
            action?.Invoke(document);
            transaction.Assimilate();
        }
        finally
        {
            if (!transaction.HasEnded()) transaction.RollBack();
        }
    }

    /// <summary>
    ///     The method used to create a group of transactions.
    /// </summary>
    public static void CreateGroupTransaction<T>(Document document, string transactionName, T param, Action<Document, T> action)
    {
        using var transaction = new TransactionGroup(document);
        transaction.Start(transactionName);
        try
        {
            action?.Invoke(document, param);
            transaction.Assimilate();
        }
        finally
        {
            if (!transaction.HasEnded()) transaction.RollBack();
        }
    }

    /// <summary>
    ///     The method used to create a group of transactions.
    /// </summary>
    public static void CreateGroupTransaction<T0, T1>(Document document, string transactionName, T0 param0, T1 param1, Action<Document, T0, T1> action)
    {
        using var transaction = new TransactionGroup(document);
        transaction.Start(transactionName);
        try
        {
            action?.Invoke(document, param0, param1);
            transaction.Assimilate();
        }
        finally
        {
            if (!transaction.HasEnded()) transaction.RollBack();
        }
    }

    /// <summary>
    ///     The method used to create a group of transactions.
    /// </summary>
    public static void CreateGroupTransaction<T0, T1, T2>(Document document, string transactionName, T0 param0, T1 param1, T2 param2, Action<Document, T0, T1, T2> action)
    {
        using var transaction = new TransactionGroup(document);
        transaction.Start(transactionName);
        try
        {
            action?.Invoke(document, param0, param1, param2);
            transaction.Assimilate();
        }
        finally
        {
            if (!transaction.HasEnded()) transaction.RollBack();
        }
    }
}