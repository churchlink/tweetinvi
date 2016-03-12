using System;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Tweetinvi.Logic.JsonConverters
{
	/// <summary>
	/// See http://stackoverflow.com/questions/23505294/is-it-possible-to-serialize-datetimeoffset-to-zulu-time-string-with-json-net
	/// </summary>
	public class JsonTwitterDateTimeOffsetConverter : IsoDateTimeConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
			var datetimeStr = reader.Value as string;

			if(datetimeStr != null) {
				DateTimeOffset datetime = DateTimeOffset.ParseExact(datetimeStr, "ddd MMM dd HH:mm:ss zzzz yyyy", CultureInfo.InvariantCulture);
				return datetime;
			}
			if(reader.Value is DateTimeOffset) {
				return reader.Value;
			}
			if(reader.Value is DateTime) {
				// don't do implicit conversion, will just pick up local time. also MUST use Ticks bec Kind.Utc will throw! 
				DateTime dt = (DateTime)reader.Value;
				return new DateTimeOffset(dt.Ticks, TimeSpan.Zero);
			}
			throw new ArgumentException();
        }
    }

	public class JsonTwitterDateTimeConverter : DateTimeConverterBase
	{
		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			throw new NotImplementedException();
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			var datetimeStr = reader.Value as string;

			if(datetimeStr != null) {
				DateTimeOffset datetime = DateTimeOffset.ParseExact(datetimeStr, "ddd MMM dd HH:mm:ss zzzz yyyy", CultureInfo.InvariantCulture);
				return datetime.UtcDateTime;
			}

			if(reader.Value is DateTime) {
				DateTime dt = (DateTime)reader.Value;
				return dt;
			}

			throw new ArgumentException();
		}
	}

}