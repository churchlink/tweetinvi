﻿namespace Tweetinvi.Core.Parameters
{
    public interface IMentionsTimelineParameters : ITimelineRequestParameters
    {
        /// <summary>
        /// Add details to the contributors who participated to the tweets.
        /// </summary>
        bool IncludeContributorDetails { get; set; }
    }

    public class MentionsTimelineParameters : TimelineRequestParameters, IMentionsTimelineParameters
    {
        public MentionsTimelineParameters()
        {
            MaximumNumberOfTweetsToRetrieve = TweetinviConsts.TIMELINE_MENTIONS_COUNT;
        }

        public bool IncludeContributorDetails { get; set; }
    }
}