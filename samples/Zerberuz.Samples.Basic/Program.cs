Console.WriteLine("Zerberuz sample shell");

var repository = new SampleRepository();
Console.WriteLine(repository.GetType().Name);

public interface Repository
{
    string FindById(string id);
}

public sealed class SampleRepository : Repository
{
    public string FindById(string id)
    {
        return id;
    }
}

public sealed class OrderService
{
    public string GetStatus()
    {
        return "Created";
    }
}
