using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.IO;

namespace AccountingServer.DAL
{
    /// <summary>
    ///     Bson���л�������д
    /// </summary>
    internal static class SerializationHelper
    {
        /// <summary>
        ///     ��ȫ���ж��Ƿ�����ĵ���β
        /// </summary>
        /// <param name="bsonReader">Bson��ȡ��</param>
        /// <returns>�Ƿ�����ĵ���β</returns>
        private static bool IsEndOfDocument(this BsonReader bsonReader)
        {
            if (bsonReader.State == BsonReaderState.Type)
                bsonReader.ReadBsonType();
            return bsonReader.State == BsonReaderState.EndOfDocument;
        }

        /// <summary>
        ///     ��ȫ���ж��Ƿ���������β
        /// </summary>
        /// <param name="bsonReader">Bson��ȡ��</param>
        /// <returns>�Ƿ���������β</returns>
        private static bool IsEndOfArray(this BsonReader bsonReader)
        {
            if (bsonReader.State == BsonReaderState.Type)
                bsonReader.ReadBsonType();
            return bsonReader.State == BsonReaderState.EndOfArray;
        }

        /// <summary>
        ///     ��ȫ�ض���ָ���ֶε�����
        /// </summary>
        /// <param name="bsonReader">Bson��ȡ��</param>
        /// <param name="expected">�ֶ���</param>
        /// <param name="read">�ֶ�������</param>
        /// <returns>�Ƿ���Լ�������</returns>
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
        ///     ����ȫ����ָ���ֶ�֮ǰ��׼������
        /// </summary>
        /// <param name="bsonReader">Bson��ȡ��</param>
        /// <param name="expected">�ֶ���</param>
        /// <param name="read">�ֶ�������</param>
        /// <returns>�Ƿ���Լ�������</returns>
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
        ///     ��ȫ�ض���ָ���������͵��ֶ�
        /// </summary>
        /// <param name="bsonReader">Bson��ȡ��</param>
        /// <param name="expected">�ֶ���</param>
        /// <param name="read">�ֶ�������</param>
        /// <param name="readFunc">���Ͷ�ȡ��</param>
        /// <returns>��ȡ���</returns>
        private static T ReadClass<T>(this BsonReader bsonReader, string expected, ref string read,
                                      Func<T> readFunc) where T : class
            => ReadPrep(bsonReader, expected, ref read) ? readFunc() : null;

        /// <summary>
        ///     ��ȫ�ض���ָ��ֵ���͵��ֶ�
        /// </summary>
        /// <param name="bsonReader">Bson��ȡ��</param>
        /// <param name="expected">�ֶ���</param>
        /// <param name="read">�ֶ�������</param>
        /// <param name="readFunc">���Ͷ�ȡ��</param>
        /// <returns>��ȡ���</returns>
        private static T? ReadStruct<T>(this BsonReader bsonReader, string expected, ref string read,
                                        Func<T> readFunc) where T : struct
            => ReadPrep(bsonReader, expected, ref read) ? readFunc() : (T?)null;

        /// <summary>
        ///     ��ȫ�ض���<c>ObjectId</c>���͵��ֶ�
        /// </summary>
        /// <param name="bsonReader">Bson��ȡ��</param>
        /// <param name="expected">�ֶ���</param>
        /// <param name="read">�ֶ�������</param>
        /// <returns>��ȡ���</returns>
        public static string ReadObjectId(this BsonReader bsonReader, string expected, ref string read)
        {
            return ReadClass(bsonReader, expected, ref read, () => bsonReader.ReadObjectId().ToString());
        }

        /// <summary>
        ///     ��ȫ�ض���<c>Int32</c>���͵��ֶ�
        /// </summary>
        /// <param name="bsonReader">Bson��ȡ��</param>
        /// <param name="expected">�ֶ���</param>
        /// <param name="read">�ֶ�������</param>
        /// <returns>��ȡ���</returns>
        public static int? ReadInt32(this BsonReader bsonReader, string expected, ref string read)
            => ReadStruct(bsonReader, expected, ref read, bsonReader.ReadInt32);

