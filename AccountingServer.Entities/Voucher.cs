/* Copyright (C) 2020 b1f6c1c4
 *
 * This file is part of ProfessionalAccounting.
 *
 * ProfessionalAccounting is free software: you can redistribute it and/or
 * modify it under the terms of the GNU Affero General Public License as
 * published by the Free Software Foundation, version 3.
 *
 * ProfessionalAccounting is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
 * FITNESS FOR A PARTICULAR PURPOSE.  See the GNU Affero General Public License
 * for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with ProfessionalAccounting.  If not, see
 * <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml.Serialization;

namespace AccountingServer.Entities
{
    /// <summary>
    ///     记账凭证类别
    /// </summary>
    [Serializable]
    public enum VoucherType
    {
        /// <summary>
        ///     普通记账凭证
        /// </summary>
        Ordinary,

        /// <summary>
        ///     非结转记账凭证
        ///     <remarks>仅检索时使用</remarks>
        /// </summary>
        General,

        /// <summary>
        ///     期末结转记账凭证
        /// </summary>
        Carry,

        /// <summary>
        ///     摊销记账凭证
        /// </summary>
        Amortization,

        /// <summary>
        ///     折旧记账凭证
        /// </summary>
        Depreciation,

        /// <summary>
        ///     减值记账凭证
        /// </summary>
        Devalue,

        /// <summary>
        ///     结转记账凭证
        /// </summary>
        AnnualCarry,

        /// <summary>
        ///     不准确记账凭证
        /// </summary>
        Uncertain,
    }

    /// <summary>
    ///     记账凭证
    /// </summary>
    [Serializable]
    public class Voucher
    {
        public Voucher() { }

        public Voucher(Voucher v)
        {
            ID = v.ID;
            Date = v.Date;
            Remark = v.Remark;
            Type = v.Type;
            if (v.Details != null)
                Details = v.Details.Select(d => new VoucherDetail(d)).ToList();
        }

        /// <summary>
        ///     编号
        /// </summary>
        [XmlAttribute("id")]
        public string ID { get; set; }

        /// <summary>
        ///     日期，若为<c>null</c>表示无日期
        /// </summary>
        public DateTime? Date { get; set; }

        /// <summary>
        ///     备注
        /// </summary>
        [XmlAttribute("remark")]
        public string Remark { get; set; }

        /// <summary>
        ///     细目
        /// </summary>
        [XmlElement("Detail")]
        public List<VoucherDetail> Details { get; set; }

        /// <summary>
        ///     类别
        /// </summary>
        [DefaultValue(VoucherType.Ordinary)]
        public VoucherType? Type { get; set; }
    }

    /// <summary>
    ///     细目
    /// </summary>
    [Serializable]
    public class VoucherDetail
    {
        /// <summary>
        ///     判断金额相等的误差
        /// </summary>
        public const double Tolerance = 1e-8;

        public VoucherDetail() { }

        public VoucherDetail(VoucherDetail d)
        {
            User = d.User;
            Currency = d.Currency;
            Title = d.Title;
            SubTitle = d.SubTitle;
            Content = d.Content;
            Fund = d.Fund;
            Remark = d.Remark;
        }

        /// <summary>
        ///     用户
        /// </summary>
        [XmlAttribute("user")]
        public string User { get; set; }

        /// <summary>
        ///     币种
        /// </summary>
        [XmlAttribute("currency")]
        public string Currency { get; set; }

        /// <summary>
        ///     会计科目一级科目代码
        /// </summary>
        public int? Title { get; set; }

        /// <summary>
        ///     会计科目二级科目代码，若为<c>null</c>表示无二级科目
        /// </summary>
        public int? SubTitle { get; set; }

        /// <summary>
        ///     内容
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        ///     金额
        /// </summary>
        public double? Fund { get; set; }

        /// <summary>
        ///     备注
        /// </summary>
        public string Remark { get; set; }
    }

    /// <summary>
    ///     带有指针的细目
    /// </summary>
    public class VoucherDetailR : VoucherDetail
    {
        public VoucherDetailR(Voucher v, VoucherDetail d) : base(d) => Voucher = v;

        public VoucherDetailR(VoucherDetailR d) : this(new Voucher(d.Voucher), d) { }

        /// <summary>
        ///     所属记账凭证
        /// </summary>
        public Voucher Voucher { get; set; }
    }
}
