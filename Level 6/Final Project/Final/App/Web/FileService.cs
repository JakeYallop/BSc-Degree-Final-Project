namespace Web;
public sealed class FileService
{
    public FileService()
    {
    }

    public async Task<Guid> StoreFileAsync(byte[] data, string extension, CancellationToken cancellationToken = default)
    {
        var id = Guid.NewGuid();
        var path = $"store/{id}";
        await File.WriteAllBytesAsync(path, data, cancellationToken);
        return id;
    }

    public Task<Stream> GetFileAsync(Guid fileId, CancellationToken cancellationToken = default)
    {
        var path = $"store/{fileId}";
        var stream = File.OpenRead(path);
        if (cancellationToken.IsCancellationRequested)
        {
            stream.Dispose();
            throw new OperationCanceledException();
        }
        return Task.FromResult<Stream>(stream);
    }
}
