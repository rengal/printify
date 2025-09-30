using System.Text;

namespace Printify.Storage.Tests.Storage;

public sealed class InMemoryBlobStorageTests
{
    // [Fact]
    // public async Task PutAndGetRoundTrip()
    // {
    //     //await using var context = TestServices.TestServices.Create();
    //     var storage = context.BlobSto;
    //     await using var source = new MemoryStream(Encoding.UTF8.GetBytes("hello"));
    //     var id = await storage.PutAsync(source, new BlobMetadata("text/plain", source.Length));
    //
    //     await using var retrieved = await storage.GetAsync(id);
    //     Assert.NotNull(retrieved);
    //     using var reader = new StreamReader(retrieved!, Encoding.UTF8);
    //     var text = await reader.ReadToEndAsync();
    //     Assert.Equal("hello", text);
    // }
    //
    // [Fact]
    // public async Task DeleteRemovesBlob()
    // {
    //     var storage = new InMemoryBlobStorage();
    //     await using var source = new MemoryStream(new byte[] { 1, 2, 3 });
    //     var id = await storage.PutAsync(source, new BlobMetadata("application/octet-stream", source.Length));
    //
    //     await storage.DeleteAsync(id);
    //     await using var retrieved = await storage.GetAsync(id);
    //     Assert.Null(retrieved);
    // }
    //
    // [Fact]
    // public async Task MetadataReflectsStoredSize()
    // {
    //     var storage = new InMemoryBlobStorage();
    //     var data = new byte[128];
    //     await using var source = new MemoryStream(data);
    //     var id = await storage.PutAsync(source, new BlobMetadata("application/octet-stream", data.Length));
    //
    //     Assert.True(storage.TryGetMetadata(id, out var stored));
    //     Assert.NotNull(stored);
    //     Assert.Equal(data.Length, stored!.ContentLength);
    // }
}
