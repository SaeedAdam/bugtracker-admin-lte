
using AdminLte.Mvc.Services.Interfaces;

namespace AdminLte.Mvc.Services;

public class BTBasicImageService : IBTImageService
{
    public async Task<byte[]> EncodeImageAsync(IFormFile file)
    {
        if (file is null) return null;

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        return ms.ToArray();
    }

    public async Task<byte[]> EncodeImageAsync(string fileName)
    {
        var file = $"{Directory.GetCurrentDirectory()}/wwwroot/assets/img/{fileName}";

        return await File.ReadAllBytesAsync(file);
    }

    public string DecodeImage(byte[] data, string type)
    {
        if (data is null || type is null) return null;

        return $"data:image/{type};base64,{Convert.ToBase64String(data)}";
    }

    public string GetContentType(IFormFile file)
    {
        return file?.ContentType;
    }

    public int GetImageSize(IFormFile file)
    {
        return Convert.ToInt32(file?.Length);
    }
}