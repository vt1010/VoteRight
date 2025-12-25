using VoteRightWebApp.Models;

namespace VoteRightWebApp.Services
{
    public interface ILocationDataService
    {
        List<string> GetDistricts();
        List<Assembly> GetAssembliesByDistrict(string district);
    }
}
