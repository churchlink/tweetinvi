﻿using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Tweetinvi.Core.Enum;
using Tweetinvi.Core.Exceptions;
using Tweetinvi.Core.Helpers;
using Tweetinvi.Core.Interfaces.Credentials;
using Tweetinvi.Core.Interfaces.DTO.QueryDTO;
using Tweetinvi.Core.Interfaces.Exceptions;
using Tweetinvi.Core.Wrappers;
using Tweetinvi.Credentials.QueryJsonConverters;
using Tweetinvi.Logic.Exceptions;
using Tweetinvi.WebLogic;

namespace Tweetinvi.Credentials
{
    public class TwitterAccessor : ITwitterAccessor
    {
        private readonly IJObjectStaticWrapper _jObjectStaticWrapper;
        private readonly IJsonObjectConverter _jsonObjectConverter;
        private readonly IExceptionHandler _exceptionHandler;
        private readonly ICursorQueryHelper _cursorQueryHelper;
        private readonly ITwitterRequestHandler _twitterRequestHandler;

		/// <summary>
		/// A function that, when set, allows one to receive the raw Http queries and 
		/// their raw results. First string parameter is the query url (or part thereof), 
		/// the last is the response (e.g. json). Useful for diagnostic or other purposes, to see 
		/// the actual query urls and the actual raw responses from Twitter.
		/// </summary>
		public static Action<string, HttpMethod, string> QueriesLog;

		public TwitterAccessor(
            IJObjectStaticWrapper jObjectStaticWrapper,
            IJsonObjectConverter jsonObjectConverter,
            IExceptionHandler exceptionHandler,
            ICursorQueryHelper cursorQueryHelper,
            ITwitterRequestHandler twitterRequestHandler)
        {
            _jObjectStaticWrapper = jObjectStaticWrapper;
            _jsonObjectConverter = jsonObjectConverter;
            _exceptionHandler = exceptionHandler;
            _cursorQueryHelper = cursorQueryHelper;
            _twitterRequestHandler = twitterRequestHandler;
        }

        // Execute<Json>
        public string ExecuteJsonGETQuery(string query)
        {
            return ExecuteQuery(query, HttpMethod.GET);
        }

        public string ExecuteJsonPOSTQuery(string query)
        {
            return ExecuteQuery(query, HttpMethod.POST);
        }

        // Try Execute<Json>
        public bool TryExecuteJsonGETQuery(string query, out string json)
        {
            try
            {
                json = ExecuteJsonGETQuery(query);
                return json != null;
            }
            catch (TwitterException)
            {
                if (!_exceptionHandler.SwallowWebExceptions)
                {
                    throw;
                }

                json = null;
                return false;
            }
        }

        public bool TryExecuteJsonPOSTQuery(string query, out string json)
        {
            try
            {
                json = ExecuteJsonPOSTQuery(query);
                return json != null;
            }
            catch (TwitterException)
            {
                json = null;
                return false;
            }
        }

        // Execute<JObject>
        public JObject ExecuteGETQuery(string query)
        {
            string jsonResponse = ExecuteQuery(query, HttpMethod.GET);
            return _jObjectStaticWrapper.GetJobjectFromJson(jsonResponse);
        }

        public JObject ExecutePOSTQuery(string query)
        {
            string jsonResponse = ExecuteQuery(query, HttpMethod.POST);
            return _jObjectStaticWrapper.GetJobjectFromJson(jsonResponse);
        }

        // Get specific type of object from path
        public T ExecuteGETQueryWithPath<T>(string query, string[] paths) where T : class
        {
            var jObject = ExecuteGETQuery(query);
            return GetResultFromPath<T>(jObject, paths);
        }

        public T ExecutePOSTQueryWithPath<T>(string query, string[] paths) where T : class
        {
            var jObject = ExecutePOSTQuery(query);
            return GetResultFromPath<T>(jObject, paths);
        }

        private T GetResultFromPath<T>(JObject jObject, string[] paths) where T : class
        {
            if (paths != null && paths.Length > 0)
            {
                JToken token;
                if (!jObject.TryGetValue(paths[0], out token))
                {
                    return null;
                }

                for (int i = 1; i < paths.Length; ++i)
                {
                    if (!jObject.TryGetValue(paths[i], out token))
                    {
                        return null;
                    }
                }

                return _jObjectStaticWrapper.ToObject<T>(token);
            }

            return _jObjectStaticWrapper.ToObject<T>(jObject);
        }

