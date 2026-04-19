using Application.Interfaces;
using Application.Options;
using Domain.Entities;
using Infrastructure.Caching;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace UnitTests;

public class CachedChecklistReadOnlyRepositoryTests
{
    [Fact]
    public async Task GetPublishedChecklistAsync_WhenCacheMiss_FetchesFromInnerAndCaches()
    {
        var id = Guid.NewGuid();
        var checklist = new Checklist { Id = id, Title = "t", Description = "d" };
        var innerMock = new Mock<IChecklistReadOnlyRepository>();
        innerMock
            .Setup(r => r.GetPublishedChecklistAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(checklist);

        var sut = CreateSut(innerMock.Object, out _);

        var first = await sut.GetPublishedChecklistAsync(id);
        var second = await sut.GetPublishedChecklistAsync(id);

        Assert.Same(checklist, first);
        Assert.Same(checklist, second);
        innerMock.Verify(
            r => r.GetPublishedChecklistAsync(id, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetPublishedChecklistAsync_WhenInnerReturnsNull_DoesNotCacheAndRefetches()
    {
        var id = Guid.NewGuid();
        var innerMock = new Mock<IChecklistReadOnlyRepository>();
        innerMock
            .Setup(r => r.GetPublishedChecklistAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Checklist?)null);

        var sut = CreateSut(innerMock.Object, out _);

        await sut.GetPublishedChecklistAsync(id);
        await sut.GetPublishedChecklistAsync(id);

        innerMock.Verify(
            r => r.GetPublishedChecklistAsync(id, It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task GetPublishedChecklistAsync_DistinctIds_AreCachedIndependently()
    {
        var idA = Guid.NewGuid();
        var idB = Guid.NewGuid();
        var a = new Checklist { Id = idA, Title = "a", Description = "a" };
        var b = new Checklist { Id = idB, Title = "b", Description = "b" };

        var innerMock = new Mock<IChecklistReadOnlyRepository>();
        innerMock
            .Setup(r => r.GetPublishedChecklistAsync(idA, It.IsAny<CancellationToken>()))
            .ReturnsAsync(a);
        innerMock
            .Setup(r => r.GetPublishedChecklistAsync(idB, It.IsAny<CancellationToken>()))
            .ReturnsAsync(b);

        var sut = CreateSut(innerMock.Object, out _);

        Assert.Same(a, await sut.GetPublishedChecklistAsync(idA));
        Assert.Same(b, await sut.GetPublishedChecklistAsync(idB));
        Assert.Same(a, await sut.GetPublishedChecklistAsync(idA));
        Assert.Same(b, await sut.GetPublishedChecklistAsync(idB));

        innerMock.Verify(
            r => r.GetPublishedChecklistAsync(idA, It.IsAny<CancellationToken>()),
            Times.Once);
        innerMock.Verify(
            r => r.GetPublishedChecklistAsync(idB, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_PassesThroughWithoutCaching()
    {
        var id = Guid.NewGuid();
        var checklist = new Checklist { Id = id, Title = "t", Description = "d" };
        var innerMock = new Mock<IChecklistReadOnlyRepository>();
        innerMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(checklist);

        var sut = CreateSut(innerMock.Object, out _);

        await sut.GetByIdAsync(id);
        await sut.GetByIdAsync(id);

        innerMock.Verify(r => r.GetByIdAsync(id), Times.Exactly(2));
    }

    [Fact]
    public async Task GetByIdWithSectionsAsync_PassesThroughWithoutCaching()
    {
        var id = Guid.NewGuid();
        var innerMock = new Mock<IChecklistReadOnlyRepository>();
        innerMock
            .Setup(r => r.GetByIdWithSectionsAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Checklist { Id = id, Title = "t", Description = "d" });

        var sut = CreateSut(innerMock.Object, out _);

        await sut.GetByIdWithSectionsAsync(id);
        await sut.GetByIdWithSectionsAsync(id);

        innerMock.Verify(
            r => r.GetByIdWithSectionsAsync(id, It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task GetByUserIdAsync_PassesThroughWithoutCaching()
    {
        const string userId = "user-1";
        var innerMock = new Mock<IChecklistReadOnlyRepository>();
        innerMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync([]);

        var sut = CreateSut(innerMock.Object, out _);

        await sut.GetByUserIdAsync(userId);
        await sut.GetByUserIdAsync(userId);

        innerMock.Verify(r => r.GetByUserIdAsync(userId), Times.Exactly(2));
    }

    private static CachedChecklistReadOnlyRepository CreateSut(
        IChecklistReadOnlyRepository inner,
        out IMemoryCache cache)
    {
        cache = new MemoryCache(new MemoryCacheOptions());
        var options = Options.Create(new CacheOptions { PublishedChecklistMinutes = 10 });
        var logger = new Mock<ILogger<CachedChecklistReadOnlyRepository>>().Object;
        return new CachedChecklistReadOnlyRepository(inner, cache, options, logger);
    }
}
