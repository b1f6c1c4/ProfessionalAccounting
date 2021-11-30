import { configureStore } from '@reduxjs/toolkit';
import editReducer from '../features/edit/editSlice.js';

export const store = configureStore({
    reducer: {
        edit: editReducer,
    },
});
