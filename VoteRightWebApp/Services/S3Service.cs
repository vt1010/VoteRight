using System.Globalization;
using System.Text;
using Amazon.S3;
using Amazon.S3.Model;
using CsvHelper;
using CsvHelper.Configuration;

namespace VoteRightWebApp.Services
{
    public class S3Service : IS3Service
    {
        private readonly IAmazonS3 _s3Client;
        private readonly ILogger<S3Service> _logger;
        private readonly IConfiguration _configuration;

        public S3Service(IAmazonS3 s3Client, ILogger<S3Service> logger, IConfiguration configuration)
        {
            _s3Client = s3Client;
            _logger = logger;
            _configuration = configuration;
        }


        // Method 1: Download the Assembly CSV file from S3 bucket
        public async Task<Stream> DownloadAssemblyFileAsync(string assemblyNumber)
        {
            string? bucketName = _configuration["AWS:BucketName"];
            if (string.IsNullOrEmpty(bucketName))
                throw new ArgumentException("Bucket name cannot be null or empty", nameof(bucketName));

            var fileNameKey = await ListFilesWithPrefixAsync(assemblyNumber);
            if (fileNameKey.Count == 0)
            {
                _logger.LogError("File {FileName} not found in bucket {BucketName}", fileNameKey, bucketName);
                throw new FileNotFoundException($"The file was not found.");
            }

            try
            {
                return await CopyS3StreamToUtf8MemoryStream(bucketName, fileNameKey[0]);
            }
            catch (CsvHelper.BadDataException ex)
            {
                _logger.LogError(ex, "CSV format error in file {FileName} from bucket {BucketName}. Bad data at row {Row}: {RawRecord}", fileNameKey, bucketName, ex.Context?.Parser?.Row, ex.Context?.Parser?.RawRecord);
                throw new InvalidOperationException($"The CSV file '{fileNameKey}' contains invalid data at row {ex.Context?.Parser?.Row}. Please check the file format.", ex);
            }
            catch (AmazonS3Exception ex)
            {
                _logger.LogError(ex, "AWS S3 error downloading file {FileName} from bucket {BucketName}: {ErrorCode}", fileNameKey, bucketName, ex.ErrorCode);
                throw new InvalidOperationException($"Failed to download file '{fileNameKey}' from S3: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading assembly file {FileName} from bucket {BucketName}", fileNameKey, bucketName);
                throw;
            }
        }

