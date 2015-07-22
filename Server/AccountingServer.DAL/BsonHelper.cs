using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.IO;

namespace AccountingServer.DAL
{
    /// <summary>
    ///     Bson序列化辅助读写
    /// </summary>
    internal static class SerializationHelper
    {
        /// <summary>
        ///     安全地判断是否读到文档结尾
        /// </summary>
        /// <param name="bsonReader">Bson读取器</param>
        /// <returns>是否读到文档结尾</returns>
        private static bool IsEndOfDocument(this BsonReader bsonReader)
        {
            if (bsonReader.State == BsonReaderState.Type)
                bsonReader.ReadBsonType();
            return bsonReader.State == BsonReaderState.EndOfDocument;
        }

        /// <summary>
        ///     安全地判断是否读到数组结尾
        /// </summary>
        /// <param name="bsonReader">Bson读取器</param>
        /// <returns>是否读到数组结尾</returns>
        private static bool IsEndOfArray(this BsonReader bsonReader)
        {
            if (bsonReader.State == BsonReaderState.Type)
                bsonReader.ReadBsonType();
            return bsonReader.State == BsonReaderState.EndOfArray;
        }

        /// <summary>
        ///     安全地读入指定字段的名称
        /// </summary>
        /// <param name="bsonReader">Bson读取器</param>
        /// <param name="expected">字段名</param>
        /// <param name="read">字段名缓存</param>
        /// <returns>是否可以继续读入</returns>
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

        /// <summary>
        ///     做安全读入指定字段之前的准备工作
        /// </summary>
        /// <param name="bsonReader">Bson读取器</param>
        /// <param name="expected">字段名</param>
        /// <param name="read">字段名缓存</param>
        /// <returns>是否可以继续读入</returns>
        private static bool ReadPrep(this BsonReader bsonReader, string expected, ref string read)
        {
            if (!bsonReader.ReadName(expected, ref read))
                return false;

            if (bsonReader.CurrentBsonType == BsonType.Null)
            {
                bsonReader.ReadNull();
                return false;
            }
            if (bsonReader.CurrentBsonType == BsonType.Undefined)
            {
                bsonReader.ReadUndefined();
                return false;
            }

            return true;
        }

        /// <summary>
        ///     安全地读入指定引用类型的字段
        /// </summary>
        /// <param name="bsonReader">Bson读取器</param>
        /// <param name="expected">字段名</param>
        /// <param name="read">字段名缓存</param>
        /// <param name="readFunc">类型读取器</param>
        /// <returns>读取结果</returns>
        private static T ReadClass<T>(this BsonReader bsonReader, string expected, ref string read,
                                      Func<T> readFunc) where T : class
            => ReadPrep(bsonReader, expected, ref read) ? readFunc() : null;

        /// <summary>
        ///     安全地读入指定值类型的字段
        /// </summary>
        /// <param name="bsonReader">Bson读取器</param>
        /// <param name="expected">字段名</param>
        /// <param name="read">字段名缓存</param>
        /// <param name="readFunc">类型读取器</param>
        /// <returns>读取结果</returns>
        private static T? ReadStruct<T>(this BsonReader bsonReader, string expected, ref string read,
                                        Func<T> readFunc) where T : struct
            => ReadPrep(bsonReader, expected, ref read) ? readFunc() : (T?)null;

        /// <summary>
        ///     安全地读入<c>ObjectId</c>类型的字段
        /// </summary>
        /// <param name="bsonReader">Bson读取器</param>
        /// <param name="expected">字段名</param>
        /// <param name="read">字段名缓存</param>
        /// <returns>读取结果</returns>
        public static string ReadObjectId(this BsonReader bsonReader, string expected, ref string read)
        {
            return ReadClass(bsonReader, expected, ref read, () => bsonReader.ReadObjectId().ToString());
        }

        /// <summary>
        ///     安全地读入<c>Int32</c>类型的字段
        /// </summary>
        /// <param name="bsonReader">Bson读取器</param>
        /// <param name="expected">字段名</param>
        /// <param name="read">字段名缓存</param>
        /// <returns>读取结果</returns>
        public static int? ReadInt32(this BsonReader bsonReader, string expected, ref string read)
            => ReadStruct(bsonReader, expected, ref read, bsonReader.ReadInt32);

        /// <summary>
        ///     安全地读入<c>Double</c>类型的字段
        /// </summary>
        /// <param name="bsonReader">Bson读取器</param>
        /// <param name="expected">字段名</param>
        /// <param name="read">字段名缓存</param>
        /// <returns>读取结果</returns>
        public static double? ReadDouble(this BsonReader bsonReader, string expected, ref string read)
            => ReadStruct(bsonReader, expected, ref read, bsonReader.ReadDouble);

        /// <summary>
        ///     安全地读入<c>string</c>类型的字段
        /// </summary>
        /// <param name="bsonReader">Bson读取器</param>
        /// <param name="expected">字段名</param>
        /// <param name="read">字段名缓存</param>
        /// <returns>读取结果</returns>
        public static string ReadString(this BsonReader bsonReader, string expected, ref string read)
            => ReadClass(bsonReader, expected, ref read, bsonReader.ReadString);

        /// <summary>
        ///     安全地读入<c>Guid</c>类型的字段
        /// </summary>
        /// <param name="bsonReader">Bson读取器</param>
        /// <param name="expected">字段名</param>
        /// <param name="read">字段名缓存</param>
        /// <returns>读取结果</returns>
        public static Guid? ReadGuid(this BsonReader bsonReader, string expected, ref string read)
        {
            return ReadStruct(bsonReader, expected, ref read, () => bsonReader.ReadBinaryData().AsGuid);
        }

