import { createSelector, createSlice } from '@reduxjs/toolkit';
import dayjs from 'dayjs';

const initialState = {
    loading: false,
    liveViewText: '',
    editor: {
        date: dayjs().format('YYYYMMDD'),
        payees: {},
        payers: {},
        details: [],
        adjustments: { t: 0, d: 0 },
        checksum: 0,
        payments: [],
        committed: false,
    },
    error: null,
};

export const editSlice = createSlice({
    name: 'edit',
    initialState,
    reducers: {
        dateInc: (state) => {
            const d = dayjs(state.editor.date, 'YYYYMMDD');
            state.editor.date = d.add(1, 'day').format('YYYYMMDD');
        },
        dateDec: (state) => {
            const d = dayjs(state.editor.date, 'YYYYMMDD');
            state.editor.date = d.subtract(1, 'day').format('YYYYMMDD');
        },
        addPayee: (state, { payload }) => {
            state.editor.payees[payload] = 1 + (payload.match(/&/g) || [] ).length;
        },
        removePayee: (state, { payload }) => {
            delete state.editor.payees[payload];
        },
        addPayer: (state, { payload }) => {
            state.editor.payers[payload] = 1 + (payload.match(/&/g) || [] ).length;
        },
        removePayer: (state, { payload }) => {
            delete state.editor.payers[payload];
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
