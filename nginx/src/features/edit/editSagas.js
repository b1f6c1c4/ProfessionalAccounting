import { call, put, takeEvery, takeLatest } from 'redux-saga/effects';
import {
    submitVoucherRequested,
    submitVoucherSucceeded,
    submitVoucherFailed,
} from './editSlice.js';

function* submitVoucher(action) {
    try {
        const text = yield call(undefined); // TODO
        yield put(submitVoucherSucceeded(text));
    } catch (e) {
        yield put(submitVoucherFailed(e.message));
    }
}

export default function* () {
    yield takeEvery(submitVoucherRequested, submitVoucher);
}
