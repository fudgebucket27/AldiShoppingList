namespace AldiApi
{
    public interface IProductService
    {
        Task<string> GetProductsAsync(string name);
    }
}
