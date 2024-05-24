namespace BatchMigration.Models;

public class S3Path(string bucket, string key)
{
    public string Bucket { get; private set; } = bucket;
    public string Key { get; private set; } = key;

    public static S3Path Parse(string s3Path)
    {
        if (string.IsNullOrWhiteSpace(s3Path))
        {
            throw new ArgumentException("S3 path cannot be null or whitespace.", nameof(s3Path));
        }

        var uri = new Uri(s3Path);
        if (uri.Scheme != "s3")
        {
            throw new ArgumentException(
                "Invalid URI scheme. Scheme 's3' expected.",
                nameof(s3Path)
            );
        }

        var bucket = uri.Host;
        var key = uri.AbsolutePath.TrimStart('/');

        return new S3Path(bucket, key);
    }

    public override string ToString() => $"[green]{this.Bucket}[/]/[yellow]{this.Key}[/]";
}
