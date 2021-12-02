import { createSlice } from '@reduxjs/toolkit';
import dayjs from 'dayjs';

const initialState = {
    loading: false,
    liveViewText: '',
    editor: {
        date: dayjs().format('YYYYMMDD'),
        person: 'anonymous',
        activity: '',
        details: [],
        adjustments: [],
        payments: [],
        checksum: 0,
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
    submitVoucherRequested,
    submitVoucherSucceeded,
    submitVoucherFailed,
} = editSlice.actions;

export default editSlice.reducer;
