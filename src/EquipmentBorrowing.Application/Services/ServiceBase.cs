using EquipmentBorrowing.Application.Abstractions;

namespace EquipmentBorrowing.Application.Services;

public abstract class ServiceBase
{
    protected readonly IUnitOfWorkFactory Uows;
    protected readonly IDatabaseErrorClassifier Classifier;

    protected ServiceBase(IUnitOfWorkFactory uows, IDatabaseErrorClassifier classifier)
    {
        Uows = uows;
        Classifier = classifier;
    }

    protected async Task<Result<T>> GuardAsync<T>(Func<Task<Result<T>>> work)
    {
        try
        {
            return await work();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (CommitOutcomeUnknownException)
        {
            return Result<T>.Fail(ErrorKind.UnknownOutcome,
                "The connection was lost while saving, so it is NOT known whether the change was stored. " +
                "Do not repeat the action yet: refresh the lists and check the current state first.");
        }
        catch (SimulatedFaultException)
        {
            return Result<T>.Fail(ErrorKind.Unexpected,
                "The operation failed part-way (simulated fault). Everything was rolled back; no changes were saved.");
        }
        catch (Exception ex)
        {
            return Classifier.Classify(ex) switch
            {
                DbFailureKind.Unavailable => Result<T>.Fail(ErrorKind.ConnectionError,
                    "Cannot reach the PostgreSQL database. No changes were saved. " +
                    "Check that the server is running and the connection settings are correct, then try again."),
                DbFailureKind.UniqueViolation or DbFailureKind.ForeignKeyViolation => Result<T>.Fail(ErrorKind.Conflict,
                    "The data changed while you were working (for example a duplicate identifier or an item that was just issued). " +
                    "No changes were saved. Refresh and try again."),
                DbFailureKind.CheckViolation => Result<T>.Fail(ErrorKind.Validation,
                    "The database rejected the values as invalid. No changes were saved."),
                _ => Result<T>.Fail(ErrorKind.Unexpected,
                    $"Something went wrong: {ex.GetType().Name}: {ex.Message} | Inner: {ex.InnerException?.Message}")
            };
        }
    }

    protected async Task CommitAsync(IUnitOfWork uow, CancellationToken ct)
    {
        try
        {
            await uow.CommitAsync(ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException
                                   && Classifier.Classify(ex) == DbFailureKind.Unavailable)
        {
            throw new CommitOutcomeUnknownException(ex);
        }
    }
}
