namespace Nice3point.Revit.Toolkit.Utils;

/// <summary>
///     Provides control over transactions
/// </summary>
[PublicAPI]
[Obsolete("This API will be removed in future package versions. Create transactions manually")]
public static class TransactionManager
{
    /// <summary>
    ///     Initializes document modification
    /// </summary>
    /// <param name="document">The document to be modified</param>
    /// <remarks> The transaction name has already been declared in the implementation, to override, use the <see cref="ITransactionSettings.SetName" /> method</remarks>
    /// <returns>New <see cref="ITransaction" /> object</returns>
    [Pure]
    public static ITransaction Modify(this Document document)
    {
        return new Transaction(document, new TransactionSettings());
    }

    /// <summary>
    ///     Initializes document modification
    /// </summary>
    /// <param name="document">The document to be modified</param>
    /// <param name="settings"><see cref="ITransactionSettings" /> instance</param>
    /// <remarks> The transaction name has already been declared in the implementation, to override, use the <see cref="ITransactionSettings.SetName" /> method</remarks>
    /// <returns>New <see cref="ITransaction" /> object</returns>
    [Pure]
    public static ITransaction Modify(this Document document, [InstantHandle] Func<IDocumentTransactionSettings, ITransactionSettings> settings)
    {
        var transactionSettings = settings?.Invoke(new DocumentTransactionSettings());
        return new Transaction(document, (TransactionSettings)transactionSettings);
    }

    /// <summary>
    ///     Initializes document modification
    /// </summary>
    /// <param name="document">The document to be modified</param>
    /// <param name="settings"><see cref="ISubTransactionSettings" /> instance</param>
    /// <remarks>SubTransaction does not provide customization settings</remarks>
    /// <returns>New <see cref="ISubTransaction" /> object</returns>
    [Pure]
    // ReSharper disable once UnusedParameter.Global
    // SubTransaction is not customizable. Latest check R2023 and older
    public static ISubTransaction Modify(this Document document, [InstantHandle] Func<IDocumentTransactionSettings, ISubTransactionSettings> settings)
    {
        return new SubTransaction(document);
    }

    /// <summary>
    ///     Initializes document modification
    /// </summary>
    /// <param name="document">The document to be modified</param>
    /// <param name="settings"><see cref="IGroupTransactionSettings" /> instance</param>
    /// <remarks> The group transaction name has already been declared in the implementation, to override, use the <see cref="IGroupTransactionSettings.SetName" /> method</remarks>
    /// <returns>New <see cref="IGroupTransaction" /> object</returns>
    [Pure]
    public static IGroupTransaction Modify(this Document document, [InstantHandle] Func<IDocumentTransactionSettings, IGroupTransactionSettings> settings)
    {
        var transactionSettings = settings?.Invoke(new DocumentTransactionSettings());
        return new GroupTransaction(document, (GroupTransactionSettings)transactionSettings);
    }
}

internal sealed class DocumentTransactionSettings : IDocumentTransactionSettings
{
    public ITransactionSettings Transaction => new TransactionSettings();
    public ISubTransactionSettings SubTransaction => null;
    public IGroupTransactionSettings GroupTransaction => new GroupTransactionSettings();
}

internal sealed class TransactionSettings : ITransactionSettings
{
    private string _name;

    public string Name
    {
        get => _name ?? "External Modifications";
        private set => _name = value;
    }

    public Action<Autodesk.Revit.DB.Transaction> InitializeHandler { get; private set; }

    public ITransactionSettings SetName(string name)
    {
        Name = name;
        return this;
    }

    public ITransactionSettings DisableModalHandling()
    {
        InitializeHandler += transaction =>
        {
            var handlingOptions = transaction.GetFailureHandlingOptions();
            handlingOptions.SetForcedModalHandling(false);
            transaction.SetFailureHandlingOptions(handlingOptions);
        };
        return this;
    }

    public ITransactionSettings EnableClearAfterRollback()
    {
        InitializeHandler += transaction =>
        {
            var handlingOptions = transaction.GetFailureHandlingOptions();
            handlingOptions.SetClearAfterRollback(true);
            transaction.SetFailureHandlingOptions(handlingOptions);
        };
        return this;
    }

