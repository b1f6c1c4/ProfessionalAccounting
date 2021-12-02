import { configureStore } from '@reduxjs/toolkit';
import createSagaMiddleware from 'redux-saga';

import editReducer from '../containers/edit/editSlice.js';
import editSaga from '../containers/edit/editSagas.js';

const sagaMiddleware = createSagaMiddleware();

export const store = configureStore({
    reducer: {
        edit: editReducer,
    },
    middleware: (getDefaultMiddleware) =>
        getDefaultMiddleware().concat(sagaMiddleware),
    devTools: process.env.NODE_ENV !== 'production',
});

sagaMiddleware.run(editSaga);