        // Method 2: Download filtered assembly CSV file with filter to 'Part' column for selected booth values
        public async Task<Stream> DownloadAssemblyFileByBoothsAsync(string assemblyNumber, List<int> booths)
        {
            string? bucketName = _configuration["AWS:BucketName"];
            if (string.IsNullOrEmpty(bucketName))
                throw new ArgumentException("Bucket name cannot be null or empty", nameof(bucketName));

            if (booths == null || booths.Count == 0)
                throw new ArgumentException("Booth values cannot be null or empty", nameof(booths));

            var fileNameKey = await ListFilesWithPrefixAsync(assemblyNumber).ConfigureAwait(false);
            if (fileNameKey.Count == 0)
            {
                _logger.LogError("File {FileName} not found in bucket {BucketName}", fileNameKey, bucketName);
                throw new FileNotFoundException($"The file was not found.");
            }

            var memoryStream = new MemoryStream();
            try
            {
                var request = new GetObjectRequest
                {
                    BucketName = bucketName,
                    Key = fileNameKey[0]
                };

                using var response = await _s3Client.GetObjectAsync(request).ConfigureAwait(false);
                using var reader = new StreamReader(response.ResponseStream, new UTF8Encoding(true), bufferSize: 65536, leaveOpen: false);
                using var writer = new StreamWriter(memoryStream, new UTF8Encoding(true), bufferSize: 65536, leaveOpen: true);
                using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true,
                    BufferSize = 65536
                });
                using var csvWriter = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    BufferSize = 65536
                });

                var boothSet = new HashSet<string>(booths.Select(v => v.ToString()), StringComparer.OrdinalIgnoreCase);

                if (!await csv.ReadAsync().ConfigureAwait(false))
                {
                    await csvWriter.FlushAsync().ConfigureAwait(false);
                    await writer.FlushAsync().ConfigureAwait(false);
                    memoryStream.Position = 0;
                    return memoryStream;
                }

                csv.ReadHeader();
                var headers = csv.HeaderRecord?.ToList() ?? new List<string>();

                // Ensure CSV contains the part_no column; otherwise, we cannot filter by booth numbers
                if (!headers.Any(h => string.Equals(h, "part_no", StringComparison.OrdinalIgnoreCase)))
                {
                    // Throw a specific exception so callers can return a friendly error message to the UI
                    throw new BoothsNotFoundException("CSV file is missing required 'part_no' column.");
                }

                foreach (var header in headers)
                {
                    csvWriter.WriteField(header);
                }
                await csvWriter.NextRecordAsync().ConfigureAwait(false);

                int matchCount = 0;
                while (await csv.ReadAsync().ConfigureAwait(false))
                {
                    string? partNo = null;
                    // Prefer TryGetField to avoid exceptions if a row has fewer fields than headers
                    if (csv.TryGetField<string>("part_no", out var pn))
                    {
                        partNo = pn;
                    }
                    else
                    {
                        foreach (var header in headers)
                        {
                            if (string.Equals(header, "part_no", StringComparison.OrdinalIgnoreCase))
                            {
                                if (csv.TryGetField<string>(header, out var pn2))
                                {
                                    partNo = pn2;
                                }
                                break;
                            }
                        }
                    }

                    if (partNo != null && boothSet.Contains(partNo))
                    {
                        // Safely read each field - use TryGetField to avoid index/out-of-range issues when rows are malformed
                        foreach (var header in headers)
                        {
                            if (csv.TryGetField<string>(header, out var fieldValue))
                            {
                                csvWriter.WriteField(fieldValue);
                            }
                            else
                            {
                                // Missing field for this row/column - write empty string to keep columns aligned
                                csvWriter.WriteField(string.Empty);
                            }
                        }
                        await csvWriter.NextRecordAsync().ConfigureAwait(false);
                        matchCount++;
                    }
                }

                // If no rows matched the requested booth numbers, throw a specific exception so the controller can return a friendly error
                if (matchCount == 0)
                {
                    throw new BoothsNotFoundException("No records found for requested booth number(s).");
                }

                await csvWriter.FlushAsync().ConfigureAwait(false);
                await writer.FlushAsync().ConfigureAwait(false);
                memoryStream.Position = 0;
                return memoryStream;
            }
            catch (CsvHelper.BadDataException ex)
            {
                memoryStream?.Dispose();
                _logger.LogError(ex, "CSV format error in file {FileName} from bucket {BucketName}. Bad data at row {Row}: {RawRecord}", fileNameKey, bucketName, ex.Context?.Parser?.Row, ex.Context?.Parser?.RawRecord);
                throw new InvalidOperationException($"The CSV file '{fileNameKey}' contains invalid data at row {ex.Context?.Parser?.Row}. Please check the file format.", ex);
            }
            catch (AmazonS3Exception ex)
            {
                memoryStream?.Dispose();
                _logger.LogError(ex, "AWS S3 error downloading file {FileName} from bucket {BucketName}: {ErrorCode}", fileNameKey, bucketName, ex.ErrorCode);
                throw new InvalidOperationException($"Failed to download file '{fileNameKey}' from S3: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                memoryStream?.Dispose();
                _logger.LogError(ex, "Error downloading filtered CSV {FileName} from bucket {BucketName} with booth filter {Booths}", fileNameKey, bucketName, string.Join(",", booths));
                throw;
            }
        }

        // Helper: Copy S3 stream to UTF-8 encoded memory stream
        private async Task<MemoryStream> CopyS3StreamToUtf8MemoryStream(string bucketName, string key)
        {
            var request = new GetObjectRequest
            {
                BucketName = bucketName,
                Key = key
            };
            var response = await _s3Client.GetObjectAsync(request);
            var memoryStream = new MemoryStream();
            using (var reader = new StreamReader(response.ResponseStream, new UTF8Encoding(true), bufferSize: 65536, leaveOpen: false))
            using (var writer = new StreamWriter(memoryStream, new UTF8Encoding(true), bufferSize: 65536, leaveOpen: true))
            {
                char[] buffer = new char[65536];
                int read;
                while ((read = await reader.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await writer.WriteAsync(buffer, 0, read);
                }
                await writer.FlushAsync();
            }
            memoryStream.Position = 0;
            return memoryStream;
        }

        private async Task<List<string>> ListFilesWithPrefixAsync(string prefix)
        {
            string? bucketName = _configuration["AWS:BucketName"];
            if (string.IsNullOrEmpty(bucketName))
                throw new ArgumentException("Bucket name cannot be null or empty", nameof(bucketName));

            var request = new ListObjectsV2Request
            {
                BucketName = bucketName,
                Prefix = prefix // Only objects with this prefix will be listed
            };

            var result = new List<string>();
            ListObjectsV2Response response;
            do
            {
                response = await _s3Client.ListObjectsV2Async(request);
                result.AddRange(response.S3Objects.Select(o => o.Key));
                request.ContinuationToken = response.NextContinuationToken;
            } while (response.IsTruncated);

            return result;
        }
    }
}
