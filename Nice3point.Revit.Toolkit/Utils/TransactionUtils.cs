using Autodesk.Revit.DB;

namespace Nice3point.Revit.Toolkit.Utils;

/// <summary>
///     Provides control over transactions
/// </summary>
public static class TransactionManager
{
    /// <summary>
    ///     Initializes a new sub-transaction
    /// </summary>
    /// <remarks>
    ///     The transaction will be canceled when exceptions occur, exceptions are not handled by the method<br />
    ///     Sub-transactions are objects that provide control over a subset of changes in a document<br />
    ///     A Sub-transaction can only be active as a part of an open transaction.
    ///     Sub-transactions may be nested inside each other, but with the restriction that every nested
    ///     sub-transaction is entirely contained (opened and closed) in the parent sub-transaction
    /// </remarks>
    public static void CreateSubTransaction(Document document, Action<Document> action)
    {
        var transaction = new SubTransaction(document);
        transaction.Start();
        try
        {
            action?.Invoke(document);
            transaction.Commit();
        }
        finally
        {
            if (!transaction.HasEnded()) transaction.RollBack();
            transaction.Dispose();
        }
    }

    /// <summary>
    ///     Initializes a new sub-transaction
    /// </summary>
    /// <remarks>
    ///     The transaction will be canceled when exceptions occur, exceptions are not handled by the method<br />
    ///     Sub-transactions are objects that provide control over a subset of changes in a document<br />
    ///     A Sub-transaction can only be active as a part of an open transaction.
    ///     Sub-transactions may be nested inside each other, but with the restriction that every nested
    ///     sub-transaction is entirely contained (opened and closed) in the parent sub-transaction
    /// </remarks>
    public static void CreateSubTransaction<T>(Document document, T param, Action<Document, T> action)
    {
        var transaction = new SubTransaction(document);
        transaction.Start();
        try
        {
            action?.Invoke(document, param);
            transaction.Commit();
        }
        finally
        {
            if (!transaction.HasEnded()) transaction.RollBack();
            transaction.Dispose();
        }
    }

    /// <summary>
    ///     Initializes a new sub-transaction
    /// </summary>
    /// <remarks>
    ///     The transaction will be canceled when exceptions occur, exceptions are not handled by the method<br />
    ///     Sub-transactions are objects that provide control over a subset of changes in a document<br />
    ///     A Sub-transaction can only be active as a part of an open transaction.
    ///     Sub-transactions may be nested inside each other, but with the restriction that every nested
    ///     sub-transaction is entirely contained (opened and closed) in the parent sub-transaction
    /// </remarks>
    public static void CreateSubTransaction<T0, T1>(Document document, T0 param0, T1 param1, Action<Document, T0, T1> action)
    {
        var transaction = new SubTransaction(document);
        transaction.Start();
        try
        {
            action?.Invoke(document, param0, param1);
            transaction.Commit();
        }
        finally
        {
            if (!transaction.HasEnded()) transaction.RollBack();
            transaction.Dispose();
        }
    }

    /// <summary>
    ///     Initializes a new sub-transaction
    /// </summary>
    /// <remarks>
    ///     The transaction will be canceled when exceptions occur, exceptions are not handled by the method<br />
    ///     Sub-transactions are objects that provide control over a subset of changes in a document<br />
    ///     A Sub-transaction can only be active as a part of an open transaction.
    ///     Sub-transactions may be nested inside each other, but with the restriction that every nested
    ///     sub-transaction is entirely contained (opened and closed) in the parent sub-transaction
    /// </remarks>
    public static void CreateSubTransaction<T0, T1, T2>(Document document, T0 param0, T1 param1, T2 param2, Action<Document, T0, T1, T2> action)
    {
        var transaction = new SubTransaction(document);
        transaction.Start();
        try
        {
            action?.Invoke(document, param0, param1, param2);
            transaction.Commit();
        }
        finally
        {
            if (!transaction.HasEnded()) transaction.RollBack();
            transaction.Dispose();
        }
    }

    /// <summary>
    ///     Initializes a new transaction
    /// </summary>
    /// <remarks>
    ///     The transaction will be canceled when exceptions occur, exceptions are not handled by the method<br />
    ///     Transactions are context-like objects that guard any changes made to a Revit model<br />
    ///     A document can have only one transaction open at any given time<br />
    ///     Transactions cannot be started when the document is in read-only mode, either permanently or temporarily.
    ///     See the Document class methods IsReadOnly and IsModifiable for more details<br />
    ///     Transactions in linked documents are not permitted, for linked documents are not allowed to be modified
    /// </remarks>
    public static void CreateTransaction(Document document, string transactionName, Action<Document> action)
    {
        var transaction = new Autodesk.Revit.DB.Transaction(document);
        transaction.Start(transactionName);
        try
        {
            action?.Invoke(document);
            transaction.Commit();
        }
        finally
        {
            if (!transaction.HasEnded()) transaction.RollBack();
            transaction.Dispose();
        }
    }

    /// <summary>
    ///     Initializes a new transaction
    /// </summary>
    /// <remarks>
    ///     The transaction will be canceled when exceptions occur, exceptions are not handled by the method<br />
    ///     Transactions are context-like objects that guard any changes made to a Revit model<br />
    ///     A document can have only one transaction open at any given time<br />
    ///     Transactions cannot be started when the document is in read-only mode, either permanently or temporarily.
    ///     See the Document class methods IsReadOnly and IsModifiable for more details<br />
    ///     Transactions in linked documents are not permitted, for linked documents are not allowed to be modified
    /// </remarks>
    public static void CreateTransaction<T>(Document document, string transactionName, T param, Action<Document, T> action)
    {
        var transaction = new Autodesk.Revit.DB.Transaction(document);
        transaction.Start(transactionName);
        try
        {
            action?.Invoke(document, param);
            transaction.Commit();
        }
        finally
        {
            if (!transaction.HasEnded()) transaction.RollBack();
            transaction.Dispose();
        }
    }

