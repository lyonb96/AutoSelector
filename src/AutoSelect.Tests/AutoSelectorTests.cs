using System.ComponentModel.DataAnnotations.Schema;

namespace AutoSelect.Tests;

public class AutoSelectorTests
{
    [Test]
    public void AutoSelector_SimpleMap_ReturnsExpression()
    {
        var map = AutoSelector<SimpleEntity, SimpleModel>.Map();
        // Setup a test "entity" to map
        var testEntity = new SimpleEntity
        {
            Id = 1,
            Name = "Test",
            Other = "This should not be mapped",
        };
        // Compile and run the mapper
        var mapper = map.Compile();
        var testModel = mapper(testEntity);
        Assert.Multiple(() =>
        {
            Assert.That(testModel.Id, Is.EqualTo(testEntity.Id));
            Assert.That(testModel.FullName, Is.EqualTo(testEntity.Name));
            // Other on the model has a [NotMapped] attribute, so it should be null even if the entity value is not
            Assert.That(testModel.Other, Is.Null);
        });
    }

    [Test]
    public void AutoSelector_AdvancedMap_ReturnsExpression()
    {
        var map = AutoSelector<AdvancedEntity, AdvancedModel>.Map();
        // Setup a test "entity" to map
        var testEntity = new AdvancedEntity
        {
            Id = 1,
            Name = "Test",
            Tags =
            [
                new("Key", "Value"),
            ],
            SimpleEntityId = 2,
            SimpleEntity = new()
            {
                Id = 2,
                Name = "Test Nested Object",
                Other = "Hello!",
            },
            Entities =
            [
                new()
                {
                    Id = 3,
                    Name = "Test Collection Object",
                    Other = "Hello again!",
                },
                new()
                {
                    Id = 4,
                    Name = "This is fun!",
                    Other = null,
                },
            ],
        };
        // Compile and run the mapper
        var mapper = map.Compile();
        var testModel = mapper(testEntity);
        Assert.Multiple(() =>
        {
            // Test the properties that should directly assign with no special mapping
            Assert.That(testModel.Id, Is.EqualTo(testEntity.Id));
            Assert.That(testModel.Name, Is.EqualTo(testEntity.Name));
            Assert.That(testModel.SimpleEntityId, Is.EqualTo(testEntity.SimpleEntityId));
            Assert.That(testModel.Tags, Has.Count.EqualTo(testEntity.Tags.Count));
            // Test the nested object properties
            Assert.That(testModel.SimpleEntity, Is.Not.Null);
            Assert.That(testModel.SimpleEntity?.Id, Is.EqualTo(testEntity.SimpleEntity.Id));
            Assert.That(testModel.SimpleEntity?.FullName, Is.EqualTo(testEntity.SimpleEntity.Name));
            // The nested object has a [NotMapped] on this property, which should be respected even if it's nested
            Assert.That(testModel.SimpleEntity?.Other, Is.Null);
            // Test the flattening logic
            Assert.That(testModel.SimpleEntityName, Is.EqualTo(testEntity.SimpleEntity.Name));
            // Now test the collection mappings
            Assert.That(testModel.EntityIds, Has.Count.EqualTo(testEntity.Entities.Count));
            Assert.That(testModel.EntityIds?.First(), Is.EqualTo(testEntity.Entities.First().Id));
            Assert.That(testModel.EntityIds?.Last(), Is.EqualTo(testEntity.Entities.Last().Id));
            Assert.That(testModel.EntityNames, Has.Length.EqualTo(testEntity.Entities.Count));
            Assert.That(testModel.EntityNames?.First(), Is.EqualTo(testEntity.Entities.First().Name));
            Assert.That(testModel.EntityNames?.Last(), Is.EqualTo(testEntity.Entities.Last().Name));
            Assert.That(testModel.Entities, Has.Count.EqualTo(testEntity.Entities.Count));
            var firstEntity = testEntity.Entities.First();
            var firstModel = testModel.Entities?.First();
            Assert.That(firstModel, Is.Not.Null);
            Assert.That(firstModel?.Id, Is.EqualTo(firstEntity.Id));
            Assert.That(firstModel?.FullName, Is.EqualTo(firstEntity.Name));
            // The nested object has a [NotMapped] on this property, which should be respected even if it's nested
            Assert.That(firstModel?.Other, Is.Null);
            var lastEntity = testEntity.Entities.Last();
            var lastModel = testModel.Entities?.Last();
            Assert.That(lastModel, Is.Not.Null);
            Assert.That(lastModel?.Id, Is.EqualTo(lastEntity.Id));
            Assert.That(lastModel?.FullName, Is.EqualTo(lastEntity.Name));
            // The nested object has a [NotMapped] on this property, which should be respected even if it's nested
            Assert.That(lastModel?.Other, Is.Null);
        });
    }
}

public class SimpleEntity
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public string? Other { get; set; }
}

public class AdvancedEntity
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public List<KeyValuePair<string, string>> Tags { get; set; } = new();

    public int SimpleEntityId { get; set; }

    public SimpleEntity? SimpleEntity { get; set; }

    public ICollection<SimpleEntity> Entities { get; set; } = new HashSet<SimpleEntity>();
}

public class SimpleModel
{
    public int Id { get; set; }

    [MapFrom("Name")]
    public string? FullName { get; set; }

    [NotMapped]
    public string? Other { get; set; }
}

public class AdvancedModel
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public List<KeyValuePair<string, string>>? Tags { get; set; }

    public int SimpleEntityId { get; set; }

    public string? SimpleEntityName { get; set; }

    public SimpleModel? SimpleEntity { get; set; }

    [MapFrom("EntitiesId")]
    public List<int>? EntityIds { get; set; }

    [MapFrom("EntitiesName")]
    public string?[]? EntityNames { get; set; }

    public ICollection<SimpleModel>? Entities { get; set; }
}
