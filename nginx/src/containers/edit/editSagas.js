import { call, put, select, takeEvery, takeLatest } from 'redux-saga/effects';
import * as Api from '../../app/api.js';
import {
    submitVoucherRequested,
    submitVoucherSucceeded,
    submitVoucherFailed,
    revertVoucherRequested,
    revertVoucherSucceeded,
    revertVoucherFailed,
} from './editSlice.js';

const theUser = window.localStorage.getItem('user') || 'anonymous';

function* submitVoucher(action) {
    try {
        const code = yield select((state) => state.edit.liveViewText);
        const { data } = yield call(Api.voucherUpsertApi, code, theUser);
        yield put(submitVoucherSucceeded(data));
    } catch (e) {
        yield put(submitVoucherFailed(e.message));
    }
}

function* revertVoucher(action) {
    try {
        const code = yield select((state) => state.edit.liveViewText);
        yield call(Api.voucherRemovalApi, code, theUser);
        yield put(revertVoucherSucceeded());
    } catch (e) {
        yield put(revertVoucherFailed(e.message));
    }
}

export default function* () {
    yield takeEvery(submitVoucherRequested, submitVoucher);
    yield takeEvery(revertVoucherRequested, revertVoucher);
}
