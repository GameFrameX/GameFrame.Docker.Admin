using Docker.Admin.Services;
using Docker.DotNet.Models;

var images = new List<ImagesListResponse>
{
    Image("sha256:used", "registry.local/app:2026-07-01", new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc)),
    Image("sha256:old", "registry.local/app:2026-07-02", new DateTime(2026, 7, 2, 0, 0, 0, DateTimeKind.Utc)),
    Image("sha256:target", "registry.local/app:2026-07-03", new DateTime(2026, 7, 3, 0, 0, 0, DateTimeKind.Utc)),
    Image("sha256:newest", "registry.local/app:2026-07-04", new DateTime(2026, 7, 4, 0, 0, 0, DateTimeKind.Utc)),
    Image("sha256:protected", "registry.local/other:stable", new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc)),
    Image("sha256:untagged", "<none>:<none>", new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc))
};

var candidates = DockerImageCleanupPlanner
                 .SelectCleanupCandidates(
                     images,
                     new[] { "sha256:used" },
                     "registry.local/app:2026-07-03",
                     1,
                     new[] { "registry.local/other:stable" })
                 .Select(image => image.ID)
                 .ToArray();

AssertSequence(candidates, "sha256:untagged", "sha256:old");

static ImagesListResponse Image(string id, string tag, DateTime created)
{
    return new ImagesListResponse
    {
        ID = id,
        RepoTags = new List<string> { tag },
        Created = created
    };
}

static void AssertSequence(string[] actual, params string[] expected)
{
    if (!actual.SequenceEqual(expected, StringComparer.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException($"候选镜像顺序错误。实际: {string.Join(", ", actual)}；期望: {string.Join(", ", expected)}");
    }
}