    public ITransactionSettings EnableDelayedMiniWarnings()
    {
        InitializeHandler += transaction =>
        {
            var handlingOptions = transaction.GetFailureHandlingOptions();
            handlingOptions.SetDelayedMiniWarnings(true);
            transaction.SetFailureHandlingOptions(handlingOptions);
        };
        return this;
    }
}

internal sealed class GroupTransactionSettings : IGroupTransactionSettings
{
    private string _name;

    public string Name
    {
        get => _name ?? "External Modifications";
        private set => _name = value;
    }

    public Action<TransactionGroup> InitializeHandler { get; private set; }

    public IGroupTransactionSettings SetName([NotNull] string name)
    {
        Name = name;
        return this;
    }

    public IGroupTransactionSettings DisableModalHandling()
    {
        InitializeHandler += transaction => transaction.IsFailureHandlingForcedModal = true;
        return this;
    }
}

internal sealed class Transaction(Document document, TransactionSettings settings) : ITransaction
{
    public void Commit(Action<Document, Autodesk.Revit.DB.Transaction> action)
    {
        var transaction = new Autodesk.Revit.DB.Transaction(document, settings.Name);
        settings.InitializeHandler?.Invoke(transaction);
        transaction.Start();
        try
        {
            action?.Invoke(document, transaction);
            transaction.Commit();
        }
        finally
        {
            if (!transaction.HasEnded()) transaction.RollBack();
            transaction.Dispose();
        }
    }

    public void RollBack(Action<Document, Autodesk.Revit.DB.Transaction> action)
    {
        var transaction = new Autodesk.Revit.DB.Transaction(document, settings.Name);
        transaction.Start();
        try
        {
            action?.Invoke(document, transaction);
        }
        finally
        {
            if (!transaction.HasEnded()) transaction.RollBack();
            transaction.Dispose();
        }
    }
}

internal sealed class SubTransaction(Document document) : ISubTransaction
{
    public void Commit(Action<Document, Autodesk.Revit.DB.SubTransaction> action)
    {
        var transaction = new Autodesk.Revit.DB.SubTransaction(document);
        transaction.Start();
        try
        {
            action?.Invoke(document, transaction);
            transaction.Commit();
        }
        finally
        {
            if (!transaction.HasEnded()) transaction.RollBack();
            transaction.Dispose();
        }
    }

    public void RollBack(Action<Document, Autodesk.Revit.DB.SubTransaction> action)
    {
        var transaction = new Autodesk.Revit.DB.SubTransaction(document);
        transaction.Start();
        try
        {
            action?.Invoke(document, transaction);
        }
        finally
        {
            if (!transaction.HasEnded()) transaction.RollBack();
            transaction.Dispose();
        }
    }
}

internal sealed class GroupTransaction(Document document, GroupTransactionSettings settings) : IGroupTransaction
{
    public void Commit(Action<Document, TransactionGroup> action)
    {
        var transaction = new TransactionGroup(document, settings.Name);
        settings.InitializeHandler?.Invoke(transaction);
        transaction.Start();
        try
        {
            action?.Invoke(document, transaction);
            transaction.Commit();
        }
        finally
        {
            if (!transaction.HasEnded()) transaction.RollBack();
            transaction.Dispose();
        }
    }

    public void RollBack(Action<Document, TransactionGroup> action)
    {
        var transaction = new TransactionGroup(document, settings.Name);
        transaction.Start();
        try
        {
            action?.Invoke(document, transaction);
        }
        finally
        {
            if (!transaction.HasEnded()) transaction.RollBack();
            transaction.Dispose();
        }
    }

    public void Assimilate(Action<Document, TransactionGroup> action)
    {
        var transaction = new TransactionGroup(document, settings.Name);
        transaction.Start();
        try
        {
            action?.Invoke(document, transaction);
            transaction.Assimilate();
        }
        finally
        {
            if (!transaction.HasEnded()) transaction.RollBack();
            transaction.Dispose();
        }
    }
}

/// <summary>
///     Contains settings for modifying the document
/// </summary>
[PublicAPI]
public interface IDocumentTransactionSettings
{
    /// <summary>
    ///     Provides access for <see cref="ITransactionSettings" />
    /// </summary>
    public ITransactionSettings Transaction { get; }

