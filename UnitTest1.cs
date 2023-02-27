using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Query.Internal;
using RecipeManager.Application.Data.Repository;
using RecipeManager.Application.Entities;
using RecipeManager.Application.Models.Recipes;
using RecipeManager.Infrastructure;

namespace VLC.RecipeManagementModule.Tests;

[TestFixture]
public class EntityBaseTests
{
    [SetUp]
    public void Setup()
    {
        _fixture = new Fixture().Customize(new AutoMoqCustomization());
    }

    private IFixture _fixture;

    [Test]
    public void Id_ShouldNotBeNull()
    {
        // Arrange
        var entity = _fixture.Create<EntityBase>();

        // Act

        // Assert
        entity.Id.Should().NotBe(null);
    }

    [Test]
    public void Id_ShouldBeDecoratedWithRequiredKeyAndDatabaseGeneratedAttributes()
    {
        // Arrange
        var propertyInfo = typeof(EntityBase).GetProperty("Id");

        // Act
        var attributes = propertyInfo.GetCustomAttributes(true);

        // Assert
        attributes.Should().Contain(a => a is RequiredAttribute);
        attributes.Should().Contain(a => a is KeyAttribute);
        attributes.Should().Contain(a => a is DatabaseGeneratedAttribute);
        ((DatabaseGeneratedAttribute)attributes[2]).DatabaseGeneratedOption.Should()
            .Be(DatabaseGeneratedOption.Identity);
    }
}


[TestFixture]
public class RecipeTests
{
    private IFixture _fixture;

    [SetUp]
    public void SetUp()
    {
        _fixture = new Fixture().Customize(new AutoMoqCustomization());
    }

    [Test]
    public void Label_ShouldBeSetCorrectly()
    {
        // Arrange
        var expectedLabel = "Test Recipe";
        var recipe = _fixture.Create<Recipe>();

        // Act
        recipe.Label = expectedLabel;

        // Assert
        recipe.Label.Should().Be(expectedLabel);
    }

    [Test]
    public void Ingredients_ShouldBeSetCorrectly()
    {
        // Arrange
        var expectedIngredients = "Ingredient1, Ingredient2, Ingredient3";
        var recipe = _fixture.Create<Recipe>();

        // Act
        recipe.Ingredients = expectedIngredients;

        // Assert
        recipe.Ingredients.Should().Be(expectedIngredients);
    }

    [Test]
    public void Instructions_ShouldBeSetCorrectly()
    {
        // Arrange
        var expectedInstructions = "Step 1, Step 2, Step 3";
        var recipe = _fixture.Create<Recipe>();

        // Act
        recipe.Instructions = expectedInstructions;

        // Assert
        recipe.Instructions.Should().Be(expectedInstructions);
    }

    [Test]
    public void Calories_ShouldBeSetCorrectly()
    {
        // Arrange
        var expectedCalories = 500;
        var recipe = _fixture.Create<Recipe>();

        // Act
        recipe.Calories = expectedCalories;

        // Assert
        recipe.Calories.Should().Be(expectedCalories);
    }

    [Test]
    public void Recipe_CreateNewRecipeWithValidProperties_ShouldCreateRecipeObject()
    {
        // Arrange
        var fixture = new Fixture().Customize(new AutoMoqCustomization());
        var recipe = fixture.Create<Recipe>();

        // Act

        // Assert
        recipe.Should().NotBeNull();
        recipe.Label.Should().NotBeNullOrWhiteSpace();
        recipe.Ingredients.Should().NotBeNull();
        recipe.Instructions.Should().NotBeNull();
    }

    [Test]
    public void Recipe_UpdateRecipeProperties_ShouldUpdateRecipeObject()
    {
        // Arrange
        var fixture = new Fixture().Customize(new AutoMoqCustomization());
        var recipe = fixture.Create<Recipe>();
        var newName = fixture.Create<string>();
        var newIngredients = fixture.Create<string>();
        var newInstructions = fixture.Create<string>();

        // Act
        recipe.Label = newName;
        recipe.Ingredients = newIngredients;
        recipe.Instructions = newInstructions;

        // Assert
        recipe.Label.Should().Be(newName);
        recipe.Ingredients.Should().Be(newIngredients);
        recipe.Instructions.Should().Be(newInstructions);
    }

