namespace ResumenTPV;

/// <summary>
/// ACE/OleDb es COM: desde el pool (MTA) las llamadas se remiten al hilo UI (STA)
/// y la ventana queda en «No responde». Ejecutamos en un STA propio que bombea mensajes.
/// </summary>
internal static class StaTask
{
    public static Task<T> Run<T>(Func<T> work)
    {
        var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try
            {
                tcs.SetResult(work());
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        })
        {
            IsBackground = true,
            Name = "ResumenTPV-ACE",
        };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return tcs.Task;
    }
}
