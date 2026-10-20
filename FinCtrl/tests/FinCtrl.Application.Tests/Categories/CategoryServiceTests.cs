using FinCtrl.Application.Categories;
using FinCtrl.Common.Application;
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

    [Fact]
    public async Task GetById_UnknownCategory_ThrowsNotFoundException()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetById(99));
    }

    [Fact]
    public async Task Create_EmptyColor_UsesDefaultColor()
    {
        var category = new Category { CategoryName = "Еда", Color = "" };

        await _service.Create(category);

        await _repository.Received(1).Create(Arg.Is<Category>(c => c.Color == Category.DefaultColor));
    }

    [Theory]
    [InlineData("", "#ffffff")]
    [InlineData("  ", "#ffffff")]
    [InlineData("Еда", "красный")]
    [InlineData("Еда", "#ffffff00")]
    public async Task Create_InvalidNameOrColor_ThrowsValidationException(string name, string color)
    {
        await Assert.ThrowsAsync<ValidationException>(() =>
            _service.Create(new Category { CategoryName = name, Color = color }));

        await _repository.DidNotReceiveWithAnyArgs().Create(default!);
    }

    [Fact]
    public async Task Create_UnknownParent_ThrowsValidationException()
    {
        await Assert.ThrowsAsync<ValidationException>(() =>
            _service.Create(new Category { CategoryName = "Еда", ParentCategoryId = 99 }));

        await _repository.DidNotReceiveWithAnyArgs().Create(default!);
    }

    [Fact]
    public async Task Update_UnknownCategory_ThrowsNotFoundException()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _service.Update(new Category(99) { CategoryName = "Еда" }));

        await _repository.DidNotReceiveWithAnyArgs().Update();
    }

    [Fact]
    public async Task Update_ValidParent_Saves()
    {
        GivenStoredCategories(Cat(1, null, 0), Cat(2, null, 0));

        await _service.Update(new Category(2) { CategoryName = "Кафе", ParentCategoryId = 1 });

        await _repository.Received(1).Update(Arg.Is<Category[]>(c => c.Length == 1 && c[0].ParentCategoryId == 1));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public async Task Update_ParentIsSelfOrDescendant_ThrowsValidationException(int parentId)
    {
        // 1 → 2 → 3: родителем категории 1 не может быть ни она сама, ни внук 3
        GivenStoredCategories(Cat(1, null, 0), Cat(2, 1, 0), Cat(3, 2, 0));

        await Assert.ThrowsAsync<ValidationException>(() =>
            _service.Update(new Category(1) { CategoryName = "Еда", ParentCategoryId = parentId }));

        await _repository.DidNotReceiveWithAnyArgs().Update();
    }

    [Fact]
    public async Task Update_UnknownParent_ThrowsValidationException()
    {
        GivenStoredCategories(Cat(1, null, 0));

        await Assert.ThrowsAsync<ValidationException>(() =>
            _service.Update(new Category(1) { CategoryName = "Еда", ParentCategoryId = 99 }));
    }

    [Fact]
    public async Task Delete_UnusedCategory_Deletes()
    {
        GivenStoredCategories(Cat(1, null, 0));
        _repository.GetUsage(1).Returns(new CategoryUsage(0, 0, 0));

        await _service.Delete(1);

        await _repository.Received(1).Delete(Arg.Is<Category[]>(c => c.Length == 1 && c[0].CategoryId == 1));
    }

    [Theory]
    [InlineData(1, 0, 0)]
    [InlineData(0, 2, 0)]
    [InlineData(0, 0, 3)]
    public async Task Delete_UsedCategory_ThrowsConflictException(int children, int sources, int payments)
    {
        GivenStoredCategories(Cat(1, null, 0));
        _repository.GetUsage(1).Returns(new CategoryUsage(children, sources, payments));

        await Assert.ThrowsAsync<ConflictException>(() => _service.Delete(1));

        await _repository.DidNotReceiveWithAnyArgs().Delete();
    }

    [Fact]
    public async Task Delete_UnknownCategory_ThrowsNotFoundException()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _service.Delete(99));

        await _repository.DidNotReceiveWithAnyArgs().Delete();
    }

    private void GivenStoredCategories(params Category[] categories)
    {
        SetCategories(categories);
        foreach (var category in categories)
        {
            _repository.GetById(category.CategoryId).Returns(category);
        }
    }

    private void SetCategories(params Category[] categories) =>
        _repository.GetAll().Returns(categories);

    private static Category Cat(int id, int? parentId, int orderIndex) => new(id)
    {
        CategoryName = $"Категория {id}",
        ParentCategoryId = parentId,
        OrderIndex = orderIndex
    };

    [Fact]
    public void GetAllChildCategories_RootWithDeepNesting_ReturnsAllDescendantsLevelByLevel()
    {
        var tree = BuildDeepTree();

        var result = _service.GetAllChildCategories(1, tree);

        Assert.Equal([2, 5, 3, 4], result.Select(c => c.CategoryId));
    }

    [Fact]
    public void GetAllChildCategories_NestedNode_ReturnsOnlyItsDescendants()
    {
        var tree = BuildDeepTree();

        var result = _service.GetAllChildCategories(2, tree);

        Assert.Equal([3, 4], result.Select(c => c.CategoryId));
    }

    [Fact]
    public void GetAllChildCategories_Leaf_ReturnsEmptyList()
    {
        var tree = BuildDeepTree();

        Assert.Empty(_service.GetAllChildCategories(4, tree));
        Assert.Empty(_service.GetAllChildCategories(6, tree));
    }

    [Fact]
    public void GetAllChildCategories_UnknownIdOrEmptyTree_ReturnsEmptyList()
    {
        Assert.Empty(_service.GetAllChildCategories(99, BuildDeepTree()));
        Assert.Empty(_service.GetAllChildCategories(1, new List<Category>()));
    }

    [Fact]
    public void GetAllChildCategories_Result_DoesNotIncludeCategoryItself()
    {
        var result = _service.GetAllChildCategories(1, BuildDeepTree());

        Assert.DoesNotContain(result, c => c.CategoryId == 1);
    }

    // 1 → (2 → (3 → 4), 5); 6 — отдельный корень без детей.
    private static List<Category> BuildDeepTree()
    {
        var level3 = new Category(3) { ParentCategoryId = 2 };
        level3.Children.Add(new Category(4) { ParentCategoryId = 3 });
        var level2 = new Category(2) { ParentCategoryId = 1 };
        level2.Children.Add(level3);
        var root = new Category(1);
        root.Children.Add(level2);
        root.Children.Add(new Category(5) { ParentCategoryId = 1 });
        return new List<Category> { root, new Category(6) };
    }
}