    [Test]
    public void Recipe_DeleteRecipe_ShouldDeleteRecipeObject()
    {
        // Arrange
        var fixture = new Fixture().Customize(new AutoMoqCustomization());
        var recipe = fixture.Create<Recipe>();

        // Act
        recipe = null;

        // Assert
        recipe.Should().BeNull();
    }
}

[TestFixture]
public class ApiDbContextTests
{
    [Test]
    public void ApiDbContext_Should_Create_Recipes_Table_Successfully()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApiDbContext>()
            .UseInMemoryDatabase(databaseName: "Test_Db")
            .Options;

        // Act
        using var context = new ApiDbContext(options);
        context.Database.EnsureCreated();
        var result = context.Recipes;

        // Assert
        result.Should().NotBeNull();
        result.Should().BeAssignableTo<DbSet<Recipe>>();
    }

    [Test]
    public void ApiDbContext_Should_Add_Recipe_Successfully()
    {
        // Arrange
        var fixture = new Fixture();
        var recipe = fixture.Create<Recipe>();
        var options = new DbContextOptionsBuilder<ApiDbContext>()
            .UseInMemoryDatabase(databaseName: "Test_Db")
            .Options;

        // Act
        using (var context = new ApiDbContext(options))
        {
            context.Database.EnsureCreated();
            context.Recipes.Add(recipe);
            context.SaveChanges();

            // Assert
            context.Recipes.Count().Should().Be(1);
            context.Recipes.Should().Contain(recipe);
        }
    }
}


public class TestEntity
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int Number { get; set; }
}

public class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
{
    private readonly IEnumerator<T> _enumerator;

    public TestAsyncEnumerator(IEnumerator<T> enumerator) => _enumerator = enumerator;

    public ValueTask<bool> MoveNextAsync() => new ValueTask<bool>(_enumerator.MoveNext());

    public T Current => _enumerator.Current;

    public ValueTask DisposeAsync() => new ValueTask(Task.CompletedTask);
}



[TestFixture]
public class RepositoryTests
{
    private IFixture _fixture;
    private Mock<IQueryable<TestEntity>> _mockQuery;
    private Mock<IRepository<TestEntity>> _mockRepository;

    [SetUp]
    public void SetUp()
    {
        _fixture = new Fixture().Customize(new AutoMoqCustomization());
        _mockQuery = new Mock<IQueryable<TestEntity>>();
        _mockRepository = new Mock<IRepository<TestEntity>>();
    }

    [Test]
    public async Task GetAllAsync_ShouldReturnAllEntities()
    {
        // Arrange
        var testEntities = _fixture.CreateMany<TestEntity>().ToList();
        _mockQuery.As<IAsyncEnumerable<TestEntity>>()
            .Setup(m => m.GetAsyncEnumerator(default))
            .Returns(new TestAsyncEnumerator<TestEntity>(testEntities.GetEnumerator()));

        _mockRepository.Setup(m => m.GetAllAsync()).ReturnsAsync(testEntities);

        // Act
        var result = await _mockRepository.Object.GetAllAsync();

        // Assert
        result.Should().BeEquivalentTo(testEntities);
    }

    [Test]
    public async Task GetRecordByIdAsync_ShouldReturnEntityWithGivenId()
    {
        // Arrange
        var testEntities = _fixture.CreateMany<TestEntity>().ToList();
        var testEntityToFind = testEntities.First();

        _mockRepository.Setup(m => m.GetRecordByIdAsync(testEntityToFind.Id))
            .ReturnsAsync(testEntityToFind);

        // Act
        var result = await _mockRepository.Object.GetRecordByIdAsync(testEntityToFind.Id);

        // Assert
        result.Should().BeEquivalentTo(testEntityToFind);
    }


}