        /// <summary>
        ///     安全地读入<c>DateTime</c>类型的字段
        /// </summary>
        /// <param name="bsonReader">Bson读取器</param>
        /// <param name="expected">字段名</param>
        /// <param name="read">字段名缓存</param>
        /// <returns>读取结果</returns>
        public static DateTime? ReadDateTime(this BsonReader bsonReader, string expected, ref string read)
        {
            return ReadStruct(
                              bsonReader,
                              expected,
                              ref read,
                              () =>
                              BsonUtils.ToDateTimeFromMillisecondsSinceEpoch(bsonReader.ReadDateTime()).ToLocalTime());
        }

        /// <summary>
        ///     安全地读入<c>null</c>类型的字段
        /// </summary>
        /// <param name="bsonReader">Bson读取器</param>
        /// <param name="expected">字段名</param>
        /// <param name="read">字段名缓存</param>
        /// <returns>是否成功</returns>
        public static bool ReadNull(this BsonReader bsonReader, string expected, ref string read)
        {
            if (!bsonReader.ReadName(expected, ref read))
                return false;

            bsonReader.ReadNull();
            return true;
        }

        /// <summary>
        ///     安全地读入文档字段
        /// </summary>
        /// <param name="bsonReader">Bson读取器</param>
        /// <param name="expected">字段名</param>
        /// <param name="read">字段名缓存</param>
        /// <param name="parser">文档读取器</param>
        /// <returns>读取结果</returns>
        public static T ReadDocument<T>(this BsonReader bsonReader, string expected, ref string read,
                                        Func<BsonReader, T> parser) where T : class
            => ReadPrep(bsonReader, expected, ref read) ? parser(bsonReader) : null;

        /// <summary>
        ///     安全地读入数组字段
        /// </summary>
        /// <param name="bsonReader">Bson读取器</param>
        /// <param name="expected">字段名</param>
        /// <param name="read">字段名缓存</param>
        /// <param name="parser">数组元素读取器</param>
        /// <returns>读取结果</returns>
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

        /// <summary>
        ///     安全地写入<c>ObjectId</c>类型的字段
        /// </summary>
        /// <param name="bsonWriter">Bson读取器</param>
        /// <param name="name">字段名</param>
        /// <param name="value">字段值</param>
        /// <param name="force">是否强制写入<c>null</c>值</param>
        public static void WriteObjectId(this BsonWriter bsonWriter, string name, string value, bool force = false)
        {
            if (value != null)
                bsonWriter.WriteObjectId(name, ObjectId.Parse(value));
            else if (force)
                bsonWriter.WriteNull(name);
        }

        /// <summary>
        ///     安全地写入<c>Guid</c>类型的字段
        /// </summary>
        /// <param name="bsonWriter">Bson读取器</param>
        /// <param name="name">字段名</param>
        /// <param name="value">字段值</param>
        /// <param name="force">是否强制写入<c>null</c>值</param>
        public static void Write(this BsonWriter bsonWriter, string name, Guid? value, bool force = false)
        {
            if (value.HasValue)
                bsonWriter.WriteBinaryData(name, value.Value.ToBsonValue());
            else if (force)
                bsonWriter.WriteNull(name);
        }

        /// <summary>
        ///     安全地写入<c>int</c>类型的字段
        /// </summary>
        /// <param name="bsonWriter">Bson读取器</param>
        /// <param name="name">字段名</param>
        /// <param name="value">字段值</param>
        /// <param name="force">是否强制写入<c>null</c>值</param>
        public static void Write(this BsonWriter bsonWriter, string name, int? value, bool force = false)
        {
            if (value.HasValue)
                bsonWriter.WriteInt32(name, value.Value);
            else if (force)
                bsonWriter.WriteNull(name);
        }

        /// <summary>
        ///     安全地写入<c>double</c>类型的字段
        /// </summary>
        /// <param name="bsonWriter">Bson读取器</param>
        /// <param name="name">字段名</param>
        /// <param name="value">字段值</param>
        /// <param name="force">是否强制写入<c>null</c>值</param>
        public static void Write(this BsonWriter bsonWriter, string name, double? value, bool force = false)
        {
            if (value.HasValue)
                bsonWriter.WriteDouble(name, value.Value);
            else if (force)
                bsonWriter.WriteNull(name);
        }

        /// <summary>
        ///     安全地写入<c>DateTime</c>类型的字段
        /// </summary>
        /// <param name="bsonWriter">Bson读取器</param>
        /// <param name="name">字段名</param>
        /// <param name="value">字段值</param>
        /// <param name="force">是否强制写入<c>null</c>值</param>
        public static void Write(this BsonWriter bsonWriter, string name, DateTime? value, bool force = false)
        {
            if (value.HasValue)
                bsonWriter.WriteDateTime(name, BsonUtils.ToMillisecondsSinceEpoch(value.Value));
            else if (force)
                bsonWriter.WriteNull(name);
        }

        /// <summary>
        ///     安全地写入<c>string</c>类型的字段
        /// </summary>
        /// <param name="bsonWriter">Bson读取器</param>
        /// <param name="name">字段名</param>
        /// <param name="value">字段值</param>
        /// <param name="force">是否强制写入<c>null</c>值</param>
        public static void Write(this BsonWriter bsonWriter, string name, string value, bool force = false)
        {
            if (value != null)
                bsonWriter.WriteString(name, value);
            else if (force)
                bsonWriter.WriteNull(name);
        }

        /// <summary>
        ///     将<c>Guid</c>转换为Bson对象
        /// </summary>
        /// <param name="id">
        ///     <c>Guid</c>
        /// </param>
        /// <returns>Bson对象</returns>
        public static BsonBinaryData ToBsonValue(this Guid id) => new BsonBinaryData(id);
    }
}
