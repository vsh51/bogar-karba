using Application.Interfaces;
using Application.Options;
using Domain.Entities;
using Infrastructure.Caching;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace UnitTests;

public class CachedChecklistRepositoryTests
{
    [Fact]
    public async Task DeleteAsync_EvictsCachedPublishedChecklistForSameId()
    {
        var id = Guid.NewGuid();
        var (read, write, readInnerMock, writeInnerMock) = CreatePair(id);

        await read.GetPublishedChecklistAsync(id);
        await read.GetPublishedChecklistAsync(id);

        readInnerMock.Verify(
            r => r.GetPublishedChecklistAsync(id, It.IsAny<CancellationToken>()),
            Times.Once);

        await write.DeleteAsync(id);

        await read.GetPublishedChecklistAsync(id);

        writeInnerMock.Verify(w => w.DeleteAsync(id), Times.Once);
        readInnerMock.Verify(
            r => r.GetPublishedChecklistAsync(id, It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task UpdateStatusAsync_EvictsCachedPublishedChecklistForSameId()
    {
        var id = Guid.NewGuid();
        var (read, write, readInnerMock, writeInnerMock) = CreatePair(id);

        await read.GetPublishedChecklistAsync(id);

        await write.UpdateStatusAsync(id, ChecklistStatus.Draft);

        await read.GetPublishedChecklistAsync(id);

        writeInnerMock.Verify(
            w => w.UpdateStatusAsync(id, ChecklistStatus.Draft),
            Times.Once);
        readInnerMock.Verify(
            r => r.GetPublishedChecklistAsync(id, It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task DeleteAsync_DoesNotEvictUnrelatedChecklist()
    {
        var idA = Guid.NewGuid();
        var idB = Guid.NewGuid();

        var readInnerMock = new Mock<IChecklistReadOnlyRepository>();
        readInnerMock
            .Setup(r => r.GetPublishedChecklistAsync(idA, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Checklist { Id = idA, Title = "a", Description = "a" });
        readInnerMock
            .Setup(r => r.GetPublishedChecklistAsync(idB, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Checklist { Id = idB, Title = "b", Description = "b" });

        var writeInnerMock = new Mock<IChecklistRepository>();

        var cache = new MemoryCache(new MemoryCacheOptions());
        var read = new CachedChecklistReadOnlyRepository(
            readInnerMock.Object,
            cache,
            Options.Create(new CacheOptions { PublishedChecklistMinutes = 10 }),
            new Mock<ILogger<CachedChecklistReadOnlyRepository>>().Object);
        var write = new CachedChecklistRepository(
            writeInnerMock.Object,
            cache,
            new Mock<ILogger<CachedChecklistRepository>>().Object);

        await read.GetPublishedChecklistAsync(idA);
        await read.GetPublishedChecklistAsync(idB);

        await write.DeleteAsync(idA);

        await read.GetPublishedChecklistAsync(idA);
        await read.GetPublishedChecklistAsync(idB);

        readInnerMock.Verify(
            r => r.GetPublishedChecklistAsync(idA, It.IsAny<CancellationToken>()),
            Times.Exactly(2));
        readInnerMock.Verify(
            r => r.GetPublishedChecklistAsync(idB, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AddAsync_DelegatesToInnerAndLeavesCacheUntouched()
    {
        var id = Guid.NewGuid();
        var (read, write, readInnerMock, writeInnerMock) = CreatePair(id);

        await read.GetPublishedChecklistAsync(id);

        var newChecklist = new Checklist { Id = Guid.NewGuid(), Title = "x", Description = "y" };
        await write.AddAsync(newChecklist);

        await read.GetPublishedChecklistAsync(id);

        writeInnerMock.Verify(w => w.AddAsync(newChecklist), Times.Once);
        readInnerMock.Verify(
            r => r.GetPublishedChecklistAsync(id, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task NonMutatingMethods_PassThroughToInner()
    {
        var innerMock = new Mock<IChecklistRepository>();
        innerMock.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        innerMock.Setup(r => r.GetTotalCountAsync()).ReturnsAsync(7);
        innerMock
            .Setup(r => r.GetCountByStatusAsync(ChecklistStatus.Published))
            .ReturnsAsync(3);

        var sut = new CachedChecklistRepository(
            innerMock.Object,
            new MemoryCache(new MemoryCacheOptions()),
            new Mock<ILogger<CachedChecklistRepository>>().Object);

        await sut.GetAllAsync();
        var total = await sut.GetTotalCountAsync();
        var published = await sut.GetCountByStatusAsync(ChecklistStatus.Published);

        Assert.Equal(7, total);
        Assert.Equal(3, published);
        innerMock.Verify(r => r.GetAllAsync(), Times.Once);
        innerMock.Verify(r => r.GetTotalCountAsync(), Times.Once);
        innerMock.Verify(r => r.GetCountByStatusAsync(ChecklistStatus.Published), Times.Once);
    }

    private static (
        CachedChecklistReadOnlyRepository Read,
        CachedChecklistRepository Write,
        Mock<IChecklistReadOnlyRepository> ReadInnerMock,
        Mock<IChecklistRepository> WriteInnerMock) CreatePair(Guid id)
    {
        var checklist = new Checklist { Id = id, Title = "t", Description = "d" };

        var readInnerMock = new Mock<IChecklistReadOnlyRepository>();
        readInnerMock
            .Setup(r => r.GetPublishedChecklistAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(checklist);

        var writeInnerMock = new Mock<IChecklistRepository>();

        var cache = new MemoryCache(new MemoryCacheOptions());
        var read = new CachedChecklistReadOnlyRepository(
            readInnerMock.Object,
            cache,
            Options.Create(new CacheOptions { PublishedChecklistMinutes = 10 }),
            new Mock<ILogger<CachedChecklistReadOnlyRepository>>().Object);
        var write = new CachedChecklistRepository(
            writeInnerMock.Object,
            cache,
            new Mock<ILogger<CachedChecklistRepository>>().Object);

        return (read, write, readInnerMock, writeInnerMock);
    }
}
