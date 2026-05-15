using Application.DTOs.User;
using Application.Interfaces;
using Application.UseCases.SearchUsers;
using Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace UnitTests;

public class SearchUsersQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithSearchTerm_ReturnsFilteredUsers()
    {
        var searchTerm = "test";
        var users = new List<UserSummaryDto>
        {
            new() { Id = "1", UserName = "testuser", Email = "test@example.com", Status = UserStatus.Active },
            new() { Id = "2", UserName = "another", Email = "another@example.com", Status = UserStatus.Active }
        };

        var repositoryMock = new Mock<IUserRepository>();
        repositoryMock.Setup(r => r.SearchUsersAsync(searchTerm))
            .ReturnsAsync(users);

        var sut = new SearchUsersQueryHandler(
            repositoryMock.Object,
            NullLogger<SearchUsersQueryHandler>.Instance);

        var result = await sut.HandleAsync(new SearchUsersQuery(searchTerm));

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value.Count);
        repositoryMock.Verify(r => r.SearchUsersAsync(searchTerm), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenNoUsersFound_ReturnsEmptyList()
    {
        var searchTerm = "nonexistent";
        var repositoryMock = new Mock<IUserRepository>();
        repositoryMock.Setup(r => r.SearchUsersAsync(searchTerm))
            .ReturnsAsync(new List<UserSummaryDto>());

        var sut = new SearchUsersQueryHandler(
            repositoryMock.Object,
            NullLogger<SearchUsersQueryHandler>.Instance);

        var result = await sut.HandleAsync(new SearchUsersQuery(searchTerm));

        Assert.True(result.Succeeded);
        Assert.Empty(result.Value!);
    }

    [Fact]
    public async Task HandleAsync_WithNullSearchTerm_CallsRepositoryWithNull()
    {
        var repositoryMock = new Mock<IUserRepository>();
        repositoryMock.Setup(r => r.SearchUsersAsync(null))
            .ReturnsAsync(new List<UserSummaryDto>());

        var sut = new SearchUsersQueryHandler(
            repositoryMock.Object,
            NullLogger<SearchUsersQueryHandler>.Instance);

        var result = await sut.HandleAsync(new SearchUsersQuery(null));

        Assert.True(result.Succeeded);
        repositoryMock.Verify(r => r.SearchUsersAsync(null), Times.Once);
    }
}
