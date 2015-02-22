using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.IO;

namespace AccountingServer.DAL
{
    /// <summary>
    ///     Bson–Ú¡–ªØ∏®÷˙∂¡–¥
    /// </summary>
    internal static class SerializationHelper
    {
        private static bool IsEndOfDocument(this BsonReader bsonReader)
        {
            if (bsonReader.State == BsonReaderState.Type)
                bsonReader.ReadBsonType();
            return bsonReader.State == BsonReaderState.EndOfDocument;
        }

        private static bool IsEndOfArray(this BsonReader bsonReader)
        {
            if (bsonReader.State == BsonReaderState.Type)
                bsonReader.ReadBsonType();
            return bsonReader.State == BsonReaderState.EndOfArray;
        }

        private static bool ReadName(this BsonReader bsonReader, string expected, ref string read)
        {
            if (bsonReader.IsEndOfDocument())
                return false;
            if (read == null)
                read = bsonReader.ReadName();
            if (read != expected)
                return false;

            read = null;
            return true;
        }

        private static bool ReadPrep(this BsonReader bsonReader, string expected, ref string read)
        {
            if (!bsonReader.ReadName(expected, ref read))
                return false;

            if (bsonReader.CurrentBsonType == BsonType.Null)
            {
                bsonReader.ReadNull();
                return false;
            }
            if (
                bsonReader.CurrentBsonType == BsonType.Undefined)
            {
                bsonReader.ReadUndefined();
                return false;
            }

            return true;
        }

        private static T ReadClass<T>(this BsonReader bsonReader, string expected, ref string read,
                                      Func<T> readFunc) where T : class
        {
            return ReadPrep(bsonReader, expected, ref read) ? readFunc() : null;
        }

        private static T? ReadStruct<T>(this BsonReader bsonReader, string expected, ref string read,
                                        Func<T> readFunc) where T : struct
        {
            return ReadPrep(bsonReader, expected, ref read) ? readFunc() : (T?)null;
        }

        public static string ReadObjectId(this BsonReader bsonReader, string expected, ref string read)
        {
            return ReadClass(bsonReader, expected, ref read, () => bsonReader.ReadObjectId().ToString());
        }

        public static int? ReadInt32(this BsonReader bsonReader, string expected, ref string read)
        {
            return ReadStruct(bsonReader, expected, ref read, bsonReader.ReadInt32);
        }

        public static double? ReadDouble(this BsonReader bsonReader, string expected, ref string read)
        {
            return ReadStruct(bsonReader, expected, ref read, bsonReader.ReadDouble);
        }

        public static string ReadString(this BsonReader bsonReader, string expected, ref string read)
        {
            return ReadClass(bsonReader, expected, ref read, bsonReader.ReadString);
        }

        public static Guid? ReadGuid(this BsonReader bsonReader, string expected, ref string read)
        {
            return ReadStruct(bsonReader, expected, ref read, () => bsonReader.ReadBinaryData().AsGuid);
        }

        public static DateTime? ReadDateTime(this BsonReader bsonReader, string expected, ref string read)
        {
            return ReadStruct(
                              bsonReader,
                              expected,
                              ref read,
                              () =>
                              BsonUtils.ToDateTimeFromMillisecondsSinceEpoch(bsonReader.ReadDateTime()).ToLocalTime());
        }

        public static bool ReadNull(this BsonReader bsonReader, string expected, ref string read)
        {
            if (!bsonReader.ReadName(expected, ref read))
                return false;

            bsonReader.ReadNull();
            return true;
        }

        public static T ReadDocument<T>(this BsonReader bsonReader, string expected, ref string read,
                                        Func<BsonReader, T> parser) where T : class
        {
            return ReadPrep(bsonReader, expected, ref read) ? parser(bsonReader) : null;
        }

        public static List<T> ReadArray<T>(this BsonReader bsonReader, string expected, ref string read,
                                           Func<BsonReader, T> parser)
        {
            if (!ReadPrep(bsonReader, expected, ref read))
                return null;

            var lst = new List<T>();
            bsonReader.ReadStartArray();
            while (!bsonReader.IsEndOfArray())
                lst.Add(parser(bsonReader));
            bsonReader.ReadEndArray();
            return lst;
        }

        public static void WriteObjectId(this BsonWriter bsonWriter, string name, string value, bool force = false)
        {
            if (value != null)
                bsonWriter.WriteObjectId(name, ObjectId.Parse(value));
            else if (force)
                bsonWriter.WriteNull(name);
        }

        public static void Write(this BsonWriter bsonWriter, string name, Guid? value, bool force = false)
        {
            if (value.HasValue)
                bsonWriter.WriteBinaryData(name, value.Value.ToBsonValue());
            else if (force)
                bsonWriter.WriteNull(name);
        }

        public static void Write(this BsonWriter bsonWriter, string name, int? value, bool force = false)
        {
            if (value.HasValue)
                bsonWriter.WriteInt32(name, value.Value);
            else if (force)
                bsonWriter.WriteNull(name);
        }

        public static void Write(this BsonWriter bsonWriter, string name, double? value, bool force = false)
        {
            if (value.HasValue)
                bsonWriter.WriteDouble(name, value.Value);
            else if (force)
                bsonWriter.WriteNull(name);
        }

        public static void Write(this BsonWriter bsonWriter, string name, DateTime? value, bool force = false)
        {
            if (value.HasValue)
                bsonWriter.WriteDateTime(name, BsonUtils.ToMillisecondsSinceEpoch(value.Value));
            else if (force)
                bsonWriter.WriteNull(name);
        }

        public static void Write(this BsonWriter bsonWriter, string name, string value, bool force = false)
        {
            if (value != null)
                bsonWriter.WriteString(name, value);
            else if (force)
                bsonWriter.WriteNull(name);
        }

        public static BsonBinaryData ToBsonValue(this Guid id) { return new BsonBinaryData(id); }
    }
}
