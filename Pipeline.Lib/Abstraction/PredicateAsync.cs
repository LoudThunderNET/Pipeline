namespace Pipeline.Lib.Abstraction
{
    public delegate Task<bool> PredicateAsync<in T>(T obj);
}
