/* Copyright (C) 2020-2021 b1f6c1c4
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

import { createSelector, createSlice } from '@reduxjs/toolkit';
import dayjs from 'dayjs';

const initialState = {
    loading: false,
    liveViewText: '',
    committed: false,
    editor: {
        date: dayjs().format('YYYYMMDD'),
        payees: {}, // { [person]: share }
        payer: '',
        details: [], // [{ title: string, subtitle: string, content: string, fund: string }]
        adjustments: { t: 0, d: 0 },
        checksum: { payment: 0, discount: 0 },
        payment: null, // { title: string, subtitle: string, content: string, fund: string }
    },
    error: null,
};

const isUser = (person) => /^U([A-Za-z0-9_]+(&[A-Za-z0-9_]+)*|'([^']|'')+')$/.test(person);

const computeShare = (person) => {
    if (isUser(person)) {
        return 1 + (person.match(/&/g) || []).length;
    } else {
        return 1;
    }
};

const parseFund = (fund) => {
    let v = 0, d = 0;
    const sp = fund.split(/\s+/);
    for (const s of sp) {
        const m = s.match(/(?<num>[0-9]+(?:\.[0-9]+)?)(?:(?<equals>=[0-9]+(?:\.[0-9]+)?)|(?<plus>(?:\+[0-9]+(?:\.[0-9]+)?)+)|(?<minus>(?:-[0-9]+(?:\.[0-9]+)?)+))?(?<times>\*[0-9]+(?:\.[0-9]+)?)?/);
        if (!m) return null;
        let fund0 = +m.groups.num;
        let fundd = 0;
        if (m.groups.equals) {
            fundd = fund0 - m.groups.equals.substr(1);
        } else if (m.groups.plus) {
            for (const mm of m.groups.plus.match(/\+[0-9]+(?:\.[0-9]+)?/g))
                fundd += +mm;
            fund0 += fundd;
        } else if (m.groups.minus) {
            for (const mm of m.groups.minus.match(/\-[0-9]+(?:\.[0-9]+)?/g))
                fundd -= +mm;
        }

        if (m.groups.times) {
            const mult = +m.groups.times.substr(1);
            fund0 *= mult;
            fundd *= mult;
        }

        v += fund0;
        d += fundd;
    }
    return { v, d };
};

const updateChecksum = (editor) => {
    let pp = 0, dd = 0;
    for (const d of editor.details) {
        const parse = parseFund(d.fund);
        if (!parse) {
            editor.checksum.payment = NaN;
            editor.checksum.discount = NaN;
            return;
        }
        pp += parse.v - parse.d;
        dd += parse.d;
    }
    pp += editor.adjustments.t;
    pp -= editor.adjustments.d;
    dd += editor.adjustments.d;
    editor.checksum.payment = pp;
    editor.checksum.discount = dd;
};

const computeExpr = (editor) => {
    try {
        if (!editor.payer) return '';
        if (!Object.keys(editor.payees).length) return '';
        if (!editor.details.length) return '';
        let expr = `new Voucher {! ${editor.date}\n`;
        for (const d of editor.details) {
            let ps = [];
            for (const p of Object.keys(editor.payees))
                for (let i = 0; i < editor.payees[p]; i++)
                    if (isUser(p)) {
                        ps.push(`${p} T${d.title.split(':')[0]}${d.subtitle.split(':')[0]} '${d.content.replace(/'/g, '\'\'')}'`);
                    } else {
                        const [pu, pp] = p.split('-', 2);
                        ps.push(`${pu} T1221 '${pp.replace(/'/g, '\'\'')}'`);
                    }
            expr += `${ps.join(' + ')} : ${d.fund} ;\n`;
        }
        expr += `t${editor.adjustments.t} `;
        expr += `d${editor.adjustments.d}\n`;
        expr += `\n`;
        if (isUser(editor.payer)) {
            const d = editor.payment;
            expr += `${editor.payer} T${d.title.split(':')[0]}${d.subtitle.split(':')[0]} '${d.content.replace(/'/g, '\'\'')}' /\n`;
        } else {
            const m = editor.payer.match(/^(?<user>U[^-]+|'(?:[^']|'')+')-(?<peer>.*)$/);
            expr += `${m.groups.user} T2241 '${m.groups.peer.replace(/'/g, '\'\'')}' /\n`;
        }
        expr += `}`;
        return expr;
    } catch (e) {
        console.error(e);
        return 'Error: ' + e.message;
    }
};

const prepare = (state, payload) => {
    if (payload.id === -1) {
        if (!isUser(state.editor.payer)) return;
        if (!state.editor.payment) {
            state.editor.payment = { title: '', subtitle: '', content: '' };
        }
        return state.editor.payment;
    } else {
        return state.editor.details[payload.id];
    }
};

export const editSlice = createSlice({
    name: 'edit',
    initialState,
    reducers: {
        dateInc: (state) => {
            const d = dayjs(state.editor.date, 'YYYYMMDD');
            state.editor.date = d.add(1, 'day').format('YYYYMMDD');
            state.liveViewText = computeExpr(state.editor);
        },
        dateDec: (state) => {
            const d = dayjs(state.editor.date, 'YYYYMMDD');
            state.editor.date = d.subtract(1, 'day').format('YYYYMMDD');
            state.liveViewText = computeExpr(state.editor);
        },
        addPayee: (state, { payload }) => {
            state.editor.payees[payload] = computeShare(payload);
            if (/^U([a-zA-Z0-9_]+)(&[a-zA-Z0-9_]+)+$/.test(payload)) {
                for (const s of payload.substr(1).split('&'))
                    delete state.editor.payees[`U${s}`];
            }
            state.liveViewText = computeExpr(state.editor);
        },
        removePayee: (state, { payload }) => {
            delete state.editor.payees[payload];
            state.liveViewText = computeExpr(state.editor);
        },
        updatePayer: (state, { payload }) => {
            state.editor.payer = payload;
            if (payload) {
                if (!Object.keys(state.editor.payees).length)
                    state.editor.payees[payload] = computeShare(payload);
                if (isUser(payload)) {
                    if (!state.editor.payment) {
                        state.editor.payment = { title: '', subtitle: '', content: '' };
                    }
                } else {
                    state.editor.payment = null;
                }
            } else {
                state.editor.payment = null;
            }
            state.liveViewText = computeExpr(state.editor);
        },
        updateTitle: (state, { payload }) => {
            prepare(state, payload).title = payload.title;
            state.liveViewText = computeExpr(state.editor);
        },
        updateSubtitle: (state, { payload }) => {
            prepare(state, payload).subtitle = payload.subtitle;
            state.liveViewText = computeExpr(state.editor);
        },
        updateContent: (state, { payload }) => {
            prepare(state, payload).content = payload.content;
            state.liveViewText = computeExpr(state.editor);
        },
        updateFund: (state, { payload }) => {
            prepare(state, payload).fund = payload.fund;
            state.liveViewText = computeExpr(state.editor);
            updateChecksum(state.editor);
        },
        removeDetail: (state, { payload }) => {
            state.editor.details.splice(payload, 1);
            state.liveViewText = computeExpr(state.editor);
            updateChecksum(state.editor);
        },
        newDetail: (state) => {
            state.editor.details.push({
                title: '',
                subtitle: '',
                content: '',
                fund: '0',
            });
            state.liveViewText = computeExpr(state.editor);
        },
        updateT: (state, { payload }) => {
            state.editor.adjustments.t = +payload;
            state.liveViewText = computeExpr(state.editor);
            updateChecksum(state.editor);
        },
        updateD: (state, { payload }) => {
            state.editor.adjustments.d = +payload;
            state.liveViewText = computeExpr(state.editor);
            updateChecksum(state.editor);
        },
        submitVoucherRequested: (state) => {
            state.loading = true;
            state.error = null;
            state.committed = false;
        },
        submitVoucherSucceeded: (state, { payload }) => {
            state.loading = false;
            state.liveViewText = payload;
            state.committed = true;
        },
        submitVoucherFailed: (state, { payload }) => {
            state.loading = false;
            state.error = payload;
        },
        revertVoucherRequested: (state) => {
            state.loading = true;
            state.error = null;
        },
        revertVoucherSucceeded: (state) => {
            state.loading = false;
            state.liveViewText = computeExpr(state.editor);
            state.committed = false;
        },
        revertVoucherFailed: (state, { payload }) => {
            state.loading = false;
            state.error = payload;
        },
        resetForm: (state) => {
            Object.assign(state, initialState);
        },
        clearError: (state) => {
            state.error = null;
        },
    },
});

export const {
    dateInc,
    dateDec,
    addPayee,
    removePayee,
    updatePayer,
    updateTitle,
    updateSubtitle,
    updateContent,
    updateFund,
    removeDetail,
    newDetail,
    updateT,
    updateD,
    submitVoucherRequested,
    submitVoucherSucceeded,
    submitVoucherFailed,
    revertVoucherRequested,
    revertVoucherSucceeded,
    revertVoucherFailed,
    resetForm,
    clearError,
} = editSlice.actions;

export default editSlice.reducer;