    /// <summary>
    ///     Provides access for <see cref="ISubTransactionSettings" />
    /// </summary>
    public ISubTransactionSettings SubTransaction { get; }

    /// <summary>
    ///     Provides access for <see cref="IGroupTransactionSettings" />
    /// </summary>
    public IGroupTransactionSettings GroupTransaction { get; }
}

/// <summary>
///     Contains settings that will be applied before the <see cref="ITransaction" /> starts
/// </summary>
[PublicAPI]
public interface ITransactionSettings
{
    /// <summary>Sets the transaction name</summary>
    /// <remarks>
    ///     The name will later appear in the Undo menu in Revit after a transaction is successfully committed
    /// </remarks>
    /// <param name="name">A name for the transaction</param>
    public ITransactionSettings SetName(string name);

    /// <summary>
    ///     Disable the display of a modal (blocking) error dialog if the transaction failed to finish
    /// </summary>
    public ITransactionSettings DisableModalHandling();

    /// <summary>
    ///     Clear all posted failures silently when the failing transaction is being rolled back intentionally <br />
    /// </summary>
    public ITransactionSettings EnableClearAfterRollback();

    /// <summary>
    ///     Delay the display of the mini-warning dialog (if one is to be shown as a result of warnings in the current transaction) until the end of the next transaction
    /// </summary>
    /// <remarks>
    ///     This controls warnings suitable for the mini-warnings dialog only<br />
    ///     If <see cref="DisableModalHandling" /> was not applied, the method call will be ignored
    /// </remarks>
    public ITransactionSettings EnableDelayedMiniWarnings();
}

/// <summary>
///     Contains settings that will be applied before the <see cref="ISubTransaction" /> starts
/// </summary>
/// <remarks>SubTransaction does not provide customization settings</remarks>
[PublicAPI]
public interface ISubTransactionSettings;

/// <summary>
///     Contains settings that will be applied before the <see cref="IGroupTransaction" /> starts
/// </summary>
[PublicAPI]
public interface IGroupTransactionSettings
{
    /// <summary>Sets the transaction group's name</summary>
    /// <remarks>
    ///     Transaction group only needs a name if it is going to be assimilated at the end.
    /// </remarks>
    /// <param name="name">A name for the transaction group</param>
    public IGroupTransactionSettings SetName(string name);

    /// <summary>
    ///     Forces all transactions finished inside this group to disable modal failure handling regardless of what failure handling options are set for those transactions
    /// </summary>
    /// <remarks>This method is ignored during events, when failure handling is always modal</remarks>
    public IGroupTransactionSettings DisableModalHandling();
}

/// <summary>
///     Provides a mechanism for managing transactions
/// </summary>
/// <remarks>
///     Transactions are context-like objects that guard any changes made to a Revit model<br />
///     A document can have only one transaction open at any given time<br />
///     Transactions cannot be started when the document is in read-only mode, either permanently or temporarily.
///     See the Document class methods IsReadOnly and IsModifiable for more details<br />
///     Transactions in linked documents are not permitted, for linked documents are not allowed to be modified
/// </remarks>
[PublicAPI]
public interface ITransaction
{
    /// <summary>Commits all changes made to the model during the transaction</summary>
    /// <remarks>
    ///     By committing a transaction, all changes made to the model during the transaction
    ///     are accepted. A new undo item will appear in the Undo menu in Revit, which allows
    ///     the user to undo the changes. The undo item will have this transaction's name.
    ///     Be aware that committing may fail or can be delayed (as a result of failure handling.)
    /// </remarks>
    /// <exception cref="T:Autodesk.Revit.Exceptions.InvalidOperationException">
    ///     Document is a linked file. Transactions can only be used in primary documents (projects or families.) <br />
    ///     The transaction's document is currently in failure mode
    /// </exception>
    void Commit(Action<Document, Autodesk.Revit.DB.Transaction> action);

    /// <summary>Rolls back all changes made to the model during the transaction</summary>
    /// <remarks>
    ///     By rolling back a transaction, all changes made to the model are discarded.
    ///     Be aware that rolling back may be delayed (as a result of failure handling.)
    /// </remarks>
    /// <exception cref="T:Autodesk.Revit.Exceptions.InvalidOperationException">
    ///     The transaction's document is currently in failure mode
    /// </exception>
    void RollBack(Action<Document, Autodesk.Revit.DB.Transaction> action);
}