        /// <summary>
        ///     ��ȫ�ض���<c>Double</c>���͵��ֶ�
        /// </summary>
        /// <param name="bsonReader">Bson��ȡ��</param>
        /// <param name="expected">�ֶ���</param>
        /// <param name="read">�ֶ�������</param>
        /// <returns>��ȡ���</returns>
        public static double? ReadDouble(this BsonReader bsonReader, string expected, ref string read)
            => ReadStruct(bsonReader, expected, ref read, bsonReader.ReadDouble);

        /// <summary>
        ///     ��ȫ�ض���<c>string</c>���͵��ֶ�
        /// </summary>
        /// <param name="bsonReader">Bson��ȡ��</param>
        /// <param name="expected">�ֶ���</param>
        /// <param name="read">�ֶ�������</param>
        /// <returns>��ȡ���</returns>
        public static string ReadString(this BsonReader bsonReader, string expected, ref string read)
            => ReadClass(bsonReader, expected, ref read, bsonReader.ReadString);

        /// <summary>
        ///     ��ȫ�ض���<c>Guid</c>���͵��ֶ�
        /// </summary>
        /// <param name="bsonReader">Bson��ȡ��</param>
        /// <param name="expected">�ֶ���</param>
        /// <param name="read">�ֶ�������</param>
        /// <returns>��ȡ���</returns>
        public static Guid? ReadGuid(this BsonReader bsonReader, string expected, ref string read)
        {
            return ReadStruct(bsonReader, expected, ref read, () => bsonReader.ReadBinaryData().AsGuid);
        }

        /// <summary>
        ///     ��ȫ�ض���<c>DateTime</c>���͵��ֶ�
        /// </summary>
        /// <param name="bsonReader">Bson��ȡ��</param>
        /// <param name="expected">�ֶ���</param>
        /// <param name="read">�ֶ�������</param>
        /// <returns>��ȡ���</returns>
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
        ///     ��ȫ�ض���<c>null</c>���͵��ֶ�
        /// </summary>
        /// <param name="bsonReader">Bson��ȡ��</param>
        /// <param name="expected">�ֶ���</param>
        /// <param name="read">�ֶ�������</param>
        /// <returns>�Ƿ�ɹ�</returns>
        public static bool ReadNull(this BsonReader bsonReader, string expected, ref string read)
        {
            if (!bsonReader.ReadName(expected, ref read))
                return false;

            bsonReader.ReadNull();
            return true;
        }

        /// <summary>
        ///     ��ȫ�ض����ĵ��ֶ�
        /// </summary>
        /// <param name="bsonReader">Bson��ȡ��</param>
        /// <param name="expected">�ֶ���</param>
        /// <param name="read">�ֶ�������</param>
        /// <param name="parser">�ĵ���ȡ��</param>
        /// <returns>��ȡ���</returns>
        public static T ReadDocument<T>(this BsonReader bsonReader, string expected, ref string read,
                                        Func<BsonReader, T> parser) where T : class
            => ReadPrep(bsonReader, expected, ref read) ? parser(bsonReader) : null;

        /// <summary>
        ///     ��ȫ�ض��������ֶ�
        /// </summary>
        /// <param name="bsonReader">Bson��ȡ��</param>
        /// <param name="expected">�ֶ���</param>
        /// <param name="read">�ֶ�������</param>
        /// <param name="parser">����Ԫ�ض�ȡ��</param>
        /// <returns>��ȡ���</returns>
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
        ///     ��ȫ��д��<c>ObjectId</c>���͵��ֶ�
        /// </summary>
        /// <param name="bsonWriter">Bson��ȡ��</param>
        /// <param name="name">�ֶ���</param>
        /// <param name="value">�ֶ�ֵ</param>
        /// <param name="force">�Ƿ�ǿ��д��<c>null</c>ֵ</param>
        public static void WriteObjectId(this BsonWriter bsonWriter, string name, string value, bool force = false)
        {
            if (value != null)
                bsonWriter.WriteObjectId(name, ObjectId.Parse(value));
            else if (force)
                bsonWriter.WriteNull(name);
        }

