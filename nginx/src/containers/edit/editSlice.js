import { createSelector, createSlice } from '@reduxjs/toolkit';
import dayjs from 'dayjs';

const initialState = {
    loading: false,
    liveViewText: '',
    editor: {
        date: dayjs().format('YYYYMMDD'),
        payees: {}, // { [person]: share }
        payers: {}, // { [person]: share }
        details: [], // [{ title: int, subtitle: int, content: string, fund: string }]
        adjustments: { t: 0, d: 0 },
        checksum: { payment: 0, discount: 0 },
        payments: [],
        committed: false,
    },
    error: null,
};

const parseFund = (fund) => {
    let v = 0, d = 0;
    const sp = fund.split(/\s+/);
    for (const s of sp) {
        const m = s.match(/(?<num>[0-9]+(?:\.[0-9]+)?)(?:(?<equals>=[0-9]+(?:\.[0-9]+)?)|(?<plus>(?:\+[0-9]+(?:\.[0-9]+)?)+)|(?<minus>(?:-[0-9]+(?:\.[0-9]+)?)+))?(?<times>\*[0-9]+(?:\.[0-9]+)?)?/);
        if (!m) return null;
        let fund0 = +m.num;
        let fundd = 0;
        if (m.equals) {
            fundd = fund0 - m.equals.substr(1);
        } else if (m.plus) {
            for (const m of m.plus.match(/\+[0-9]+(?:\.[0-9]+)?/g))
                fundd += +m;
            fund0 += fundd;
        } else if (m.minus) {
            for (const m of m.minus.match(/\-[0-9]+(?:\.[0-9]+)?/g))
                fundd -= +m;
        }

        if (m.times) {
            const mult = +m.times;
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
    let expr = `new Voucher {! ${editor.date}\n`;
    for (const d of editor.details) {
        let ps = [];
        for (const p of Object.keys(editor.payee))
            for (let i = 0; i < editor.payee[p]; i++)
                if (/^U([a-z&]+|'([^']|'')+')$/.test(p)) {
                    ps.push(`${p} T${d.title}${(''+d.subtitle).padStart(2)} '${d.content.replace(/'/g, '\'\'')}'`);
                } else {
                    const [pu, pp] = p.split('-', 1);
                    ps.push(`${pu} T1221 '${pp.replace(/'/g, '\'\'')}'`);
                }
        expr += `${ps.join(' + ')} : ${d.fund}\n`;
    }
    expr += `t${editor.adjustments.t} `;
    expr += `d${editor.adjustments.d}\n`;
    expr += `\n`;
    // TODO: payments
    expr += `}`;
    return expr;
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
            state.editor.payees[payload] = 1 + (payload.match(/&/g) || [] ).length;
            state.liveViewText = computeExpr(state.editor);
        },
        removePayee: (state, { payload }) => {
            delete state.editor.payees[payload];
            state.liveViewText = computeExpr(state.editor);
        },
        addPayer: (state, { payload }) => {
            state.editor.payers[payload] = 1 + (payload.match(/&/g) || [] ).length;
            state.liveViewText = computeExpr(state.editor);
        },
        removePayer: (state, { payload }) => {
            delete state.editor.payers[payload];
            state.liveViewText = computeExpr(state.editor);
        },
        submitVoucherRequested: (state) => {
            state.loading = true;
            state.error = null;
            state.editor.committed = false;
        },
        submitVoucherSucceeded: (state, { payload }) => {
            state.loading = false;
            state.liveViewText = payload;
            state.editor.committed = true;
        },
        submitVoucherFailed: (state, { payload }) => {
            state.loading = false;
            state.error = payload;
        },
    },
});

export const {
    dateInc,
    dateDec,
    addPayee,
    removePayee,
    addPayer,
    removePayer,
    submitVoucherRequested,
    submitVoucherSucceeded,
    submitVoucherFailed,
} = editSlice.actions;

export default editSlice.reducer;
