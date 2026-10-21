using FinCtrl.Application.Categories;
using FinCtrl.Domain;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace FinCtrl.Application.Tests.Categories;

public class CategoryServiceTests
{
    private readonly ICategoryRepository _repository = Substitute.For<ICategoryRepository>();
    private readonly CategoryService _service;

    public CategoryServiceTests()
    {
        _service = new CategoryService(_repository, new FakeTimeProvider());
    }

    [Fact]
    public async Task GetCategoriesTree_FlatList_BuildsNestedTreeWithSingleQuery()
    {
        SetCategories(
            Cat(1, null, 0),
            Cat(2, 1, 0),
            Cat(3, 2, 0),
            Cat(4, null, 1));

        var tree = await _service.GetCategoriesTree();

        await _repository.Received(1).GetAll();
        Assert.Equal([1, 4], tree.Select(c => c.CategoryId));
        var child = Assert.Single(tree[0].Children);
        Assert.Equal(2, child.CategoryId);
        Assert.Equal(3, Assert.Single(child.Children).CategoryId);
        Assert.Empty(tree[1].Children);
    }

    [Fact]
    public async Task GetCategoriesTree_UnorderedList_OrdersRootsAndChildrenByOrderIndex()
    {
        SetCategories(
            Cat(10, null, 2),
            Cat(11, null, 1),
            Cat(12, 11, 5),
            Cat(13, 11, 3),
            Cat(14, 11, 3));

        var tree = await _service.GetCategoriesTree();

        Assert.Equal([11, 10], tree.Select(c => c.CategoryId));
        Assert.Equal([13, 14, 12], tree[0].Children.Select(c => c.CategoryId));
    }

    [Fact]
    public async Task GetCategoriesTree_ParentMissing_SkipsOrphan()
    {
        SetCategories(
            Cat(1, null, 0),
            Cat(2, 99, 0));

        var tree = await _service.GetCategoriesTree();

        var root = Assert.Single(tree);
        Assert.Equal(1, root.CategoryId);
        Assert.Empty(root.Children);
    }

    private void SetCategories(params Category[] categories) =>
        _repository.GetAll().Returns(categories);

    private static Category Cat(int id, int? parentId, int orderIndex) => new(id)
    {
        CategoryName = $"Категория {id}",
        ParentCategoryId = parentId,
        OrderIndex = orderIndex
    };
}