        // Execute<T>
        public T ExecuteGETQuery<T>(string query, JsonConverter[] converters = null) where T : class
        {
            string jsonResponse = ExecuteQuery(query, HttpMethod.GET);
            return _jsonObjectConverter.DeserializeObject<T>(jsonResponse, converters);
        }

        public T ExecutePOSTQuery<T>(string query, JsonConverter[] converters = null) where T : class
        {
            string jsonResponse = ExecuteQuery(query, HttpMethod.POST);
            return _jsonObjectConverter.DeserializeObject<T>(jsonResponse, converters);
        }

        // Try Execute
        public bool TryExecuteGETQuery(string query, JsonConverter[] converters = null)
        {
            try
            {
                var jObject = ExecuteGETQuery(query);
                return jObject != null;
            }
            catch (TwitterException)
            {
                if (!_exceptionHandler.SwallowWebExceptions)
                {
                    throw;
                }

                return false;
            }
        }

        public bool TryExecutePOSTQuery(string query, JsonConverter[] converters = null)
        {
            try
            {
                var jObject = ExecutePOSTQuery(query);
                return jObject != null;
            }
            catch (TwitterException)
            {
                if (!_exceptionHandler.SwallowWebExceptions)
                {
                    throw;
                }

                return false;
            }
        }

        // Try Execute<T>
        public bool TryExecuteGETQuery<T>(string query, out T resultObject, JsonConverter[] converters = null)
            where T : class
        {
            try
            {
                resultObject = ExecuteGETQuery<T>(query, converters);
                return resultObject != null;
            }
            catch (TwitterException)
            {
                if (!_exceptionHandler.SwallowWebExceptions)
                {
                    throw;
                }

                resultObject = null;
                return false;
            }
        }

        public bool TryExecutePOSTQuery<T>(string query, out T resultObject, JsonConverter[] converters = null)
            where T : class
        {
            try
            {
                resultObject = ExecutePOSTQuery<T>(query, converters);
                return resultObject != null;
            }
            catch (TwitterException)
            {
                if (!_exceptionHandler.SwallowWebExceptions)
                {
                    throw;
                }

                resultObject = null;
                return false;
            }
        }

        // Multipart Query
        public T ExecuteMultipartQuery<T>(string query, IEnumerable<byte[]> binaries, string contentId, JsonConverter[] converters = null) where T : class
        {
            string jsonResponse = ExecuteMultipartQuery(query, binaries, contentId);
            return _jsonObjectConverter.DeserializeObject<T>(jsonResponse, converters);
        }

        public bool TryExecuteMultipartQuery(string query, IEnumerable<byte[]> binaries, string contentId)
        {
            string unused;
            return TryExecuteMultipartQuery(query, binaries, contentId, out unused);
        }

        public string ExecuteMultipartQuery(string query, IEnumerable<byte[]> binaries, string contentId)
        {
            string result;
            TryExecuteMultipartQuery(query, binaries, contentId, out result);

            return result;
        }

        // Cursor Query
        public IEnumerable<string> ExecuteJsonCursorGETQuery<T>(
                string baseQuery,
                int maxObjectToRetrieve = Int32.MaxValue,
                long cursor = -1)
            where T : class, IBaseCursorQueryDTO
        {
            int nbOfObjectsProcessed = 0;
            long previousCursor = -2;
            long nextCursor = cursor;

            // add & for query parameters
            baseQuery = FormatBaseQuery(baseQuery);

            var result = new List<string>();
            while (previousCursor != nextCursor && nbOfObjectsProcessed < maxObjectToRetrieve)
            {
                T cursorResult = ExecuteCursorQuery<T>(baseQuery, cursor, true);

                if (!CanCursorQueryContinue(cursorResult))
                {
                    return result;
                }

                nbOfObjectsProcessed += cursorResult.GetNumberOfObjectRetrieved();
                previousCursor = cursorResult.PreviousCursor;
                nextCursor = cursorResult.NextCursor;

                result.Add(cursorResult.RawJson);
            }

            return result;
        }

