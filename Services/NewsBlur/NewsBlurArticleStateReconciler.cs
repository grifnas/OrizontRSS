namespace CititorRSS.Jaws;

/// <summary>Result of comparing one local article with its NewsBlur story against the last synchronized baselines.</summary>
public sealed record NewsBlurArticleReconciliation(
    NewsBlurReadSyncAction ReadAction,
    bool SendStar,
    bool SendUnstar,
    bool SendStarForTags,
    int AppliedRemote,
    int Conflicts,
    bool SkippedTags);

/// <summary>
/// Bidirectional state decision for an article that already has NewsBlur baselines.
/// Changes on both sides that reach the same value are not conflicts: only the baselines are updated.
/// Remote changes are applied locally; local changes are returned for the caller to send.
/// </summary>
public static class NewsBlurArticleStateReconciler
{
    public static NewsBlurArticleReconciliation Reconcile(
        Article article,
        NewsBlurStory story,
        Func<Article, bool> savedState,
        Action<Article, bool> applySavedState)
    {
        var appliedRemote = 0;
        var conflicts = 0;

        var localReadChanged = article.IsRead != article.NewsBlurLastLocalRead!.Value;
        var remoteReadChanged = story.IsRead != article.NewsBlurLastRead.GetValueOrDefault();
        var readAction = NewsBlurReadStatePolicy.Decide(article.IsRead, article.NewsBlurLastLocalRead.Value, story.IsRead, article.NewsBlurLastRead.GetValueOrDefault());
        switch (readAction)
        {
            case NewsBlurReadSyncAction.Conflict: conflicts++; break;
            case NewsBlurReadSyncAction.ApplyRemote: article.IsRead = story.IsRead; appliedRemote++; break;
        }

        var localSaved = savedState(article);
        var localStarredChanged = localSaved != article.NewsBlurLastLocalSaved!.Value;
        var remoteStarredChanged = story.IsStarred != article.NewsBlurLastStarred.GetValueOrDefault();
        var starredAligned = localStarredChanged && remoteStarredChanged && localSaved == story.IsStarred;
        var sendStar = false;
        var sendUnstar = false;
        if (localStarredChanged && remoteStarredChanged)
        {
            if (!starredAligned) conflicts++;
        }
        else if (localStarredChanged)
        {
            if (localSaved) sendStar = true;
            else sendUnstar = true;
        }
        else if (remoteStarredChanged) { applySavedState(article, story.IsStarred); appliedRemote++; }

        var localTagsChanged = !SameTags(article.Tags, article.NewsBlurLastLocalTags!);
        var remoteTagsChanged = !SameTags(story.UserTags, article.NewsBlurLastTags!);
        var tagsAligned = localTagsChanged && remoteTagsChanged && SameTags(article.Tags, story.UserTags);
        var sendStarForTags = false;
        var skippedTags = false;
        if (localTagsChanged && remoteTagsChanged)
        {
            if (!tagsAligned) conflicts++;
        }
        else if (localTagsChanged && localSaved) sendStarForTags = true;
        else if (localTagsChanged) skippedTags = true;
        else if (remoteTagsChanged) { article.Tags = story.UserTags.ToList(); appliedRemote++; }

        if ((!localReadChanged && !remoteReadChanged) || readAction == NewsBlurReadSyncAction.AlreadyAligned)
        {
            article.NewsBlurLastRead = story.IsRead;
            article.NewsBlurLastLocalRead = article.IsRead;
        }
        if ((!localStarredChanged && !remoteStarredChanged) || starredAligned)
        {
            article.NewsBlurLastStarred = story.IsStarred;
            article.NewsBlurLastLocalStarred = article.IsFavorite;
            article.NewsBlurLastLocalSaved = savedState(article);
        }
        if ((!localTagsChanged && !remoteTagsChanged) || tagsAligned)
        {
            article.NewsBlurLastTags = story.UserTags.ToList();
            article.NewsBlurLastLocalTags = (article.Tags ?? []).ToList();
        }
        if (remoteReadChanged && !localReadChanged)
        {
            article.NewsBlurLastRead = story.IsRead;
            article.NewsBlurLastLocalRead = story.IsRead;
        }
        if (remoteStarredChanged && !localStarredChanged)
        {
            article.NewsBlurLastStarred = story.IsStarred;
            article.NewsBlurLastLocalStarred = article.IsFavorite;
            article.NewsBlurLastLocalSaved = savedState(article);
        }
        if (remoteTagsChanged && !localTagsChanged)
        {
            article.NewsBlurLastTags = story.UserTags.ToList();
            article.NewsBlurLastLocalTags = story.UserTags.ToList();
        }

        return new NewsBlurArticleReconciliation(readAction, sendStar, sendUnstar, sendStarForTags, appliedRemote, conflicts, skippedTags);
    }

    public static bool SameTags(IEnumerable<string>? left, IEnumerable<string>? right) =>
        (left ?? []).Where(tag => !string.IsNullOrWhiteSpace(tag)).Select(tag => tag.Trim()).ToHashSet(StringComparer.CurrentCultureIgnoreCase)
            .SetEquals((right ?? []).Where(tag => !string.IsNullOrWhiteSpace(tag)).Select(tag => tag.Trim()));
}
