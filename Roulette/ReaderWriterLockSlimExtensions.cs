namespace Roulette;

internal static class ReaderWriterLockSlimExtensions
{
    internal static T Read<T>(this ReaderWriterLockSlim rwLock, Func<T> function)
    {
        rwLock.EnterReadLock();
        try
        {
            return function();
        }
        finally
        {
            rwLock.ExitReadLock();
        }
    }

    internal static void Write(this ReaderWriterLockSlim rwLock, Action action)
    {
        rwLock.EnterWriteLock();
        try
        {
            action();
        }
        finally
        {
            rwLock.ExitWriteLock();
        }
    }
}
