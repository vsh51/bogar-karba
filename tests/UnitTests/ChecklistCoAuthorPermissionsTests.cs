using Application.Interfaces;
using Application.Options;
using Domain.Entities;
using Infrastructure.Caching;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace UnitTests;

public class ChecklistCoAuthorPermissionsTests
{
    private const string CoAuthorUserId = "co-author-1";
    private const string OtherUserId = "other-user";

    [Fact]
    public void ChecklistCoAuthor_HasExpectedDefaults()
    {
        var coAuthor = new ChecklistCoAuthor();

        Assert.Equal(Guid.Empty, coAuthor.ChecklistId);
        Assert.Equal(string.Empty, coAuthor.UserId);
        Assert.False(coAuthor.WritingPermission);
        Assert.False(coAuthor.DeletePermission);
        Assert.False(coAuthor.ActivateTogglePermission);
        Assert.Null(coAuthor.Checklist);
    }

    [Fact]
    public async Task GrantCoAuthorAsync_PassesEntityThroughToInner()
    {
        var innerMock = new Mock<IChecklistRepository>();
        var sut = CreateWrite(innerMock.Object);

        var coAuthor = new ChecklistCoAuthor
        {
            ChecklistId = Guid.NewGuid(),
            UserId = CoAuthorUserId,
            WritingPermission = true,
            DeletePermission = false,
            ActivateTogglePermission = true,
        };

        await sut.GrantCoAuthorAsync(coAuthor);

        innerMock.Verify(r => r.GrantCoAuthorAsync(coAuthor), Times.Once);
    }

    [Fact]
    public async Task RevokeCoAuthorAsync_PassesArgumentsThroughToInner()
    {
        var checklistId = Guid.NewGuid();
        var innerMock = new Mock<IChecklistRepository>();
        var sut = CreateWrite(innerMock.Object);

        await sut.RevokeCoAuthorAsync(checklistId, CoAuthorUserId);

        innerMock.Verify(r => r.RevokeCoAuthorAsync(checklistId, CoAuthorUserId), Times.Once);
    }

    [Fact]
    public async Task RevokeAllCoAuthorsAsync_PassesChecklistIdThroughToInner()
    {
        var checklistId = Guid.NewGuid();
        var innerMock = new Mock<IChecklistRepository>();
        var sut = CreateWrite(innerMock.Object);

        await sut.RevokeAllCoAuthorsAsync(checklistId);

        innerMock.Verify(r => r.RevokeAllCoAuthorsAsync(checklistId), Times.Once);
    }

    [Fact]
    public async Task RevokeAllAccessesAsync_PassesChecklistIdThroughToInner()
    {
        var checklistId = Guid.NewGuid();
        var innerMock = new Mock<IChecklistRepository>();
        var sut = CreateWrite(innerMock.Object);

        await sut.RevokeAllAccessesAsync(checklistId);

        innerMock.Verify(r => r.RevokeAllAccessesAsync(checklistId), Times.Once);
    }

    [Fact]
    public async Task GrantCoAuthorAsync_PropagatesInnerExceptions()
    {
        var innerMock = new Mock<IChecklistRepository>();
        innerMock
            .Setup(r => r.GrantCoAuthorAsync(It.IsAny<ChecklistCoAuthor>()))
            .ThrowsAsync(new InvalidOperationException("DB Error"));

        var sut = CreateWrite(innerMock.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.GrantCoAuthorAsync(new ChecklistCoAuthor
            {
                ChecklistId = Guid.NewGuid(),
                UserId = CoAuthorUserId,
            }));
    }

    [Fact]
    public async Task IsCoAuthorAsync_ReturnsInnerResult()
    {
        var checklistId = Guid.NewGuid();
        var innerMock = new Mock<IChecklistReadOnlyRepository>();
        innerMock
            .Setup(r => r.IsCoAuthorAsync(checklistId, CoAuthorUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        innerMock
            .Setup(r => r.IsCoAuthorAsync(checklistId, OtherUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var sut = CreateRead(innerMock.Object);

        Assert.True(await sut.IsCoAuthorAsync(checklistId, CoAuthorUserId));
        Assert.False(await sut.IsCoAuthorAsync(checklistId, OtherUserId));

        innerMock.Verify(
            r => r.IsCoAuthorAsync(checklistId, CoAuthorUserId, It.IsAny<CancellationToken>()),
            Times.Once);
        innerMock.Verify(
            r => r.IsCoAuthorAsync(checklistId, OtherUserId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetCoAuthorIdsAsync_ReturnsInnerListWithoutCaching()
    {
        var checklistId = Guid.NewGuid();
        var ids = new List<string> { CoAuthorUserId, OtherUserId };
        var innerMock = new Mock<IChecklistReadOnlyRepository>();
        innerMock
            .Setup(r => r.GetCoAuthorIdsAsync(checklistId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ids);

        var sut = CreateRead(innerMock.Object);

        var first = await sut.GetCoAuthorIdsAsync(checklistId);
        var second = await sut.GetCoAuthorIdsAsync(checklistId);

        Assert.Equal(ids, first);
        Assert.Equal(ids, second);
        innerMock.Verify(
            r => r.GetCoAuthorIdsAsync(checklistId, It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task HasWritingPermissionAsync_ReflectsInnerResult()
    {
        var checklistId = Guid.NewGuid();
        var innerMock = new Mock<IChecklistReadOnlyRepository>();
        innerMock
            .Setup(r => r.HasWritingPermissionAsync(checklistId, CoAuthorUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        innerMock
            .Setup(r => r.HasWritingPermissionAsync(checklistId, OtherUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var sut = CreateRead(innerMock.Object);

        Assert.True(await sut.HasWritingPermissionAsync(checklistId, CoAuthorUserId));
        Assert.False(await sut.HasWritingPermissionAsync(checklistId, OtherUserId));
    }

    [Fact]
    public async Task HasWritingPermissionAsync_ForwardsCancellationToken()
    {
        var checklistId = Guid.NewGuid();
        using var cts = new CancellationTokenSource();
        var innerMock = new Mock<IChecklistReadOnlyRepository>();
        innerMock
            .Setup(r => r.HasWritingPermissionAsync(checklistId, CoAuthorUserId, cts.Token))
            .ReturnsAsync(true);

        var sut = CreateRead(innerMock.Object);

        var result = await sut.HasWritingPermissionAsync(checklistId, CoAuthorUserId, cts.Token);

        Assert.True(result);
        innerMock.Verify(
            r => r.HasWritingPermissionAsync(checklistId, CoAuthorUserId, cts.Token),
            Times.Once);
    }

    private static CachedChecklistRepository CreateWrite(IChecklistRepository inner) =>
        new(
            inner,
            new MemoryCache(new MemoryCacheOptions()),
            new Mock<ILogger<CachedChecklistRepository>>().Object);

    private static CachedChecklistReadOnlyRepository CreateRead(IChecklistReadOnlyRepository inner) =>
        new(
            inner,
            new MemoryCache(new MemoryCacheOptions()),
            Options.Create(new CacheOptions { PublishedChecklistMinutes = 10 }),
            new Mock<ILogger<CachedChecklistReadOnlyRepository>>().Object);
}