        public IEnumerable<T> ExecuteCursorGETCursorQueryResult<T>(
                string baseQuery,
                int maxObjectToRetrieve = Int32.MaxValue,
                long cursor = -1)
            where T : class, IBaseCursorQueryDTO
        {
            int nbOfObjectsProcessed = 0;
            long previousCursor = -2;
            long nextCursor = cursor;

            // add & for query parameters
            baseQuery = FormatBaseQuery(baseQuery);

            var result = new List<T>();
            while (previousCursor != nextCursor && nbOfObjectsProcessed < maxObjectToRetrieve)
            {
                T cursorResult = ExecuteCursorQuery<T>(baseQuery, nextCursor, false);

                if (!CanCursorQueryContinue(cursorResult))
                {
                    return result;
                }

                nbOfObjectsProcessed += cursorResult.GetNumberOfObjectRetrieved();
                previousCursor = cursorResult.PreviousCursor;
                nextCursor = cursorResult.NextCursor;

                result.Add(cursorResult);
            }

            return result;
        }

        private bool CanCursorQueryContinue<T>(T cursorResult) where T : class, IBaseCursorQueryDTO
        {

            if (cursorResult == null)
            {
                return false;
            }

            if (cursorResult.GetNumberOfObjectRetrieved() == 0 && cursorResult.NextCursor == 0 && cursorResult.PreviousCursor == -1)
            {
                return false;
            }

            return true;
        }

        public IEnumerable<T> ExecuteCursorGETQuery<T, T1>(
                string baseQuery,
                int maxObjectToRetrieve = Int32.MaxValue,
                long cursor = -1)
            where T1 : class, IBaseCursorQueryDTO<T>
        {
            var cursorQueryResult = ExecuteCursorGETCursorQueryResult<T1>(baseQuery, maxObjectToRetrieve, cursor);
            return _cursorQueryHelper.GetResultsFromCursorQuery(cursorQueryResult, maxObjectToRetrieve);
        }

        private string FormatBaseQuery(string baseQuery)
        {
            if (baseQuery.Contains("?") && baseQuery[baseQuery.Length - 1] != '?')
            {
                baseQuery += "&";
            }

            return baseQuery;
        }

        private T ExecuteCursorQuery<T>(string baseQuery, long cursor, bool storeJson) where T : class, IBaseCursorQueryDTO
        {
            var query = string.Format("{0}cursor={1}", baseQuery, cursor);

            string json;
            if (TryExecuteJsonGETQuery(query, out json))
            {
                var dtoResult = _jsonObjectConverter.DeserializeObject<T>(json, JsonQueryConverterRepository.Converters);

                if (storeJson)
                {
                    dtoResult.RawJson = json;
                }

                return dtoResult;
            }

            return null;
        }

        // Concrete Execute
        public string ExecuteQuery(string query, HttpMethod method)
        {
            if (query == null)
            {
                // When a query is null and has been generated by Tweetinvi it implies that one of the query parameter was invalid
                throw new ArgumentException("At least one of the arguments provided to the query was invalid.");
            }

            try
            {
				string result = _twitterRequestHandler.ExecuteQuery(query, method);
				if(QueriesLog != null)
					QueriesLog(query, method, result);
				return result;
            }
            catch (TwitterException ex)
            {
                HandleQueryException(ex);
                return null;
            }
        }

        private bool TryExecuteMultipartQuery(string query, IEnumerable<byte[]> binaries, string contentId, out string result)
        {
            if (query == null)
            {
                throw new ArgumentException("At least one of the arguments provided to the query was invalid.");
            }

            try
            {
                result = _twitterRequestHandler.ExecuteMultipartQuery(query, contentId, HttpMethod.POST, binaries);
				if(QueriesLog != null)
					QueriesLog(query, HttpMethod.POST, result);
				return true;
            }
            catch (TwitterException ex)
            {
                HandleQueryException(ex);

                result = null;
                return false;
            }
        }

        private void HandleQueryException(TwitterException ex)
        {
            if (_exceptionHandler.SwallowWebExceptions)
            {
                return;
            }

            throw ex;
        }
    }
}