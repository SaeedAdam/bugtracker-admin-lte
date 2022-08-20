using AdminLte.Mvc.Services.Interfaces;

namespace AdminLte.Mvc.Services;

public class BTFileService : IBTFileService
{
    private readonly string[] suffixes = { "Bytes", "KB", "MB", "GB", "TB", "PB" };

    public async Task<byte[]> ConvertFileToByteArrayAsync(IFormFile file)
    {
        if (file is null) return null;

        try
        {
            using MemoryStream ms = new();
            await file.CopyToAsync(ms);
            var byteFile = ms.ToArray();
            ms.Close();
            ms.Dispose();

            return byteFile;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public string ConvertByteArrayToFile(byte[] fileData, string extension)
    {
        if (fileData is null || extension is null) return null;

        try
        {
            var imageBased64Data = Convert.ToBase64String(fileData);

            return $"data:image/{extension};base64,{imageBased64Data}";
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public string GetFileIcon(string file)
    {
        string fileImage = "default";

        if (!string.IsNullOrWhiteSpace(file))
        {
            fileImage = Path.GetExtension(file).Replace(".", "");
            return $"/img/contenttype/{fileImage}.png";
        }

        return fileImage;
    }

    public string FormatFileSize(long bytes)
    {
        int counter = 0;
        decimal fileSize = bytes;
        while (Math.Round(fileSize / 1024) >= 1)
        {
            fileSize /= bytes;
            counter++;
        }

        return $"{fileSize:N1}{suffixes[counter]}";
    }
}
