using System.Linq.Expressions;

namespace AutoSelect.Tests;

public class PathSelectorTests
{
    [Test]
    public void SimpleProperty_WithValidPath_ReturnsExpression()
    {
        var source = Expression.Parameter(typeof(SampleEntity));
        // Should expand to "src.Sample"
        var result = PathSelector.Select(source, nameof(SampleEntity.Sample));
        Assert.That(result.Type, Is.EqualTo(typeof(string)));
        // Test that it returns the correct value if we compile it and run it
        var lambda = Expression.Lambda<Func<SampleEntity, string>>(result, source).Compile();
        var sample = new SampleEntity
        {
            Sample = "Value",
        };
        var extractedValue = lambda(sample);
        Assert.That(extractedValue, Is.EqualTo(sample.Sample));
    }

    [Test]
    public void SimpleProperty_WithInvalidPath_ThrowsArgumentException()
    {
        var source = Expression.Parameter(typeof(SampleEntity));
        var exception = Assert.Throws<ArgumentException>(() =>
        {
            _ = PathSelector.Select(source, "DoesNotExist");
        });
        Assert.That(exception.ParamName, Is.EqualTo("path"));
    }

    [Test]
    public void NestedProperty_WithValidPath_ReturnsExpression()
    {
        var source = Expression.Parameter(typeof(SampleEntity));
        // Should expand to "src.Related.Name"
        var result = PathSelector.Select(source, "RelatedName");
        Assert.That(result.Type, Is.EqualTo(typeof(string)));
        // Test that it returns the correct value if we compile it and run it
        var lambda = Expression.Lambda<Func<SampleEntity, string>>(result, source).Compile();
        var sample = new SampleEntity
        {
            Related = new()
            {
                Name = "Other Name",
            },
        };
        var extractedValue = lambda(sample);
        Assert.That(extractedValue, Is.EqualTo(sample.Related.Name));
    }

    [Test]
    public void CollectionProperty_WithValidPath_ReturnsExpression()
    {
        var source = Expression.Parameter(typeof(SampleEntity));
        // Should expand to "src.Others.Select(x => x.Name)"
        var result = PathSelector.Select(source, "OthersName");
        Assert.That(result.Type, Is.EqualTo(typeof(IEnumerable<string>)));
        // Test that it returns the correct value if we compile it and run it
        var lambda = Expression.Lambda<Func<SampleEntity, IEnumerable<string>>>(result, source).Compile();
        var sample = new SampleEntity
        {
            Others =
            [
                new()
                {
                    Name = "First",
                },
                new()
                {
                    Name = "Second",
                },
            ],
        };
        var extractedValue = lambda(sample).ToList();
        Assert.Multiple(() =>
        {
            Assert.That(extractedValue, Has.Count.EqualTo(2));
            Assert.That(extractedValue.First(), Is.EqualTo("First"));
            Assert.That(extractedValue.Last(), Is.EqualTo("Second"));
        });
    }
}