namespace VoteRightWebApp.Services
{
    public interface IS3Service
    {
        Task<Stream> DownloadAssemblyFileAsync(string assemblyNumber);
        Task<Stream> DownloadAssemblyFileByBoothsAsync(string assemblyNumber, List<int> booths);
    }
}
