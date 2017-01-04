using System;
using AccountingServer.Entities;
using MongoDB.Bson.IO;

namespace AccountingServer.DAL
{
    /// <summary>
    ///     余额表条目反序列化器（从MapReduce的结果中反序列化）
    /// </summary>
    internal class BalanceSerializer : BaseSerializer<Balance>
    {
        public override void Serialize(IBsonWriter bsonWriter, Balance voucher)
        {
            throw new InvalidOperationException();
        }

        public override Balance Deserialize(IBsonReader bsonReader)
        {
            string read = null;

            bsonReader.ReadStartDocument();
            var balance =
                bsonReader
                    .ReadDocument(
                                  "_id",
                                  ref read,
                                  bR =>
                                  {
                                      bR.ReadStartDocument();
                                      var bal =
                                          new Balance
                                              {
                                                  Date = bR.ReadDateTime("date", ref read),
                                                  Title = (int?)bR.ReadDouble("title", ref read),
                                                  SubTitle = (int?)bR.ReadDouble("subtitle", ref read),
                                                  Content = bR.ReadString("content", ref read),
                                                  Remark = bR.ReadString("remark", ref read),
                                                  Currency = bR.ReadString("currency", ref read)
                                              };
                                      bR.ReadEndDocument();
                                      return bal;
                                  });
            // ReSharper disable once PossibleInvalidOperationException
            balance.Fund = bsonReader.ReadDouble("value", ref read).Value;
            bsonReader.ReadEndDocument();

            return balance;
        }
    }
}