        /// <summary>
        ///     ��ȫ��д��<c>Guid</c>���͵��ֶ�
        /// </summary>
        /// <param name="bsonWriter">Bson��ȡ��</param>
        /// <param name="name">�ֶ���</param>
        /// <param name="value">�ֶ�ֵ</param>
        /// <param name="force">�Ƿ�ǿ��д��<c>null</c>ֵ</param>
        public static void Write(this BsonWriter bsonWriter, string name, Guid? value, bool force = false)
        {
            if (value.HasValue)
                bsonWriter.WriteBinaryData(name, value.Value.ToBsonValue());
            else if (force)
                bsonWriter.WriteNull(name);
        }

        /// <summary>
        ///     ��ȫ��д��<c>int</c>���͵��ֶ�
        /// </summary>
        /// <param name="bsonWriter">Bson��ȡ��</param>
        /// <param name="name">�ֶ���</param>
        /// <param name="value">�ֶ�ֵ</param>
        /// <param name="force">�Ƿ�ǿ��д��<c>null</c>ֵ</param>
        public static void Write(this BsonWriter bsonWriter, string name, int? value, bool force = false)
        {
            if (value.HasValue)
                bsonWriter.WriteInt32(name, value.Value);
            else if (force)
                bsonWriter.WriteNull(name);
        }

        /// <summary>
        ///     ��ȫ��д��<c>double</c>���͵��ֶ�
        /// </summary>
        /// <param name="bsonWriter">Bson��ȡ��</param>
        /// <param name="name">�ֶ���</param>
        /// <param name="value">�ֶ�ֵ</param>
        /// <param name="force">�Ƿ�ǿ��д��<c>null</c>ֵ</param>
        public static void Write(this BsonWriter bsonWriter, string name, double? value, bool force = false)
        {
            if (value.HasValue)
                bsonWriter.WriteDouble(name, value.Value);
            else if (force)
                bsonWriter.WriteNull(name);
        }

        /// <summary>
        ///     ��ȫ��д��<c>DateTime</c>���͵��ֶ�
        /// </summary>
        /// <param name="bsonWriter">Bson��ȡ��</param>
        /// <param name="name">�ֶ���</param>
        /// <param name="value">�ֶ�ֵ</param>
        /// <param name="force">�Ƿ�ǿ��д��<c>null</c>ֵ</param>
        public static void Write(this BsonWriter bsonWriter, string name, DateTime? value, bool force = false)
        {
            if (value.HasValue)
                bsonWriter.WriteDateTime(name, BsonUtils.ToMillisecondsSinceEpoch(value.Value));
            else if (force)
                bsonWriter.WriteNull(name);
        }

        /// <summary>
        ///     ��ȫ��д��<c>string</c>���͵��ֶ�
        /// </summary>
        /// <param name="bsonWriter">Bson��ȡ��</param>
        /// <param name="name">�ֶ���</param>
        /// <param name="value">�ֶ�ֵ</param>
        /// <param name="force">�Ƿ�ǿ��д��<c>null</c>ֵ</param>
        public static void Write(this BsonWriter bsonWriter, string name, string value, bool force = false)
        {
            if (value != null)
                bsonWriter.WriteString(name, value);
            else if (force)
                bsonWriter.WriteNull(name);
        }

        /// <summary>
        ///     ��<c>Guid</c>ת��ΪBson����
        /// </summary>
        /// <param name="id">
        ///     <c>Guid</c>
        /// </param>
        /// <returns>Bson����</returns>
        public static BsonBinaryData ToBsonValue(this Guid id) => new BsonBinaryData(id);
    }
}
