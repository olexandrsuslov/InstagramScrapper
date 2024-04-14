using InstagramApiSharp.API;
using InstagramApiSharp.API.Builder;
using InstagramApiSharp.Classes;
using InstagramApiSharp.Classes.Models;
using InstagramApiSharp.Logger;

namespace VideoDownloader;

class Program
{

    public static UserSessionData userSession { get; set; }
    public static IInstaApi api { get; set; }

    public static InstaMediaType MediaType;
        
    static async Task Main(string[] args)
    {
        
        Console.WriteLine("Enter your Instagram username: ");
        var username = Console.ReadLine();

        Console.WriteLine("Enter your Instagram password: ");
        var password = Console.ReadLine();

        Console.WriteLine("Enter the Instagram video or story URL: ");
        var videoUrl = Console.ReadLine();

        Console.WriteLine("Enter the download file name (without extension): ");
        var fileName = Console.ReadLine();
        
        Console.WriteLine("Enter the download folder full path:");
        var downloadFolder = Console.ReadLine();

        userSession = new UserSessionData
        {
            UserName = username,
            Password = password
        };
        
        api = InstaApiBuilder.CreateBuilder()
            .SetUser(userSession)
            .UseLogger(new DebugLogger(LogLevel.All))
            .Build();
        
        MediaType = videoUrl.Contains("reel") ? InstaMediaType.Video : InstaMediaType.Story;
            
        // Login
        var loginResult = await api.LoginAsync();
        if (!loginResult.Succeeded)
        {
            Console.WriteLine($"Login error: {loginResult.Info.Message}");
            return;
        }

        try
        {
            // Get media info
            var mediaId = await GetMediaId(videoUrl);
            var media = await api.MediaProcessor.GetMediaByIdAsync(mediaId.Value);

            if (media == null)
            {
                Console.WriteLine("Error: Media not found.");
                return;
            }

            switch (MediaType)
            {
                // Determine download type based on media type
                case InstaMediaType.Video:
                {
                    var videoDownloadUrl = media.Value.Videos[0].Uri;
                    await DownloadFile(videoDownloadUrl, Path.Combine(downloadFolder, $"{fileName}.mp4"));
                    break;
                }
                case InstaMediaType.Story:
                {
                    string storyDownloadUrl;
                    string filePath;
                    if (media.Value.Videos != null && media.Value.Videos.Count > 0)
                    {
                        storyDownloadUrl = media.Value.Videos[0].Uri;
                        filePath = $"{fileName}.mp4";
                    }
                    else if (media.Value.Images != null && media.Value.Images.Count > 0)
                    {
                        storyDownloadUrl = media.Value.Images[0].Uri;
                        filePath = $"{fileName}.jpg";
                    }
                    else
                    {
                        Console.WriteLine("Error: No valid video or image URL found.");
                        return;
                    }
                    await DownloadFile(storyDownloadUrl, Path.Combine(downloadFolder, filePath));
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            Console.WriteLine($"Downloaded successfully");
            
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
    
    static async Task<IResult<string>> GetMediaId(string url)
    {
        if (MediaType == InstaMediaType.Video)
            return await api.MediaProcessor.GetMediaIdFromUrlAsync(new Uri(url));
        // Get the last segment of the URL
        string[] segments = url.Trim('/').Split('/');
        return new Result<string>(true, segments[^1]);
    }
    
    static async Task DownloadFile(string url, string filePath)
    {
        using (var httpClient = new HttpClient())
        {
            var mediaStream = await httpClient.GetStreamAsync(url);
            using (var fileStream = File.Create(filePath))
            {
                await mediaStream.CopyToAsync(fileStream);
            }
        }
    }
    
}