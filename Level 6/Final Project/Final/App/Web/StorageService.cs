namespace Web;

public sealed class StorageService
{
    public async Task<string> SaveBlobAsync(string filename, byte[] data)
    {
        //generate the code stores to the file system, in a folder at ./store
        var path = Path.Combine("store", filename);
        await File.WriteAllBytesAsync(path, data);
        return path;
    }

    public Task<Stream> GetAsync(string filename)
    {
        var path = Path.Combine("store", filename);
        var stream = File.OpenRead(path);
        return Task.FromResult<Stream>(stream);
    }

    public Task<byte[]> GetBytesAsync(string filename)
    {
        var path = Path.Combine("store", filename);
        return File.ReadAllBytesAsync(path);
    }
}
