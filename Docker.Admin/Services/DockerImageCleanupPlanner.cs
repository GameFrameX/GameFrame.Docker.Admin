namespace Docker.Admin.Services;

internal static class DockerImageCleanupPlanner
{
    public static IReadOnlyList<ImagesListResponse> SelectCleanupCandidates(IList<ImagesListResponse> images, IEnumerable<string> usedImageIds, string targetImageReference, int keepRecentImages, IEnumerable<string> protectedImageTags)
    {
        var usedIds = usedImageIds
                      .Where(id => !string.IsNullOrWhiteSpace(id))
                      .Select(NormalizeImageId)
                      .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var protectedTags = protectedImageTags
                            .Where(tag => !string.IsNullOrWhiteSpace(tag))
                            .Select(tag => tag.Trim())
                            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var recentProtectedIds = GetRecentProtectedImageIds(images, keepRecentImages);

        return images
               .Where(image => !usedIds.Contains(NormalizeImageId(image.ID)))
               .Where(image => !HasRepoTag(image, targetImageReference))
               .Where(image => !HasAnyRepoTag(image, protectedTags))
               .Where(image => !recentProtectedIds.Contains(NormalizeImageId(image.ID)))
               .OrderBy(image => image.Created)
               .ThenBy(image => image.ID, StringComparer.OrdinalIgnoreCase)
               .ToList();
    }

    private static HashSet<string> GetRecentProtectedImageIds(IList<ImagesListResponse> images, int keepRecentImages)
    {
        var protectedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (keepRecentImages <= 0)
        {
            return protectedIds;
        }

        var repositoryImages = images
                               .SelectMany(image => GetRepositories(image).Select(repository => new { Repository = repository, Image = image }))
                               .GroupBy(item => item.Repository, StringComparer.OrdinalIgnoreCase);

        foreach (var group in repositoryImages)
        {
            foreach (var item in group.OrderByDescending(item => item.Image.Created).Take(keepRecentImages))
            {
                protectedIds.Add(NormalizeImageId(item.Image.ID));
            }
        }

        return protectedIds;
    }

    private static bool HasRepoTag(ImagesListResponse image, string repoTag)
    {
        if (string.IsNullOrWhiteSpace(repoTag))
        {
            return false;
        }

        return image.RepoTags?.Any(tag => string.Equals(tag, repoTag.Trim(), StringComparison.OrdinalIgnoreCase)) == true;
    }

    private static bool HasAnyRepoTag(ImagesListResponse image, HashSet<string> repoTags)
    {
        if (repoTags.Count == 0 || image.RepoTags == null)
        {
            return false;
        }

        return image.RepoTags.Any(tag => repoTags.Contains(tag));
    }

    private static IEnumerable<string> GetRepositories(ImagesListResponse image)
    {
        if (image.RepoTags == null)
        {
            yield break;
        }

        foreach (var tag in image.RepoTags)
        {
            if (string.IsNullOrWhiteSpace(tag) || string.Equals(tag, "<none>:<none>", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var slashIndex = tag.LastIndexOf('/');
            var colonIndex = tag.LastIndexOf(':');
            yield return colonIndex > slashIndex ? tag[..colonIndex] : tag;
        }
    }

    private static string NormalizeImageId(string imageId)
    {
        const string prefix = "sha256:";
        return imageId != null && imageId.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ? imageId[prefix.Length..] : imageId ?? string.Empty;
    }
}