/// <summary>
///     Provides a mechanism for managing sub-transactions
/// </summary>
/// <remarks>
///     Sub-transactions are objects that provide control over a subset of changes in a document<br />
///     A Sub-transaction can only be active as a part of an open transaction<br />
///     Sub-transactions may be nested inside each other, but with the restriction that every nested
///     sub-transaction is entirely contained (opened and closed) in the parent sub-transaction
/// </remarks>
[PublicAPI]
public interface ISubTransaction
{
    /// <summary>Commits all changes made to the model made during the sub-transaction</summary>
    /// <remarks>
    ///     <p>
    ///         The changes are not permanently committed to the document yet. They will be
    ///         committed only when the active transaction is committed. If the transaction
    ///         is rolled back instead, the changes committed during this sub-transaction will be discarded
    ///     </p>
    /// </remarks>
    /// <exception cref="T:Autodesk.Revit.Exceptions.InvalidOperationException">
    ///     A sub-transaction can only be active inside an open Transaction
    /// </exception>
    void Commit(Action<Document, Autodesk.Revit.DB.SubTransaction> action);

    /// <summary>Discards all changes made to the model during the sub-transaction</summary>
    /// <remarks>
    ///     <p>
    ///         The parent transaction (or a parent sub-transaction, if any)
    ///         can still be committed, but the changes rolled back by this
    ///         method will not be part of the committed transaction
    ///     </p>
    /// </remarks>
    /// <exception cref="T:Autodesk.Revit.Exceptions.InvalidOperationException">
    ///     A sub-transaction can only be active inside an open Transaction
    /// </exception>
    void RollBack(Action<Document, Autodesk.Revit.DB.SubTransaction> action);
}

/// <summary>
///     Provides a mechanism for managing group transactions
/// </summary>
/// <remarks>
///     Transaction groups aggregate a number of transactions<br />
///     A transaction group can only be started when no transaction is active
/// </remarks>
[PublicAPI]
public interface IGroupTransaction
{
    /// <summary>
    ///     Commits the transaction group
    /// </summary>
    /// <remarks>
    ///     <p>Committing a group does not change the model. It only confirms the commitment of all inner groups and transactions</p>
    ///     <p>
    ///         Commit can be called only when all inner transaction groups and transactions are finished,
    ///         i.e. after they were either committed or rolled back. If there is still a transaction or an inner
    ///         transaction group open, an attempt to commit this outer group will cause an exception
    ///     </p>
    /// </remarks>
    /// <exception cref="T:Autodesk.Revit.Exceptions.InvalidOperationException">
    ///     The transaction's document is currently in failure mode<br />
    ///     Transaction groups cannot be closed until failure handling is finished
    /// </exception>
    void Commit(Action<Document, TransactionGroup> action);

    /// <summary>
    ///     Rolls back the transaction group, which effectively undoes all transactions committed inside the group
    /// </summary>
    /// <remarks>
    ///     <p>
    ///         RollBack can be called only when all inner transaction groups and transactions are finished,
    ///         i.e. after they were either committed or rolled back
    ///     </p>
    /// </remarks>
    /// <exception cref="T:Autodesk.Revit.Exceptions.InvalidOperationException">
    ///     The transaction's document is currently in failure mode<br />
    ///     Transaction groups cannot be closed until failure handling is finished
    /// </exception>
    void RollBack(Action<Document, TransactionGroup> action);

    /// <summary>
    ///     Assimilates all inner transactions by merging them into a single undo item
    /// </summary>
    /// <remarks>
    ///     <p>After a successful assimilation the transaction group is committed</p>
    ///     <p>
    ///         All transactions committed inside this group will be merged into one
    ///         single transaction. The resulting undo item will bear this group's name
    ///     </p>
    ///     <p>
    ///         Assimilate can be called only when all inner transaction groups and transactions
    ///         are finished, i.e. after they were either committed or rolled back
    ///     </p>
    /// </remarks>
    /// <exception cref="T:Autodesk.Revit.Exceptions.InvalidOperationException">
    ///     The transaction's document is currently in failure mode <br />
    ///     Transaction groups cannot be closed until failure handling is finished
    /// </exception>
    void Assimilate(Action<Document, TransactionGroup> action);
}