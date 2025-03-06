# Welcome to Auto Selector!

Hello, fellow Entity Framework users! I built this library because I got tired of manually rolling long Select statements, or maintaining code generators to write them for me. I wanted a way to declare what I wanted my models to look like, and map them by convention. I struggled to make AutoMapper work for me in this regard, and wanted something with minimal upfront setup, so I decided to roll my own.

As an example, you might be familiar with giant select queries like the following:

```cs
var products = await db.Set<Product>()
    .Where(x => x.IsActive)
    .Select(x => new ProductModel
    {
        Id = x.Id,
        Name = x.Name,
        // etc...
        Categories = x.Categories
            .Select(y => new CategoryModel
            {
                Id = y.Id,
                Name = y.Name,
                // it just keeps going!
            }),
    })
    .ToListAsync();
```

These become exhausting to maintain, especially when you adjust your model. A lot of codebases resort to code generators to handle common mappings, but this just becomes another layer of code you need to maintain (and in my experience, codegen is often more of a pain than it's worth).

With AutoSelector, you simply write your models, and the library automatically generates and caches an expression for you based on easy-to-understand conventions:

```cs
public class ProductModel
{
    public int Id { get; set; }

    public string? Name { get; set; }

    [MapFrom("ManufacturerNumber")]
    public string? MfgNumber { get; set; }

    public List<CategoryModel>? Categories { get; set; }
}

public class CategoryModel
{
    public int Id { get; set; }

    public string? Name { get; set; }
}
```

And then to use it, you simply call `.Map<>()` on your query:

```cs
var products = await db.Set<Product>()
    .Where(x => x.IsActive)
    .Map<ProductModel>()
    .ToListAsync();
```

And that's it! When you change your model, you don't have to worry about updating a bunch of Select statements, rerunning a T4 template, or hoping your source generators don't break Visual Studio for the hundredth time this week. It also builds the whole thing as an Expression, meaning EF optimizes the SQL it generates to only select the properties on your models.

## What can it do?

The rules for mapping are actually quite simple:

1. If the mapper finds a property that matches by exact name, it will use that.
2. If the mapper finds a property that matches as a prefix, it will delve into properties of that subobject (often referred to as *flattening*)
3. If the mapper processes a property that is a collection type, it will use the remainder of the path to select properties of the elements of that collection.
4. You can use `[MapFrom]` to assign values from paths that don't exactly match the name of the property on the model. This can be nice when mapping a flattened value across many joins.
5. You can use `[NotMapped]` to prevent the mapper from assigning a value to a property.
6. The mapper will try to match collection types - eg, if your model specifies a `List<SomeModel>`, it will append a `.ToList()` to the generated expression. It is built to handle `List, ICollection, IEnumerable`, and raw arrays.

Some advanced examples:

```cs
// Expands to "x.User.Contact.State.Name"
[MapFrom("UserContactStateName")]
public string? StateOfResidency { get; set; }

// Expands to "x.Categories.Select(y => y.Id)"
[MapFrom("CategoriesId")]
public IEnumerable<int> CategoryIds { get; set; }
```

As an additional note, you can use the same model against different source types, allowing you to define reusable models in case that's useful to you.

## How fast is it?

Fast enough that you won't notice it next to any realistic database latency. The expression is also cached, so subsequent calls to map from the same source to destination type are no less performant than hand-written Select statements.

## Can I use it?

Yes. To be perfectly honest I'm sure there are a thousand tools just like this one, but I had fun building it and figured someone else might get some use out of it. It's MIT licensed, feel free to use it for whatever you want.

## Future feature ideas

Including a way to specify an expression for properties instead of building one automatically, eg:

```cs
AutoSelector<Product, ProductModel>.SetPropertyExpression(
    x => x.Price,
    x => x.Prices
        .OrderByDescending(x => x.Amount)
        .Select(x => x.Amount)
        .FirstOrDefault());
```

Another fun feature would be inline filtering for collection properties:

```cs
public class ProductModel
{
    // Somehow use this to inject ".Where(x => x.IsPublic == true)" into the generated expression
    [Filter("IsPublic", "true")]
    public List<FileModel> Files { get; set; }
}
```