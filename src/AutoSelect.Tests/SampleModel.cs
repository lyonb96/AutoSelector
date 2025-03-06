namespace AutoSelect.Tests;

public class SampleModel
{
    public string? Sample { get; set; }

    public string? OtherName { get; set; }
}

public class SampleEntity
{
    public string? Sample { get; set; }

    public OtherEntity? Related { get; set; }

    public ICollection<OtherEntity> Others { get; set; } = new HashSet<OtherEntity>();
}

public class OtherEntity
{
    public string? Name { get; set; }
}