    /// <summary>
    ///     Initializes a new transaction
    /// </summary>
    /// <remarks>
    ///     The transaction will be canceled when exceptions occur, exceptions are not handled by the method<br />
    ///     Transactions are context-like objects that guard any changes made to a Revit model<br />
    ///     A document can have only one transaction open at any given time<br />
    ///     Transactions cannot be started when the document is in read-only mode, either permanently or temporarily.
    ///     See the Document class methods IsReadOnly and IsModifiable for more details<br />
    ///     Transactions in linked documents are not permitted, for linked documents are not allowed to be modified
    /// </remarks>
    public static void CreateTransaction<T0, T1>(Document document, string transactionName, T0 param0, T1 param1, Action<Document, T0, T1> action)
    {
        var transaction = new Autodesk.Revit.DB.Transaction(document);
        transaction.Start(transactionName);
        try
        {
            action?.Invoke(document, param0, param1);
            transaction.Commit();
        }
        finally
        {
            if (!transaction.HasEnded()) transaction.RollBack();
            transaction.Dispose();
        }
    }

    /// <summary>
    ///     Initializes a new transaction
    /// </summary>
    /// <remarks>
    ///     The transaction will be canceled when exceptions occur, exceptions are not handled by the method<br />
    ///     Transactions are context-like objects that guard any changes made to a Revit model<br />
    ///     A document can have only one transaction open at any given time<br />
    ///     Transactions cannot be started when the document is in read-only mode, either permanently or temporarily.
    ///     See the Document class methods IsReadOnly and IsModifiable for more details<br />
    ///     Transactions in linked documents are not permitted, for linked documents are not allowed to be modified
    /// </remarks>
    public static void CreateTransaction<T0, T1, T2>(Document document, string transactionName, T0 param0, T1 param1, T2 param2, Action<Document, T0, T1, T2> action)
    {
        var transaction = new Autodesk.Revit.DB.Transaction(document);
        transaction.Start(transactionName);
        try
        {
            action?.Invoke(document, param0, param1, param2);
            transaction.Commit();
        }
        finally
        {
            if (!transaction.HasEnded()) transaction.RollBack();
            transaction.Dispose();
        }
    }

    /// <summary>
    ///     Initializes a new transaction
    /// </summary>
    /// <remarks>
    ///     The transaction will be canceled when exceptions occur, exceptions are not handled by the method<br />
    ///     Transaction groups aggregate a number of transactions<br />
    ///     A transaction group can only be started when no transaction is active
    /// </remarks>
    public static void CreateGroupTransaction(Document document, string transactionName, Action<Document> action)
    {
        var transaction = new TransactionGroup(document);
        transaction.Start(transactionName);
        try
        {
            action?.Invoke(document);
            transaction.Assimilate();
        }
        finally
        {
            if (!transaction.HasEnded()) transaction.RollBack();
            transaction.Dispose();
        }
    }

    /// <summary>
    ///     Initializes a new transaction
    /// </summary>
    /// <remarks>
    ///     The transaction will be canceled when exceptions occur, exceptions are not handled by the method<br />
    ///     Transaction groups aggregate a number of transactions<br />
    ///     A transaction group can only be started when no transaction is active
    /// </remarks>
    public static void CreateGroupTransaction<T>(Document document, string transactionName, T param, Action<Document, T> action)
    {
        var transaction = new TransactionGroup(document);
        transaction.Start(transactionName);
        try
        {
            action?.Invoke(document, param);
            transaction.Assimilate();
        }
        finally
        {
            if (!transaction.HasEnded()) transaction.RollBack();
            transaction.Dispose();
        }
    }

    /// <summary>
    ///     Initializes a new transaction
    /// </summary>
    /// <remarks>
    ///     The transaction will be canceled when exceptions occur, exceptions are not handled by the method<br />
    ///     Transaction groups aggregate a number of transactions<br />
    ///     A transaction group can only be started when no transaction is active
    /// </remarks>
    public static void CreateGroupTransaction<T0, T1>(Document document, string transactionName, T0 param0, T1 param1, Action<Document, T0, T1> action)
    {
        var transaction = new TransactionGroup(document);
        transaction.Start(transactionName);
        try
        {
            action?.Invoke(document, param0, param1);
            transaction.Assimilate();
        }
        finally
        {
            if (!transaction.HasEnded()) transaction.RollBack();
            transaction.Dispose();
        }
    }

    /// <summary>
    ///     Initializes a new transaction
    /// </summary>
    /// <remarks>
    ///     The transaction will be canceled when exceptions occur, exceptions are not handled by the method<br />
    ///     Transaction groups aggregate a number of transactions<br />
    ///     A transaction group can only be started when no transaction is active
    /// </remarks>
    public static void CreateGroupTransaction<T0, T1, T2>(Document document, string transactionName, T0 param0, T1 param1, T2 param2, Action<Document, T0, T1, T2> action)
    {
        var transaction = new TransactionGroup(document);
        transaction.Start(transactionName);
        try
        {
            action?.Invoke(document, param0, param1, param2);
            transaction.Assimilate();
        }
        finally
        {
            if (!transaction.HasEnded()) transaction.RollBack();
            transaction.Dispose();
        }
    }
}