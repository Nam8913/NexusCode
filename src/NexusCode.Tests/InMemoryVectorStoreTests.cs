using NexusCode.VectorStore;
using Xunit;

namespace NexusCode.Tests;

public class InMemoryVectorStoreTests
{
    [Fact]
    public async Task CreateCollection_And_ListCollections()
    {
        var store = new InMemoryVectorStore();
        await store.CreateCollectionAsync("test", 3);

        var collections = await store.ListCollectionsAsync();
        Assert.Contains("test", collections);
    }

    [Fact]
    public async Task Upsert_And_Search_Works()
    {
        var store = new InMemoryVectorStore();
        await store.CreateCollectionAsync("vectors", 3);

        var point = new VectorPoint
        {
            Id = "1",
            Vector = [1.0f, 0.0f, 0.0f],
            Payload = new() { ["name"] = "test" }
        };
        await store.UpsertAsync("vectors", point);

        var results = await store.SearchAsync("vectors", [1.0f, 0.0f, 0.0f], limit: 5);
        Assert.Single(results);
        Assert.Equal("1", results[0].Id);
        Assert.True(results[0].Score > 0.99f);
    }

    [Fact]
    public async Task Search_ReturnsSimilarResults()
    {
        var store = new InMemoryVectorStore();
        await store.CreateCollectionAsync("vectors", 3);

        await store.UpsertAsync("vectors", new VectorPoint { Id = "a", Vector = [1.0f, 0.0f, 0.0f] });
        await store.UpsertAsync("vectors", new VectorPoint { Id = "b", Vector = [0.0f, 1.0f, 0.0f] });
        await store.UpsertAsync("vectors", new VectorPoint { Id = "c", Vector = [0.9f, 0.1f, 0.0f] });

        var results = await store.SearchAsync("vectors", [1.0f, 0.0f, 0.0f], limit: 3);

        Assert.Equal("a", results[0].Id);
        Assert.Equal("c", results[1].Id);
        Assert.Equal("b", results[2].Id);
    }

    [Fact]
    public async Task Delete_RemovesPoint()
    {
        var store = new InMemoryVectorStore();
        await store.CreateCollectionAsync("vectors", 3);
        await store.UpsertAsync("vectors", new VectorPoint { Id = "1", Vector = [1.0f, 0.0f, 0.0f] });

        await store.DeleteAsync("vectors", "1");

        var results = await store.SearchAsync("vectors", [1.0f, 0.0f, 0.0f]);
        Assert.Empty(results);
    }

    [Fact]
    public async Task DeleteCollection_RemovesAll()
    {
        var store = new InMemoryVectorStore();
        await store.CreateCollectionAsync("to_delete", 3);
        await store.UpsertAsync("to_delete", new VectorPoint { Id = "1", Vector = [1.0f, 0.0f, 0.0f] });

        await store.DeleteCollectionAsync("to_delete");

        var collections = await store.ListCollectionsAsync();
        Assert.DoesNotContain("to_delete", collections);
    }

    [Fact]
    public async Task Scroll_ReturnsPoints()
    {
        var store = new InMemoryVectorStore();
        await store.CreateCollectionAsync("vectors", 3);
        await store.UpsertAsync("vectors", new VectorPoint { Id = "1", Vector = [1.0f, 0.0f, 0.0f] });
        await store.UpsertAsync("vectors", new VectorPoint { Id = "2", Vector = [0.0f, 1.0f, 0.0f] });

        var points = await store.ScrollAsync("vectors", limit: 10);
        Assert.Equal(2, points.Count);
    }

    [Fact]
    public async Task IsAvailable_ReturnsTrue()
    {
        var store = new InMemoryVectorStore();
        Assert.True(await store.IsAvailableAsync());
    }
}
