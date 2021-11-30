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
        commited: false,
    },
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
    },
});

export const {
    dateInc,
    dateDec,
} = editSlice.actions;

console.log(dateInc());

export default editSlice.reducer;
