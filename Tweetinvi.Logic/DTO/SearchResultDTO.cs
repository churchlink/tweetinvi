﻿using Newtonsoft.Json;
using Tweetinvi.Core.Interfaces.DTO;
using Tweetinvi.Logic.JsonConverters;

namespace Tweetinvi.Logic.DTO
{
    public class SearchResultsDTO : ISearchResultsDTO
    {
        [JsonProperty("statuses")]
        public ITweetWithSearchMetadataDTO[] TweetDTOs { get; set; }

        [JsonIgnore]
        public ITweetWithSearchMetadataDTO[] MatchingTweetDTOs { get; set; }

        [JsonProperty("search_metadata")]
        [JsonConverter(typeof(JsonPropertyConverterRepository))]
        public ISearchMetadata SearchMetadata { get; set; }
    }
}