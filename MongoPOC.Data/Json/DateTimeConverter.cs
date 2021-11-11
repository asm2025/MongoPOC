using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MongoPOC.Data.Json
{
	public class DateTimeConverter : JsonConverter<DateTime>
    {
		/// <inheritdoc />
		public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (!typeof(DateTime).IsAssignableFrom(typeToConvert)) throw new InvalidOperationException();
			string value = reader.GetString();
			return string.IsNullOrEmpty(value)
						? DateTime.MinValue
						: DateTime.Parse(value);
		}

		/// <inheritdoc />
		public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
		{
			writer.WriteStringValue(value.ToString("yyyy'-'MM'-'dd hh':'mm':'ss"));
		}
	}
}
